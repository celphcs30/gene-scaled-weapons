using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace GeneScaledWeapons
{
    [HarmonyPatch]
    internal static class Patch_PawnRenderer_DrawEqCtx
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            // Cover both aiming and non-aiming equipment draws
            return typeof(PawnRenderer).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == "DrawEquipment" || m.Name == "DrawEquipmentAiming");
        }

        static readonly System.Reflection.FieldInfo FI_Pawn = AccessTools.Field(typeof(PawnRenderer), "pawn");

        static void Prefix(object __instance)
        {
            var pawn = (Pawn)FI_Pawn.GetValue(__instance);
            RenderCtx.CurrentPawn = pawn;
        }

        static void Finalizer() => RenderCtx.CurrentPawn = null;
    }
}

