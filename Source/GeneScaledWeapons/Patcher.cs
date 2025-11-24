using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace GeneScaledWeapons
{
    internal static class Patcher
    {
        internal static int Apply(Harmony harmony)
        {
            int patched = 0;

            // Aiming
            var aiming = TryFind(typeof(PawnRenderer), "DrawEquipmentAiming");
            if (aiming != null)
            {
                var transp = new HarmonyMethod(typeof(Patch_DrawEquipmentAiming).GetMethod(nameof(Patch_DrawEquipmentAiming.Transpiler_Aiming), BindingFlags.Public | BindingFlags.Static));
                harmony.Patch(aiming, transpiler: transp);
                patched++;
            }
            else Log.Warning("[GeneScaledWeapons] Could not find PawnRenderer.DrawEquipmentAiming; aiming scale will be skipped.");

            // Idle/held
            var equip = TryFind(typeof(PawnRenderer), "DrawEquipment");
            if (equip != null)
            {
                var transp = new HarmonyMethod(typeof(Patch_DrawEquipment).GetMethod(nameof(Patch_DrawEquipment.Transpiler_Equip), BindingFlags.Public | BindingFlags.Static));
                harmony.Patch(equip, transpiler: transp);
                patched++;
            }
            else Log.Warning("[GeneScaledWeapons] Could not find PawnRenderer.DrawEquipment; idle/held scale will be skipped.");

            return patched;
        }

        private static MethodInfo TryFind(Type type, string name)
        {
            // Prefer exact name
            var m = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) return m;

            // Fallback: any matching name
            var candidates = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(mi => mi.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (candidates.Count == 1) return candidates[0];
            return candidates.FirstOrDefault();
        }
    }
}
