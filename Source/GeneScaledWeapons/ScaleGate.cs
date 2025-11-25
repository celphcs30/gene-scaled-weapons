using System;
using Verse;
using UnityEngine;

namespace GeneScaledWeapons
{
    internal static class ScaleGate
    {
        private static readonly string[] DefNamePrefixBlacklist = { "BEWH_" }; // RimDark/RimWarhammer
        private static int debugSeen = 0;
        private const int debugMax = 30;

        public static bool ShouldScale(Thing eq)
        {
            if (eq?.def == null)
            {
                DebugGate(eq, "eq/def null -> no scale");
                return false;
            }

            var s = GeneScaledWeaponsMod.Settings;
            bool force = s?.debugForceScale ?? false;

            // Prefix blacklist unless forced
            var dn = eq.def.defName ?? string.Empty;
            if (!force)
            {
                foreach (var p in DefNamePrefixBlacklist)
                {
                    if (dn.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    {
                        DebugGate(eq, $"blacklisted by prefix {p}");
                        return false;
                    }
                }

                var ext = eq.def.GetModExtension<GSWDefExtension>();
                if (ext?.disableScaling == true)
                {
                    DebugGate(eq, "modExtension disableScaling");
                    return false;
                }
            }

            // Settings: be null-safe, default to true for ranged, false for melee
            bool isRanged = eq.def.IsRangedWeapon;
            bool scaleRanged = s?.scaleRanged ?? true;
            bool scaleMelee = s?.scaleMelee ?? false; // Default false for melee, true for ranged

            if (!force)
            {
                if (isRanged && !scaleRanged)
                {
                    DebugGate(eq, "ranged gated by settings");
                    return false;
                }
                if (!isRanged && !scaleMelee)
                {
                    DebugGate(eq, "melee gated by settings");
                    return false;
                }
            }

            DebugGate(eq, "OK");
            return true;
        }

        public static float ExtraMult(Thing eq)
        {
            var ext = eq?.def?.GetModExtension<GSWDefExtension>();
            return ext?.scaleMult ?? 1f;
        }

        private static void DebugGate(Thing eq, string reason)
        {
            if (debugSeen++ >= debugMax) return;
            string dn = eq?.def?.defName ?? "<null>";
            GSWLog.Verb($"ScaleGate: {dn} -> {reason}");
        }
    }
}
