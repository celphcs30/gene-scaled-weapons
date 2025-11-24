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

            int totalPatched = 0;
            if (hasNodes)
            {
#if DEBUG
                NodeScanUtil.LogPotentialEquipmentNodes();
                Patcher16.Debug_ListRenderNodes();
#endif
                totalPatched += Patcher16.Apply(harmony);
                totalPatched += PatcherEquipmentUtility.Apply(harmony);
            }
            else
            {
                totalPatched += LegacyPatcher.Apply(harmony);
            }

            Log.Message($"[GeneScaledWeapons] Patch applied. Methods patched: {totalPatched}");
        }
    }
}

