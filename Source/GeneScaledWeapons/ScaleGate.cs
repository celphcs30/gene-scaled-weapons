using System;
using Verse;

namespace GeneScaledWeapons
{
    internal static class ScaleGate
    {
        private static readonly string[] DefNamePrefixBlacklist = { "BEWH_" };

        public static bool ShouldScale(Thing eq)
        {
            if (eq?.def == null) return false;

            var s = GeneScaledWeaponsMod.Settings;
            bool force = s?.debugForceScale ?? false;

            // Prefix blacklist unless forced
            string dn = eq.def.defName ?? string.Empty;
            if (!force)
            {
                foreach (var p in DefNamePrefixBlacklist)
                    if (dn.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                        return false;

                // Per-def opt-out
                var ext = eq.def.GetModExtension<GSWDefExtension>();
                if (ext?.disableScaling == true)
                    return false;
            }

            // Settings (null-safe, default true/true to avoid accidental off during load)
            bool isRanged = eq.def.IsRangedWeapon;
            bool scaleRanged = s?.scaleRanged ?? true;
            bool scaleMelee = s?.scaleMelee ?? true;

            if (isRanged) return scaleRanged;
            return scaleMelee;
        }

        public static float ExtraMult(Thing eq)
        {
            var ext = eq?.def?.GetModExtension<GSWDefExtension>();
            return ext?.scaleMult ?? 1f;
        }
    }
}
