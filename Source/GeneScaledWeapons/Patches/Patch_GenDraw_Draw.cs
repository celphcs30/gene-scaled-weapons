using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons.Patches
{
    [HarmonyPatch]
    internal static class Patch_GenDraw_Draw
    {
        // Patch every overload of DrawMeshNowOrLater that takes a Matrix4x4
        static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(GenDraw).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == "DrawMeshNowOrLater"
                    && m.GetParameters().Any(p => p.ParameterType == typeof(Matrix4x4)));
        }

        // Matrix4x4 is arg index 1 in all these overloads
        static void Prefix([HarmonyArgument(1)] ref Matrix4x4 loc)
        {
            var pawn = RenderCtx.CurrentPawn;
            if (pawn == null) return;
            loc = ScaleFactor.MultiplyMatrix(loc, pawn);
        }
    }
}

