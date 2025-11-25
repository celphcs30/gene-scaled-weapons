using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    [HarmonyPatch]
    internal static class Patch_GenDraw_Draw
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var methods = typeof(GenDraw).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == "DrawMeshNowOrLater"
                    && m.GetParameters().Any(p => p.ParameterType == typeof(Matrix4x4)))
                .ToList();

            // Optional: one-time info to confirm we actually patched something
            if (!logged)
            {
                logged = true;
                Log.Message($"[GeneScaledWeapons] GenDraw.DrawMeshNowOrLater overloads to patch: {methods.Count}");
            }

            return methods;
        }

        static bool logged;

        // Robust prefix: scan for Matrix4x4 arg and scale it
        static void Prefix(object[] __args)
        {
            var pawn = RenderCtx.CurrentPawn;
            if (pawn == null || __args == null) return;

            for (int i = 0; i < __args.Length; i++)
            {
                if (__args[i] is Matrix4x4 m)
                {
                    __args[i] = ScaleFactor.Apply(m, pawn);
                    break;
                }
            }
        }
    }
}

