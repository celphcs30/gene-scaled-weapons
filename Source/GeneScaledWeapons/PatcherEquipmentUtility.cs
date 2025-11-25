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

            int classicPatched = 0;

            // Verse.PawnRenderUtility.*DrawEquipment*
            var util = AccessTools.TypeByName("Verse.PawnRenderUtility");
            if (util != null)
            {
                var methods = util.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                                  .Where(m => !m.IsGenericMethodDefinition &&
                                              (m.Name.IndexOf("DrawEquipment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                               m.Name.IndexOf("DrawHeld", StringComparison.OrdinalIgnoreCase) >= 0))
                                  .ToList();

                // Count found targets before patching
                classicPatched += methods.Count;

                foreach (var m in methods)
                {
                    if (!TryPatch(harmony, m))
                        classicPatched--; // Decrement if patch failed
                }
            }

            // Verse.PawnRenderer.*DrawEquipment* (backup path)
            var renderer = AccessTools.TypeByName("Verse.PawnRenderer");
            if (renderer != null)
            {
                var methods = renderer.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                      .Where(m => !m.IsGenericMethodDefinition &&
                                                  (m.Name.IndexOf("DrawEquipment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                   m.Name.IndexOf("DrawHeld", StringComparison.OrdinalIgnoreCase) >= 0))
                                      .ToList();

                // Count found targets before patching
                classicPatched += methods.Count;

                foreach (var m in methods)
                {
                    if (!TryPatch(harmony, m))
                        classicPatched--; // Decrement if patch failed
                }
            }

            return classicPatched;
        }

        private static bool TryPatch(Harmony harmony, MethodBase m)
        {
            try
            {
                var transpiler = new HarmonyMethod(typeof(Patch_TRSScale).GetMethod(nameof(Patch_TRSScale.Transpiler)));
                harmony.Patch(m, transpiler: transpiler);
                GSWLog.Trace($"Equip patch: Patched {m.DeclaringType?.FullName}.{m.Name}");
                return true;
            }
            catch (Exception e)
            {
                GSWLog.WarnOnce($"Equip patch: Failed to patch {m.DeclaringType?.FullName}.{m.Name}: {e}", m.GetHashCode());
                return false;
            }
        }
    }
}
