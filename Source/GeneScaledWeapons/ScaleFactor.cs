using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    internal static class ScaleFactor
    {
        private static StatDef _vefCosmeticSize;
        private static StatDef VefCosmeticSize
        {
            get
            {
                if (_vefCosmeticSize == null)
                    _vefCosmeticSize = DefDatabase<StatDef>.GetNamedSilentFail("VEF_CosmeticBodySize_Multiplier");
                return _vefCosmeticSize;
            }
        }

        public static float Factor(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed) return 1f;

            var stat = VefCosmeticSize;
            if (stat == null) return 1f; // no VEF -> no change

            float v = pawn.GetStatValue(stat, true);
            if (float.IsNaN(v) || float.IsInfinity(v) || v <= 0f) return 1f;

            const float k = 0.5f; // 0=no change, 1=full size match. Try 0.5 first.
            float target = 1f / v;     // full proportional
            float f = Mathf.Pow(target, k);

            // Optional mild safety clamp so nothing gets absurd
            // f = Mathf.Clamp(f, 0.7f, 1.9f);

            return f;
        }

        public static Matrix4x4 Apply(Matrix4x4 m, Pawn pawn)
        {
            float f = Factor(pawn);
            if (Mathf.Approximately(f, 1f)) return m;
            // Right-multiply to scale in local space (T * R * S) -> T * R * (S * scale)
            var s = Matrix4x4.Scale(new Vector3(f, 1f, f));
            return m * s;
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

        // Legacy alias for backward compat
        public static Matrix4x4 MultiplyMatrix(Matrix4x4 m, Pawn pawn) => Apply(m, pawn);
    }
}

