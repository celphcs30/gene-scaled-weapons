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
        internal static int Apply(Harmony harmony)
        {
            int patched = 0;

            var baseNode = AccessTools.TypeByName("Verse.PawnRenderNode");
            if (baseNode == null)
            {
                Log.Message("[GeneScaledWeapons] 1.6 patch: Verse.PawnRenderNode not found; skipping 1.6 render-node patch.");
                return 0;
            }

            var asm = typeof(Pawn).Assembly; // Assembly-CSharp
            var candidates = asm.GetTypes().Where(t =>
                t != null
                && baseNode.IsAssignableFrom(t)
                && (t.Name.IndexOf("Equipment", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.Name.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0));

            var transpiler = new HarmonyMethod(typeof(Patch_RenderNode_Equipment).GetMethod(nameof(Patch_RenderNode_Equipment.Transpiler), BindingFlags.Public | BindingFlags.Static));

            foreach (var t in candidates)
            {
                var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               .Where(m => m.Name.IndexOf("Render", StringComparison.OrdinalIgnoreCase) >= 0);

                foreach (var m in methods)
                {
                    try
                    {
                        harmony.Patch(m, transpiler: transpiler);
                        Log.Message($"[GeneScaledWeapons] Patched {t.FullName}.{m.Name} for equipment scaling.");
                        patched++;
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"[GeneScaledWeapons] Failed to patch {t.FullName}.{m.Name}: {e}");
                    }
                }
            }

            if (patched == 0)
                Log.Warning("[GeneScaledWeapons] 1.6 patch: No equipment/weapon render-node methods got patched. Weapon scaling disabled.");
            else
                Log.Message($"[GeneScaledWeapons] 1.6 patch: Patched {patched} method(s).");

            return patched;
        }
    }

    public static class Patch_RenderNode_Equipment
    {
        static readonly MethodInfo TRS = AccessTools.Method(typeof(Matrix4x4), nameof(Matrix4x4.TRS), new[] { typeof(Vector3), typeof(Quaternion), typeof(Vector3) });
        static readonly MethodInfo Adjust = AccessTools.Method(typeof(Patch_RenderNode_Equipment), nameof(AdjustScale));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var list = new List<CodeInstruction>(instructions);
            bool patchedAny = false;

            for (int i = 0; i < list.Count; i++)
            {
                var ins = list[i];

                if (ins.Calls(TRS))
                {
                    // Stack before TRS: pos(Vector3), rot(Quaternion), scale(Vector3)
                    // We need to adjust scale before it's consumed by TRS
                    // Method signature: AdjustScale(Vector3 scale, object nodeObj)
                    // Parameters are pushed left-to-right, so scale (already there), then push nodeObj
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // push nodeObj -> stack: pos, rot, scale, nodeObj
                    yield return new CodeInstruction(OpCodes.Call, Adjust); // AdjustScale(scale, nodeObj) -> returns adjusted scale -> stack: pos, rot, adjusted_scale
                    patchedAny = true;
                }

                yield return ins;
            }

            if (!patchedAny)
            {
                Log.Message($"[GeneScaledWeapons] Note: {original.DeclaringType?.FullName}.{original.Name} had no TRS call to patch.");
            }
        }

        public static Vector3 AdjustScale(Vector3 scale, object nodeObj)
        {
            try
            {
                var pawn = TryGetPawnFromNode(nodeObj);
                if (pawn != null)
                {
                    // Check if the weapon should be skipped (get primary equipment)
                    var weapon = pawn.equipment?.Primary;
                    if (weapon?.def != null && GeneScaleUtil.ShouldSkip(weapon.def))
                        return scale; // Skip scaling for this weapon

                    float factor = GeneScaleUtil.GetPawnScaleFactor(pawn);
                    if (!Mathf.Approximately(factor, 1f))
                    {
                        scale.x *= factor;
                        scale.z *= factor;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[GeneScaledWeapons] AdjustScale failed: " + e);
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

