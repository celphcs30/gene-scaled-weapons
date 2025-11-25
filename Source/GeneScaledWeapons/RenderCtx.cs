using Verse;

namespace GeneScaledWeapons
{
    internal static class RenderCtx
    {
        [System.ThreadStatic]
        public static Pawn CurrentPawn;
    }
}

