using HarmonyLib;
using Verse;

namespace GeneScaledWeapons
{
    public class GeneScaledWeaponsMod : Mod
    {
        public GeneScaledWeaponsMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("celphcs30.genescaledweapons");
            bool hasNodes = AccessTools.TypeByName("Verse.PawnRenderNode") != null;
            Log.Message("[GeneScaledWeapons] Init. PawnRenderNode exists: " + hasNodes);

            if (hasNodes)
            {
#if DEBUG
                Patcher16.Debug_ListRenderNodes();
#endif
                int n = Patcher16.Apply(harmony);
                Log.Message("[GeneScaledWeapons] 1.6 patch applied. Methods patched: " + n);
            }
            else
            {
                int n = LegacyPatcher.Apply(harmony);
                Log.Message("[GeneScaledWeapons] Legacy patch applied. Methods patched: " + n);
            }
        }
    }
}

