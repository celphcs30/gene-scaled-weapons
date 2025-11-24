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

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var list = new List<CodeInstruction>(instructions);
            bool patchedAny = false;

            // For static methods (e.g., PawnRenderUtility.DrawEquipmentAiming), pass the Pawn argument.
            // For instance methods (nodes/workers), pass "this".
            int pawnParamIndex = -1;
            if (original.IsStatic)
            {
                var pars = original.GetParameters();
                for (int i = 0; i < pars.Length; i++)
                {
                    if (typeof(Pawn).IsAssignableFrom(pars[i].ParameterType))
                    {
                        pawnParamIndex = i;
                        break;
                    }
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                var ins = list[i];
                bool isTRS = ins.Calls(TRS);
                bool isScale = ins.Calls(ScaleM);

                if (isTRS || isScale)
                {
                    // Stack state before call has the scale Vector3 on top.
                    // Insert a call to AdjustScale(scale, context) BEFORE the TRS/Scale call
                    if (original.IsStatic && pawnParamIndex >= 0)
                    {
                        // ldarg.<pawnIndex>
                        var ldarg = pawnParamIndex switch
                        {
                            0 => OpCodes.Ldarg_0,
                            1 => OpCodes.Ldarg_1,
                            2 => OpCodes.Ldarg_2,
                            3 => OpCodes.Ldarg_3,
                            _ => OpCodes.Ldarg_S
                        };
                        if (pawnParamIndex >= 4)
                        {
                            yield return new CodeInstruction(ldarg, pawnParamIndex);
                        }
                        else
                        {
                            yield return new CodeInstruction(ldarg);
                        }
                    }
                    else
                    {
                        // instance method: use "this"
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                    }
                    yield return new CodeInstruction(OpCodes.Call, Adjust);
                    // Now yield the original TRS/Scale call (with adjusted scale on stack)
                    yield return ins;
                    patchedAny = true;
                }
                else
                {
                    yield return ins;
                }
            }

            if (!patchedAny)
            {
                Log.Message($"[GeneScaledWeapons] Note: {original.DeclaringType?.FullName}.{original.Name} had no TRS/Scale call to patch.");
            }
        }

        // Adjusts x/z scale for pawns with your gene. Context can be Pawn or a render node instance.
        public static Vector3 AdjustScale(Vector3 s, object context)
        {
            try
            {
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
