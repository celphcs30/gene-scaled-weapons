using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace GeneScaledWeapons
{
    internal static class PatcherEquipmentUtility
    {
        private static bool applied;

        internal static int Apply(Harmony harmony)
        {
            if (applied) return 0;
            applied = true;

            int patched = 0;

            // Verse.PawnRenderUtility.*DrawEquipment*
            var util = AccessTools.TypeByName("Verse.PawnRenderUtility");
            if (util != null)
            {
                var methods = util.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                                  .Where(m => !m.IsGenericMethodDefinition &&
                                              (m.Name.IndexOf("DrawEquipment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                               m.Name.IndexOf("DrawHeld", StringComparison.OrdinalIgnoreCase) >= 0));

                foreach (var m in methods)
                {
                    TryPatch(harmony, m, ref patched);
                }
            }

            // Verse.PawnRenderer.*DrawEquipment* (backup path)
            var renderer = AccessTools.TypeByName("Verse.PawnRenderer");
            if (renderer != null)
            {
                var methods = renderer.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                      .Where(m => !m.IsGenericMethodDefinition &&
                                                  (m.Name.IndexOf("DrawEquipment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                   m.Name.IndexOf("DrawHeld", StringComparison.OrdinalIgnoreCase) >= 0));

                foreach (var m in methods)
                {
                    TryPatch(harmony, m, ref patched);
                }
            }

            if (patched == 0)
                GSWLog.WarnOnce("Equip patch: found no *DrawEquipment* methods to patch.", 19482232);
            else
                GSWLog.Min($"Equip patch: Patched {patched} method(s).");

            return patched;
        }

        private static void TryPatch(Harmony harmony, MethodBase m, ref int count)
        {
            try
            {
                var transpiler = new HarmonyMethod(typeof(Patch_TRSScale).GetMethod(nameof(Patch_TRSScale.Transpiler)));
                harmony.Patch(m, transpiler: transpiler);
                GSWLog.Trace($"Equip patch: Patched {m.DeclaringType?.FullName}.{m.Name}");
                count++;
            }
            catch (Exception e)
            {
                GSWLog.WarnOnce($"Equip patch: Failed to patch {m.DeclaringType?.FullName}.{m.Name}: {e}", m.GetHashCode());
            }
        }
    }
}
