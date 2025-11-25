using System;
using Verse;

namespace GeneScaledWeapons
{
    internal static class ScaleGate
    {
        private static readonly string[] DefNamePrefixBlacklist = { "BEWH_" }; // RimDark/RimWarhammer

        public static bool ShouldScale(Thing eq)
        {
            if (eq?.def == null) return false;

            // Prefix blacklist
            var dn = eq.def.defName;
            if (!string.IsNullOrEmpty(dn))
            {
                foreach (var p in DefNamePrefixBlacklist)
                    if (dn.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                        return false;
            }

            // Per-def mod extension
            var ext = eq.def.GetModExtension<GSWDefExtension>();
            if (ext != null && ext.disableScaling)
                return false;

            // Settings: ranged vs melee
            bool isRanged = eq.def.IsRangedWeapon;
            var s = GeneScaledWeaponsMod.Settings;
            if (isRanged && !s.scaleRanged) return false;
            if (!isRanged && !s.scaleMelee) return false;

            return true;
        }

        public static float ExtraMult(Thing eq)
        {
            var ext = eq?.def?.GetModExtension<GSWDefExtension>();
            return ext?.scaleMult ?? 1f;
        }
    }
}
