using RimWorld;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    public static class GeneScaleUtil
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

        public static float GetPawnScaleFactor(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed) return 1f;

            var stat = VefCosmeticSize;
            if (stat == null) return 1f; // VEF not loaded or stat missing

            float f = pawn.GetStatValue(stat, true); // typically 1.0 + offsets
            if (float.IsNaN(f) || float.IsInfinity(f)) f = 1f;

            return Mathf.Clamp(f, 0.5f, 2.5f);
        }

        public static bool ShouldSkip(ThingDef def)
        {
            if (def == null) return true;

            // Explicit per-weapon flag
            if (def.HasModExtension<ModExt_SkipGeneWeaponScale>()) return true;

            // Blanket: any weapon from Rimdark packages (contains "rimdark")
            string pkg = def.modContentPack?.PackageId;
            if (!string.IsNullOrEmpty(pkg) && pkg.ToLowerInvariant().Contains("rimdark")) return true;

            return false;
        }
    }
}

