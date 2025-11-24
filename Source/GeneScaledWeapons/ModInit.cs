using HarmonyLib;
using Verse;

namespace GeneScaledWeapons
{
    [StaticConstructorOnStartup]
    public static class ModInit
    {
        static ModInit()
        {
            var h = new Harmony("celphcs30.genescaledweapons");
            int count = 0;

            // Try 1.6 render node system first
            var renderNodeType = AccessTools.TypeByName("Verse.PawnRenderNode");
            if (renderNodeType != null)
            {
                count = Patcher16.Apply(h);
                // Only use old patches as fallback if 1.6 patch found nothing
                if (count == 0)
                {
                    count = Patcher.Apply(h, suppressWarnings: true);
                }
            }
            else
            {
                // Older RimWorld version - use legacy patches
                count = Patcher.Apply(h, suppressWarnings: false);
            }

            if (count > 0)
            {
                Log.Message($"[GeneScaledWeapons] Successfully patched {count} method(s).");
            }
        }
    }

    public class GeneScaledWeaponsMod : Mod
    {
        public GeneScaledWeaponsMod(ModContentPack pack) : base(pack) { }
    }
}

