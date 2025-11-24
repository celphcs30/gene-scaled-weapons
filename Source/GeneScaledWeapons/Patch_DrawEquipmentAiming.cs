using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    [HarmonyPatch(typeof(PawnRenderer), "DrawEquipmentAiming")]
    public static class Patch_DrawEquipmentAiming
    {
        // Fast ref to private field PawnRenderer.pawn
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnRef =
            AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

        // We swap Graphics.DrawMesh call with our wrapper that can scale by pawn
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var drawMesh = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });

            var wrapper = AccessTools.Method(typeof(Patch_DrawEquipmentAiming), nameof(DrawMeshScaledWrapper));

            var code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count; i++)
            {
                var ci = code[i];

                if (ci.Calls(drawMesh))
                {
                    // Stack before call: mesh, matrix, material, layer
                    // We need to also pass renderer (this) and eq (arg1)
                    // Insert ldarg.0 (this) and ldarg.1 (Thing eq)
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // PawnRenderer
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // Thing eq
                    // Replace call with our wrapper call
                    yield return new CodeInstruction(OpCodes.Call, wrapper);
                    continue;
                }

                yield return ci;
            }
        }

        // Our wrapper does: if not blacklisted => matrix = matrix * Scale(factor); then draw.
        public static void DrawMeshScaledWrapper(Mesh mesh, Matrix4x4 matrix, Material material, int layer, PawnRenderer renderer, Thing eq)
        {
            try
            {
                var def = eq?.def;
                if (def != null && !GeneScaleUtil.ShouldSkip(def))
                {
                    var pawn = PawnRef(renderer);
                    float f = GeneScaleUtil.GetPawnScaleFactor(pawn);
                    if (f > 0f && Mathf.Abs(f - 1f) > 0.01f)
                    {
                        // Post-multiply to affect only scale (keeps translation intact)
                        matrix = matrix * Matrix4x4.Scale(new Vector3(f, 1f, f));
                    }
                }
            }
            catch
            {
                // swallow; drawing must never hard-fail
            }
            Graphics.DrawMesh(mesh, matrix, material, layer);
        }
    }
}

