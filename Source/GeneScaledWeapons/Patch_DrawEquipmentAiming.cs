using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    public static class Patch_DrawEquipmentAiming
    {
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnRef =
            AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");
        private static readonly FieldInfo PawnField =
            AccessTools.Field(typeof(PawnRenderer), "pawn");

        private static Pawn GetPawn(PawnRenderer r)
        {
            try { return PawnRef != null ? PawnRef(r) : null; } catch { }
            try { return (Pawn)PawnField?.GetValue(r); } catch { }
            return null;
        }

        public static IEnumerable<CodeInstruction> Transpiler_Aiming(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            var drawMeshMatrix = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });
            var drawMeshTR = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
                new[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int) });

            var wrapperMatrix = AccessTools.Method(typeof(Patch_DrawEquipmentAiming), nameof(DrawMeshScaledWrapperMatrix));
            var wrapperTR = AccessTools.Method(typeof(Patch_DrawEquipmentAiming), nameof(DrawMeshScaledWrapperTR));

            int replaced = 0;
            for (int i = 0; i < code.Count; i++)
            {
                var ci = code[i];

                if (ci.Calls(drawMeshMatrix))
                {
                    // push this (renderer) and eq (arg1)
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, wrapperMatrix);
                    replaced++;
                    continue;
                }
                if (ci.Calls(drawMeshTR))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, wrapperTR);
                    replaced++;
                    continue;
                }

                yield return ci;
            }

            if (replaced == 0)
                GSWLog.Trace("Aiming transpiler: found no Graphics.DrawMesh calls.");
        }

        public static void DrawMeshScaledWrapperMatrix(Mesh mesh, Matrix4x4 matrix, Material material, int layer, PawnRenderer renderer, Thing eq)
        {
            try
            {
                if (eq?.def != null && !GeneScaleUtil.ShouldSkip(eq.def))
                {
                    var pawn = GetPawn(renderer);
                    float f = GeneScaleUtil.GetPawnScaleFactor(pawn);
                    if (f > 0f && Mathf.Abs(f - 1f) > 0.01f)
                        matrix = matrix * Matrix4x4.Scale(new Vector3(f, 1f, f));
                }
            }
            catch { }

            Graphics.DrawMesh(mesh, matrix, material, layer);
        }

        public static void DrawMeshScaledWrapperTR(Mesh mesh, Vector3 pos, Quaternion rot, Material material, int layer, PawnRenderer renderer, Thing eq)
        {
            try
            {
                if (eq?.def != null && !GeneScaleUtil.ShouldSkip(eq.def))
                {
                    var pawn = GetPawn(renderer);
                    float f = GeneScaleUtil.GetPawnScaleFactor(pawn);
                    if (f > 0f && Mathf.Abs(f - 1f) > 0.01f)
                    {
                        var m = Matrix4x4.TRS(pos, rot, new Vector3(f, 1f, f));
                        Graphics.DrawMesh(mesh, m, material, layer);
                        return;
                    }
                }
            }
            catch { }

            Graphics.DrawMesh(mesh, pos, rot, material, layer);
        }
    }
}
