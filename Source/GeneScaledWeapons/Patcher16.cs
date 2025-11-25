using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    internal static class Patcher16
    {
        private static bool applied;
        static readonly string[] NameHints = { "equip", "weapon", "held", "carry", "primary", "offhand" };

        internal static int Apply(Harmony harmony)
        {
            if (applied) return 0;
            applied = true;

            int rnPatched = 0;
            var baseNode = AccessTools.TypeByName("Verse.PawnRenderNode");
            var baseWorker = AccessTools.TypeByName("Verse.PawnRenderNodeWorker");

            if (baseNode == null)
            {
                GSWLog.Trace("1.6 patch: Verse.PawnRenderNode not found; skipping.");
                return 0;
            }

            var asm = typeof(Pawn).Assembly;
            IEnumerable<Type> candidates = asm.GetTypes().Where(t =>
                t != null
                && !t.IsAbstract
                && ((baseNode.IsAssignableFrom(t) || (baseWorker != null && baseWorker.IsAssignableFrom(t))))
                && NameHints.Any(h => t.Name.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0));

            var transpiler = new HarmonyMethod(typeof(Patch_RenderNode_Equipment).GetMethod(nameof(Patch_RenderNode_Equipment.Transpiler), BindingFlags.Public | BindingFlags.Static));

            foreach (var t in candidates)
            {
                var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               .Where(m => !m.IsAbstract && !m.IsGenericMethodDefinition
                                           && (m.Name.IndexOf("render", StringComparison.OrdinalIgnoreCase) >= 0
                                               || m.Name.IndexOf("draw", StringComparison.OrdinalIgnoreCase) >= 0));

                foreach (var m in methods)
                {
                    try
                    {
                        harmony.Patch(m, transpiler: transpiler);
                        rnPatched++;
                        GSWLog.Trace($"1.6: Patched {t.FullName}.{m.Name}");
                    }
                    catch (Exception e)
                    {
                        GSWLog.WarnOnce($"1.6: Failed to patch {t.FullName}.{m.Name}: {e}", m.GetHashCode());
                        // Don't count failed patches
                    }
                }
            }

            return rnPatched;
        }

#if DEBUG
        internal static void Debug_ListRenderNodes()
        {
            var baseNode = AccessTools.TypeByName("Verse.PawnRenderNode");
            if (baseNode == null)
            {
                GSWLog.Verb("Debug: no PawnRenderNode type found.");
                return;
            }

            var asm = typeof(Pawn).Assembly;
            var nodes = asm.GetTypes().Where(t => t != null && baseNode.IsAssignableFrom(t)).ToList();

            GSWLog.Verb($"Debug: Found {nodes.Count} PawnRenderNode types:");
            foreach (var t in nodes)
            {
                var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name.IndexOf("Render", StringComparison.OrdinalIgnoreCase) >= 0
                             || m.Name.IndexOf("Draw", StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(m => m.Name).Distinct();
                GSWLog.Trace($" - {t.FullName} methods: {string.Join(", ", methods)}");
            }
        }
#endif
    }

    public static class Patch_RenderNode_Equipment
    {
        static readonly MethodInfo TRS = AccessTools.Method(typeof(Matrix4x4), nameof(Matrix4x4.TRS), new[] { typeof(Vector3), typeof(Quaternion), typeof(Vector3) });
        static readonly MethodInfo ScaleM = AccessTools.Method(typeof(Matrix4x4), nameof(Matrix4x4.Scale), new[] { typeof(Vector3) });
        static readonly MethodInfo Adjust = AccessTools.Method(typeof(Patch_RenderNode_Equipment), nameof(AdjustScale));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var list = new List<CodeInstruction>(instructions);
            bool patchedAny = false;

            for (int i = 0; i < list.Count; i++)
            {
                var ins = list[i];

                if (ins.Calls(TRS) || ins.Calls(ScaleM))
                {
                    // Stack has ..., (for TRS: pos, rot,) scale
                    // Method signature: AdjustScale(Vector3 scale, object nodeObj)
                    // Parameters are pushed left-to-right, so scale (already there), then push nodeObj
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // push nodeObj
                    yield return new CodeInstruction(OpCodes.Call, Adjust); // AdjustScale(scale, nodeObj) -> returns adjusted scale
                    patchedAny = true;
                }

                yield return ins;
            }

            if (!patchedAny)
            {
                GSWLog.Trace($"Note: {original.DeclaringType?.FullName}.{original.Name} had no TRS/Scale call to patch.");
            }
        }

        public static Vector3 AdjustScale(Vector3 scale, object nodeObj)
        {
            try
            {
                var pawn = TryGetPawnFromNode(nodeObj);
                if (pawn == null) return scale;

                // Get primary equipment
                var weapon = pawn.equipment?.Primary;
                if (weapon == null) return scale;

                // Central scale gate (BEWH_* blacklist, settings, extensions)
                if (!ScaleGate.ShouldScale(weapon)) return scale;

                // Per-pawn multiplier
                float factor = GeneScaleUtil.GetPawnScaleFactor(pawn);
                if (Mathf.Approximately(factor, 1f)) return scale;

                // Apply per-def multiplier
                float extraMult = ScaleGate.ExtraMult(weapon);
                factor *= extraMult;

                scale.x *= factor;
                scale.z *= factor;
            }
            catch (Exception e)
            {
                GSWLog.Error("AdjustScale failed: " + e);
            }
            return scale;
        }

        static Pawn TryGetPawnFromNode(object nodeObj)
        {
            if (nodeObj == null) return null;
            var t = nodeObj.GetType();

            // Property Pawn
            var prop = AccessTools.Property(t, "Pawn");
            var getter = prop?.GetGetMethod(true);
            if (getter != null)
                return getter.Invoke(nodeObj, null) as Pawn;

            // Field pawn on node or base
            var fld = AccessTools.Field(t, "pawn") ?? AccessTools.Field(AccessTools.TypeByName("Verse.PawnRenderNode"), "pawn");
            if (fld != null)
                return fld.GetValue(nodeObj) as Pawn;

            // Fallback via tree
            var treeFld = AccessTools.Field(t, "tree");
            var tree = treeFld?.GetValue(nodeObj);
            if (tree != null)
            {
                var pawnFld = AccessTools.Field(tree.GetType(), "pawn");
                if (pawnFld != null) return pawnFld.GetValue(tree) as Pawn;

                var pawnProp = AccessTools.Property(tree.GetType(), "Pawn");
                var pawnGetter = pawnProp?.GetGetMethod(true);
                if (pawnGetter != null) return pawnGetter.Invoke(tree, null) as Pawn;
            }

            return null;
        }
    }
}

