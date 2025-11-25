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
            if (Settings != null)
            {
                GSWLog.LevelSetting = Settings.logLevel;
            }

            var harmony = new Harmony("celphcs30.genescaledweapons");

            // Remove any old transpilers/attribute patches for DrawEquipment... and PawnRenderer/Nodes
            // Apply manual patchers instead:
            int ctx = EquipmentContextPatcher.Apply(harmony);
            int draw = GenDrawPatcher.Apply(harmony);

            GSWLog.Min($"GeneScaledWeapons: Patch applied. Context={ctx}, GenDraw={draw}");
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

        public override string SettingsCategory() => "Gene Scaled Weapons";
    }
}

