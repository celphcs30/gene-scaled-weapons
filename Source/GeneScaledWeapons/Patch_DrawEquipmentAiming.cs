using System;
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
        // Try fast FieldRef; fallback to FieldInfo if layout changes
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnRef =
            AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");
        private static readonly FieldInfo PawnField =
            AccessTools.Field(typeof(PawnRenderer), "pawn");

        private static Pawn GetPawn(PawnRenderer r)
        {
            try { return PawnRef != null ? PawnRef(r) : null; }
            catch { /* ignore */ }
            try { return (Pawn)PawnField?.GetValue(r); }
            catch { return null; }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            // We look for Graphics.DrawMesh(mesh, matrix, material, layer) calls
            var drawMeshMatrix = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });

            // Some builds use the Vector3/Quaternion overload; handle that too.
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
                    // Stack: mesh, matrix, material, layer
                    // We add: ldarg.0 (this PawnRenderer), ldarg.1 (Thing eq)
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // eq (assumes eq is 1st param)
                    yield return new CodeInstruction(OpCodes.Call, wrapperMatrix);
                    replaced++;
                    continue;
                }
                if (ci.Calls(drawMeshTR))
                {
                    // Stack: mesh, position, rotation, material, layer
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // eq
                    yield return new CodeInstruction(OpCodes.Call, wrapperTR);
                    replaced++;
                    continue;
                }

                yield return ci;
            }

            if (replaced == 0)
                Log.Warning("[GeneScaledWeapons] Transpiler found no Graphics.DrawMesh calls to replace. Weapon scale may not apply.");
        }

        // Wrapper for Matrix4x4 overload
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
            catch { /* never break draw chain */ }

            Graphics.DrawMesh(mesh, matrix, material, layer);
        }

        // Wrapper for Transform (pos/rot) overload
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
                        // Build a scale matrix around pos/rot
                        var m = Matrix4x4.TRS(pos, rot, new Vector3(f, 1f, f));
                        Graphics.DrawMesh(mesh, m, material, layer);
                        return;
                    }
                }
            }
            catch { /* fall through */ }

            Graphics.DrawMesh(mesh, pos, rot, material, layer);
        }
    }
}
