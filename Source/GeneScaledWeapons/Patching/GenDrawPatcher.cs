using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    internal static class GenDrawPatcher
    {
        public static int Apply(Harmony harmony)
        {
            int patched = 0;
            try
            {
                var targets = typeof(GenDraw).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(m => m.Name == "DrawMeshNowOrLater"
                        && m.GetParameters().Any(p => p.ParameterType == typeof(Matrix4x4)))
                    .ToArray();

                var prefix = new HarmonyMethod(typeof(GenDrawPatcher).GetMethod(nameof(ScaleMatrixPrefix), BindingFlags.NonPublic | BindingFlags.Static));

                foreach (var m in targets)
                {
                    harmony.Patch(m, prefix, null);
                    patched++;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[GeneScaledWeapons] Failed to patch GenDraw.DrawMeshNowOrLater: {e}");
            }

            Log.Message($"[GeneScaledWeapons] GenDraw.DrawMeshNowOrLater patched: {patched}");
            return patched;
        }

        // Generic prefix: locate the Matrix4x4 arg in __args and scale it if we are in equipment context
        private static void ScaleMatrixPrefix(object[] __args)
        {
            var pawn = RenderCtx.CurrentPawn;
            if (pawn == null || __args == null) return;

            for (int i = 0; i < __args.Length; i++)
            {
                if (__args[i] is Matrix4x4 m)
                {
                    var factor = ScaleFactor.Factor(pawn);
                    if (!Mathf.Approximately(factor, 1f))
                    {
                        __args[i] = ScaleFactor.Apply(m, pawn);
                        if (Prefs.DevMode && UnityEngine.Random.value < 0.01f) // Log 1% of calls to avoid spam
                            Log.Message($"[GeneScaledWeapons] Scaled matrix: pawn={pawn?.LabelShortCap}, factor={factor:F2}");
                    }
                    break;
                }
            }
        }
    }
}

