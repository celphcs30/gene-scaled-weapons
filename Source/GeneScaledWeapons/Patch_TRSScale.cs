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
    internal static class Patch_TRSScale
    {
        static readonly MethodInfo TRS = AccessTools.Method(typeof(Matrix4x4), nameof(Matrix4x4.TRS), new[] { typeof(Vector3), typeof(Quaternion), typeof(Vector3) });
        static readonly MethodInfo ScaleM = AccessTools.Method(typeof(Matrix4x4), nameof(Matrix4x4.Scale), new[] { typeof(Vector3) });
        static readonly MethodInfo Adjust = AccessTools.Method(typeof(Patch_TRSScale), nameof(AdjustScale));

        private static CodeInstruction LoadArg(int index)
        {
            return index switch
            {
                0 => new CodeInstruction(OpCodes.Ldarg_0),
                1 => new CodeInstruction(OpCodes.Ldarg_1),
                2 => new CodeInstruction(OpCodes.Ldarg_2),
                3 => new CodeInstruction(OpCodes.Ldarg_3),
                _ => new CodeInstruction(OpCodes.Ldarg_S, (byte)index)
            };
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var list = new List<CodeInstruction>(instructions);
            bool patchedAny = false;

            int pawnParamIndex = -1;
            if (original.IsStatic)
            {
                var pars = original.GetParameters();
                for (int i = 0; i < pars.Length; i++)
                    if (typeof(Pawn).IsAssignableFrom(pars[i].ParameterType)) { pawnParamIndex = i; break; }
            }

            for (int i = 0; i < list.Count; i++)
            {
                var ins = list[i];

                if (ins.Calls(TRS) || ins.Calls(ScaleM))
                {
                    // Stack: …, scale -> …, scale, context
                    if (original.IsStatic && pawnParamIndex >= 0)
                        yield return LoadArg(pawnParamIndex);
                    else
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // this

                    yield return new CodeInstruction(OpCodes.Call, Adjust);
                    patchedAny = true;
                }

                yield return ins;
            }

#if DEBUG
            if (!patchedAny)
                Log.Message($"[GeneScaledWeapons] Note: {original.DeclaringType?.FullName}.{original.Name} had no TRS/Scale call to patch.");
#endif
            // no explicit return; iterator ends
        }

        // Adjusts x/z scale for pawns with your gene. Context can be Pawn or a render node instance.
        public static Vector3 AdjustScale(Vector3 s, object context)
        {
            try
            {
#if DEBUG
                // Log.Message($"[GeneScaledWeapons] AdjustScale called with scale {s}, context {context?.GetType().Name ?? "null"}");
#endif
                Pawn pawn = null;

                if (context is Pawn p) pawn = p;
                else
                {
                    // Try node -> pawn
                    var nodeType = AccessTools.TypeByName("Verse.PawnRenderNode");
                    if (nodeType != null && context != null && nodeType.IsInstanceOfType(context))
                    {
                        // Try common paths to get Pawn from node
                        // 1) node.tree?.pawn property/field
                        var tree = AccessTools.Field(context.GetType(), "tree")?.GetValue(context)
                                   ?? AccessTools.Property(context.GetType(), "tree")?.GetValue(context, null);
                        if (tree != null)
                        {
                            pawn = AccessTools.Field(tree.GetType(), "pawn")?.GetValue(tree) as Pawn
                                   ?? AccessTools.Property(tree.GetType(), "pawn")?.GetValue(tree, null) as Pawn;
                        }
                        // 2) direct pawn field/property
                        pawn ??= AccessTools.Field(context.GetType(), "pawn")?.GetValue(context) as Pawn
                                 ?? AccessTools.Property(context.GetType(), "pawn")?.GetValue(context, null) as Pawn;
                    }
                }

                if (pawn == null)
                    return s;

                // Check if the weapon should be skipped (get primary equipment)
                var weapon = pawn.equipment?.Primary;
                if (weapon?.def != null && GeneScaleUtil.ShouldSkip(weapon.def))
                    return s; // Skip scaling for this weapon

                float mult = GeneScaleUtil.GetPawnScaleFactor(pawn);
#if DEBUG
                Log.Message($"[GeneScaledWeapons] AdjustScale for {pawn?.LabelShort ?? "null"}: {s} -> mult {mult}");
#endif
                if (Mathf.Approximately(mult, 1f)) return s;

                s.x *= mult;
                s.z *= mult;
                return s;
            }
            catch
            {
                return s;
            }
        }
    }
}
