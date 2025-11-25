using HarmonyLib;
using Verse;

namespace GeneScaledWeapons.Patches
{
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras))]
    internal static class Patch_DrawExtrasCtx
    {
        static void Prefix(Pawn pawn) => RenderCtx.CurrentPawn = pawn;
        static void Finalizer() => RenderCtx.CurrentPawn = null;
    }
}

