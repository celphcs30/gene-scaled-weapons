using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace GeneScaledWeapons
{
    internal static class EquipmentContextPatcher
    {
        public static int Apply(Harmony harmony)
        {
            int patched = 0;

            var prefix = new HarmonyMethod(typeof(EquipmentContextPatcher).GetMethod(nameof(ContextPrefix), BindingFlags.NonPublic | BindingFlags.Static));
            var finalizer = new HarmonyMethod(typeof(EquipmentContextPatcher).GetMethod(nameof(ContextFinalizer), BindingFlags.NonPublic | BindingFlags.Static));

            // 1) RenderNode equipment: patch any ...Render method that takes a PawnRenderContext
            try
            {
                var nodeTargets = FindRenderNodeEquipmentRenderMethods();
                foreach (var m in nodeTargets)
                {
                    harmony.Patch(m, prefix, null, null, finalizer);
                    patched++;
                    Log.Message($"[GeneScaledWeapons] Patched RN: {m.DeclaringType?.Name}.{m.Name}");
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[GeneScaledWeapons] RN equipment scan failed: {e}");
            }

            // 2) Legacy utility: PawnRenderUtility.DrawEquipmentAndApparelExtras (1.4–1.6 in some builds)
            try
            {
                var m = AccessTools.Method(typeof(PawnRenderUtility), "DrawEquipmentAndApparelExtras");
                if (m != null)
                {
                    harmony.Patch(m, prefix, null, null, finalizer);
                    patched++;
                    Log.Message($"[GeneScaledWeapons] Patched: {m.DeclaringType?.Name}.{m.Name}");
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[GeneScaledWeapons] Failed to patch DrawEquipmentAndApparelExtras: {e}");
            }

            // 3) Legacy renderer methods if they still exist (1.3–1.5)
            try
            {
                var prType = typeof(PawnRenderer);
                foreach (var m in prType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (m.Name == "DrawEquipment" || m.Name == "DrawEquipmentAiming")
                    {
                        harmony.Patch(m, prefix, null, null, finalizer);
                        patched++;
                        Log.Message($"[GeneScaledWeapons] Patched: {m.DeclaringType?.Name}.{m.Name}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[GeneScaledWeapons] Failed to patch PawnRenderer equipment methods: {e}");
            }

            Log.Message($"[GeneScaledWeapons] Equipment context methods patched: {patched}");
            return patched;
        }

        private static IEnumerable<MethodInfo> FindRenderNodeEquipmentRenderMethods()
        {
            var results = new List<MethodInfo>();
            var asms = AppDomain.CurrentDomain.GetAssemblies();

            // Accept both Verse.* and RimWorld.* nodes
            foreach (var asm in asms)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException rtle) { types = rtle.Types.Where(t => t != null).ToArray(); }

                foreach (var t in types)
                {
                    if (t == null) continue;

                    // Heuristic: node types with "Equipment" in the name that are PawnRenderNode or Worker (1.6)
                    // e.g., Verse.PawnRenderNode_Equipment, RimWorld.PawnRenderNode_Equipment, Verse.PawnRenderNodeWorker_Equipment
                    bool nameMatches = t.Name.IndexOf("Equipment", StringComparison.OrdinalIgnoreCase) >= 0
                                       && (t.Name.Contains("PawnRenderNode") || t.Name.Contains("RenderNode"));

                    if (!nameMatches) continue;

                    // Find Render(PawnRenderContext ...) method
                    var render = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                  .FirstOrDefault(mi =>
                                  {
                                      if (mi.Name != "Render") return false;
                                      var ps = mi.GetParameters();
                                      if (ps.Length != 1) return false;
                                      var p0 = ps[0].ParameterType;
                                      return p0.Name.Contains("PawnRenderContext");
                                  });

                    if (render != null)
                        results.Add(render);
                }
            }

            return results;
        }

        // One prefix that can extract the Pawn from different targets
        private static void ContextPrefix(object __instance, object[] __args)
        {
            Pawn pawn = null;

            // Try instance field (PawnRenderer path)
            if (__instance != null)
            {
                var instType = __instance.GetType();
                if (typeof(PawnRenderer).IsAssignableFrom(instType))
                {
                    var fi = AccessTools.Field(instType, "pawn") ?? AccessTools.Field(typeof(PawnRenderer), "pawn");
                    if (fi != null) pawn = fi.GetValue(__instance) as Pawn;
                }
            }

            // Try args: direct Pawn or PawnRenderContext
            if (pawn == null && __args != null)
            {
                foreach (var arg in __args)
                {
                    if (arg is Pawn p) { pawn = p; break; }
                    if (arg == null) continue;

                    var t = arg.GetType();
                    if (t.Name.Contains("PawnRenderContext"))
                    {
                        var f = AccessTools.Field(t, "pawn") ?? AccessTools.Field(t, "Pawn");
                        if (f != null) pawn = f.GetValue(arg) as Pawn;
                        if (pawn == null)
                        {
                            var prop = AccessTools.Property(t, "pawn") ?? AccessTools.Property(t, "Pawn");
                            if (prop != null) pawn = prop.GetValue(arg, null) as Pawn;
                        }
                        if (pawn != null) break;
                    }
                }
            }

            if (pawn != null)
            {
                RenderCtx.CurrentPawn = pawn;
                var factor = ScaleFactor.Factor(pawn);
                if (Prefs.DevMode)
                    Log.Message($"[GeneScaledWeapons] Context set: pawn={pawn?.LabelShortCap}, factor={factor:F2}");
            }
            else if (Prefs.DevMode)
            {
                Log.Warning($"[GeneScaledWeapons] ContextPrefix: Could not extract pawn from instance={__instance?.GetType()?.Name}, args={__args?.Length}");
            }
        }

        private static void ContextFinalizer()
        {
            RenderCtx.CurrentPawn = null;
        }
    }
}

