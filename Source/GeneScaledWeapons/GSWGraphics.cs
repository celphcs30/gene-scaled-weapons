using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    internal static class GSWGraphics
    {
        public static void DrawMeshScaled(Mesh mesh, Matrix4x4 matrix, Material material, int layer)
        {
            var pawn = RenderCtx.CurrentPawn;
            if (pawn != null)
                matrix = ScaleFactor.MultiplyMatrix(matrix, pawn);
            Graphics.DrawMesh(mesh, matrix, material, layer);
        }

        public static void DrawMeshScaled(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock props)
        {
            var pawn = RenderCtx.CurrentPawn;
            if (pawn != null)
                matrix = ScaleFactor.MultiplyMatrix(matrix, pawn);
            Graphics.DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, props);
        }
    }
}

