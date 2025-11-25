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
                // Patch GenDraw.DrawMeshNowOrLater
                var genDrawTargets = typeof(GenDraw).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(m => m.Name == "DrawMeshNowOrLater"
                        && m.GetParameters().Any(p => p.ParameterType == typeof(Matrix4x4)))
                    .ToArray();

                var prefix = new HarmonyMethod(typeof(GenDrawPatcher).GetMethod(nameof(ScaleMatrixPrefix), BindingFlags.NonPublic | BindingFlags.Static));

                foreach (var m in genDrawTargets)
                {
                    harmony.Patch(m, prefix, null);
                    patched++;
                }

                // Also patch Graphics.DrawMesh directly (equipment might use this)
                var graphicsTargets = typeof(Graphics).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == "DrawMesh"
                        && m.GetParameters().Any(p => p.ParameterType == typeof(Matrix4x4)))
                    .ToArray();

                foreach (var m in graphicsTargets)
                {
                    harmony.Patch(m, prefix, null);
                    patched++;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[GeneScaledWeapons] Failed to patch draw methods: {e}");
            }

            Log.Message($"[GeneScaledWeapons] Draw methods patched: {patched}");
            return patched;
        }

        // Generic prefix: locate the Matrix4x4 arg in __args and scale it if we are in equipment context
        private static void ScaleMatrixPrefix(object[] __args)
        {
            var pawn = RenderCtx.CurrentPawn;
            if (pawn == null || __args == null) return;

            bool foundMatrix = false;
            for (int i = 0; i < __args.Length; i++)
            {
                if (__args[i] is Matrix4x4 m)
                {
                    foundMatrix = true;
                    var factor = ScaleFactor.Factor(pawn);
                    if (!Mathf.Approximately(factor, 1f))
                    {
                        __args[i] = ScaleFactor.Apply(m, pawn);
                        // Log occasionally to confirm it's working
                        if (Prefs.DevMode && UnityEngine.Random.value < 0.05f) // Log 5% of calls
                            Log.Message($"[GeneScaledWeapons] Scaled matrix: pawn={pawn?.LabelShortCap}, factor={factor:F2}");
                    }
                    break;
                }
            }

            // Debug: log if we're called but no matrix found, or if pawn is null
            if (Prefs.DevMode && UnityEngine.Random.value < 0.001f) // Very rare log
            {
                if (pawn == null)
                    Log.Warning($"[GeneScaledWeapons] GenDraw called but no pawn context");
                else if (!foundMatrix)
                    Log.Warning($"[GeneScaledWeapons] GenDraw called with pawn={pawn?.LabelShortCap} but no Matrix4x4 in args");
            }
        }
    }
}

