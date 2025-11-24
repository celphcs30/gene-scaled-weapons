using HarmonyLib;
using Verse;

namespace GeneScaledWeapons
{
    [StaticConstructorOnStartup]
    public static class ModInit
    {
        static ModInit()
        {
            new Harmony("celphcs30.genescaledweapons").PatchAll();
            Log.Message("[GeneScaledWeapons] Harmony patched.");
        }
    }

    // Optional settings shell (expand later if you want UI)
    public class GeneScaledWeaponsMod : Mod
    {
        public GeneScaledWeaponsMod(ModContentPack pack) : base(pack) { }
    }
}

