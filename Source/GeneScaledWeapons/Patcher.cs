using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace GeneScaledWeapons
{
    internal static class Patcher
    {
        internal static int Apply(Harmony harmony, bool suppressWarnings = false)
        {
            int patched = 0;

            // Transpilers
            var aimingTransp = new HarmonyMethod(typeof(Patch_DrawEquipmentAiming).GetMethod(nameof(Patch_DrawEquipmentAiming.Transpiler_Aiming), BindingFlags.Public | BindingFlags.Static));
            var equipTransp  = new HarmonyMethod(typeof(Patch_DrawEquipment).GetMethod(nameof(Patch_DrawEquipment.Transpiler_Equip), BindingFlags.Public | BindingFlags.Static));

            // 1) Try PawnRenderer (legacy)
            patched += TryPatch(harmony, AccessTools.TypeByName("Verse.PawnRenderer"), "DrawEquipmentAiming", aimingTransp);
            patched += TryPatch(harmony, AccessTools.TypeByName("Verse.PawnRenderer"), "DrawEquipment",       equipTransp);

            // 2) Try PawnRenderUtility (newer RW)
            patched += TryPatch(harmony, AccessTools.TypeByName("Verse.PawnRenderUtility"), "DrawEquipmentAiming", aimingTransp);
            patched += TryPatch(harmony, AccessTools.TypeByName("Verse.PawnRenderUtility"), "DrawEquipment",       equipTransp);

            // 3) Fallback: try without namespace (some forks rename namespaces)
            patched += TryPatch(harmony, AccessTools.TypeByName("PawnRenderer"), "DrawEquipmentAiming", aimingTransp);
            patched += TryPatch(harmony, AccessTools.TypeByName("PawnRenderer"), "DrawEquipment",       equipTransp);
            patched += TryPatch(harmony, AccessTools.TypeByName("PawnRenderUtility"), "DrawEquipmentAiming", aimingTransp);
            patched += TryPatch(harmony, AccessTools.TypeByName("PawnRenderUtility"), "DrawEquipment",       equipTransp);

            // 4) Last-resort: scan Assembly-CSharp for any matching method names
            if (patched == 0)
            {
                var asm = typeof(Pawn).Assembly;
                int found = 0;
                foreach (var t in asm.GetTypes())
                {
                    // Skip generated and compiler types
                    if (t.FullName == null || t.FullName.StartsWith("<")) continue;

                    found += TryPatch(harmony, t, "DrawEquipmentAiming", aimingTransp, quiet: true);
                    found += TryPatch(harmony, t, "DrawEquipment",       equipTransp,  quiet: true);
                }

                if (found > 0)
                {
                    Log.Message($"[GeneScaledWeapons] Fallback patched {found} method(s) by assembly scan.");
                    patched += found;
                }
            }

            if (patched == 0 && !suppressWarnings)
            {
                Log.Warning("[GeneScaledWeapons] Could not locate any DrawEquipmentAiming/DrawEquipment methods to patch. Weapon scaling disabled.");
            }
            else if (patched > 0)
            {
                Log.Message($"[GeneScaledWeapons] Legacy patch: Patched {patched} method(s).");
            }

            return patched;
        }

        private static int TryPatch(Harmony harmony, Type type, string methodName, HarmonyMethod transpiler, bool quiet = false)
        {
            if (type == null) return 0;

            // Find method by exact name first, then contains
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var mi = type.GetMethod(methodName, flags)
                     ?? type.GetMethods(flags).FirstOrDefault(m => m.Name.IndexOf(methodName, StringComparison.OrdinalIgnoreCase) >= 0);

            if (mi == null)
            {
                if (!quiet) Log.Warning($"[GeneScaledWeapons] Could not find {type.FullName}.{methodName}; skipping.");
                return 0;
            }

            try
            {
                harmony.Patch(mi, transpiler: transpiler);
                if (!quiet) Log.Message($"[GeneScaledWeapons] Patched {type.FullName}.{mi.Name} ({(mi.IsStatic ? "static" : "instance")}).");
                return 1;
            }
            catch (Exception e)
            {
                Log.Warning($"[GeneScaledWeapons] Failed to patch {type.FullName}.{mi.Name}: {e}");
                return 0;
            }
        }
    }
}
