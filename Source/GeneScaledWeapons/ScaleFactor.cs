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

            const float k = 0.5f;              // 0=no change, 1=full match. 0.4–0.6 looks good.
            float target = v;                  // DIRECT mapping (bigger VEF => bigger weapon)
            float f = Mathf.Pow(target, k);

            // Optional mild clamp so the extremes aren't silly. Adjust/tweak or remove.
            // f = Mathf.Clamp(f, 0.75f, 1.6f);

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
    }
}

