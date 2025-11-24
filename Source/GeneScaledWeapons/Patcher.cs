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
            var target = TryFindDrawEquipmentAiming();
            if (target != null)
            {
                var transpiler = new HarmonyMethod(typeof(Patch_DrawEquipmentAiming).GetMethod(nameof(Patch_DrawEquipmentAiming.Transpiler), BindingFlags.Public | BindingFlags.Static));
                try
                {
                    harmony.Patch(original: target, transpiler: transpiler);
                    patched++;
                    Log.Message($"[GeneScaledWeapons] Patched: {target.DeclaringType.FullName}.{target.Name}({string.Join(", ", target.GetParameters().Select(p=>p.ParameterType.Name))})");
                }
                catch (Exception e)
                {
                    Log.Error($"[GeneScaledWeapons] Failed to patch {target}: {e}");
                }
            }
            else
            {
                Log.Warning("[GeneScaledWeapons] Could not find PawnRenderer.DrawEquipmentAiming. Skipping weapon scale patch (compat or version mismatch).");
            }
            return patched;
        }

        private static MethodInfo TryFindDrawEquipmentAiming()
        {
            // Primary: PawnRenderer.DrawEquipmentAiming(Thing, Vector3, float) [RimWorld 1.4/1.5 typical]
            var pr = typeof(PawnRenderer);
            var m = pr.GetMethod("DrawEquipmentAiming", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) return m;

            // Fallbacks: look for method by name or signature variants in PawnRenderer
            var candidates = pr.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(mi => mi.Name.IndexOf("DrawEquipmentAiming", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            if (candidates.Count == 1) return candidates[0];
            if (candidates.Count > 1)
            {
                // Prefer ones whose first arg is Thing
                var pick = candidates.FirstOrDefault(mi =>
                {
                    var p = mi.GetParameters();
                    return p.Length >= 1 && typeof(Thing).IsAssignableFrom(p[0].ParameterType);
                }) ?? candidates[0];
                return pick;
            }

            // Some mods move logic; as a last attempt, scan any type named *PawnRender* for a DrawEquipmentAiming
            var asm = typeof(PawnRenderer).Assembly;
            var altType = asm.GetTypes().FirstOrDefault(t => t.Name.Contains("PawnRender") && t.GetMethod("DrawEquipmentAiming", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null);
            return altType?.GetMethod("DrawEquipmentAiming", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}

