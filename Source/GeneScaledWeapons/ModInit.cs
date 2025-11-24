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
            int count = Patcher.Apply(h);
            Log.Message($"[GeneScaledWeapons] Patched {count} method(s).");
        }
    }

    public class GeneScaledWeaponsMod : Mod
    {
        public GeneScaledWeaponsMod(ModContentPack pack) : base(pack) { }
    }
}

