using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    internal static class ScaleFactor
    {
        public static float Factor(Pawn pawn)
        {
            if (pawn == null) return 1f;
            float f = pawn.BodySize; // core behavior: scale to body size
            if (f < 0.25f) f = 0.25f;
            if (f > 4f) f = 4f;
            return f;
        }

        public static Matrix4x4 MultiplyMatrix(Matrix4x4 m, Pawn pawn)
        {
            float f = Factor(pawn);
            if (Mathf.Approximately(f, 1f)) return m;

            // Scale X and Z columns uniformly
            m.m00 *= f; m.m10 *= f; m.m20 *= f; m.m30 *= f;
            m.m02 *= f; m.m12 *= f; m.m22 *= f; m.m32 *= f;
            return m;
        }

        // Legacy Vector3 multiply (for backward compat with old TRS patches)
        public static Vector3 MultiplyVec(Vector3 vanillaScale, object pawnOrRenderer, Thing eq)
        {
            Pawn pawn = null;
            if (pawnOrRenderer is Pawn p)
            {
                pawn = p;
            }
            else if (pawnOrRenderer is PawnRenderer pr)
            {
                var pawnField = AccessTools.Field(typeof(PawnRenderer), "pawn");
                pawn = pawnField?.GetValue(pr) as Pawn;
            }
            
            if (pawn == null) return vanillaScale;
            
            float f = Factor(pawn);
            if (f == 1f) return vanillaScale;
            
            return new Vector3(vanillaScale.x * f, vanillaScale.y, vanillaScale.z * f);
        }
    }
}

