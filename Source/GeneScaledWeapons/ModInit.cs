using HarmonyLib;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    public class GeneScaledWeaponsMod : Mod
    {
        public static ModSettings_GSW Settings;

        public GeneScaledWeaponsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ModSettings_GSW>();
            GSWLog.LevelSetting = Settings.logLevel;

            var harmony = new Harmony("celphcs30.genescaledweapons");
            bool hasNodes = AccessTools.TypeByName("Verse.PawnRenderNode") != null;
            GSWLog.Trace("Init. PawnRenderNode exists: " + hasNodes);

            int totalPatched = 0;
            if (hasNodes)
            {
                NodeScanUtil.LogPotentialEquipmentNodes();
                Patcher16.Debug_ListRenderNodes();
                totalPatched += Patcher16.Apply(harmony);
                totalPatched += PatcherEquipmentUtility.Apply(harmony);
            }
            else
            {
                totalPatched += LegacyPatcher.Apply(harmony);
            }

            GSWLog.Min($"Patch applied. Methods patched: {totalPatched}");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var l = new Listing_Standard();
            l.Begin(inRect);
            l.Label("Logging level (Verbose/Trace require Dev Mode):");
            foreach (GSWLog.Level lvl in System.Enum.GetValues(typeof(GSWLog.Level)))
            {
                bool selected = Settings.logLevel == lvl;
                if (l.RadioButton(lvl.ToString(), selected))
                {
                    Settings.logLevel = lvl;
                    GSWLog.LevelSetting = lvl;
                }
            }
            l.End();
        }

        public override string SettingsCategory() => "GeneScaledWeapons";
    }
}

