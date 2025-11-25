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
            bool hasNodes = AccessTools.TypeByName("Verse.PawnRenderNode") != null;
            GSWLog.Trace("Init. PawnRenderNode exists: " + hasNodes);

            // Patch DrawEquipmentAndApparelExtras context (1.6 method) - always try this first
            int drawExtrasPatched = 0;
            try
            {
                harmony.CreateClassProcessor(typeof(Patches.Patch_DrawExtrasCtx)).Patch();
                harmony.CreateClassProcessor(typeof(Patches.Patch_GenDraw_Draw)).Patch();
                drawExtrasPatched = 1;
                GSWLog.Trace("Patched DrawEquipmentAndApparelExtras context and GenDraw.DrawMeshNowOrLater");
            }
            catch (System.Exception e)
            {
                GSWLog.WarnOnce($"Failed to patch DrawEquipmentAndApparelExtras/GenDraw: {e}", typeof(Patches.Patch_DrawExtrasCtx).GetHashCode());
            }

            int rnPatched = 0;
            int classicPatched = 0;
            if (hasNodes)
            {
                NodeScanUtil.LogPotentialEquipmentNodes();
                Patcher16.Debug_ListRenderNodes();
                rnPatched = Patcher16.Apply(harmony);
                classicPatched = PatcherEquipmentUtility.Apply(harmony);
            }
            else
            {
                classicPatched = LegacyPatcher.Apply(harmony);
            }

            int totalPatched = rnPatched + classicPatched + drawExtrasPatched;

            if (drawExtrasPatched > 0)
            {
                GSWLog.Min("GeneScaledWeapons: Patched DrawEquipmentAndApparelExtras (1.6 method)");
            }

            if (rnPatched == 0 && classicPatched > 0)
            {
                // Normal on vanilla 1.6; keep quiet by default.
                GSWLog.Verb("1.6: No render-node weapon hooks found; using classic draw hooks.");
            }
            else if (totalPatched == 0)
            {
                GSWLog.Error("GeneScaledWeapons: No weapon draw hooks patched. Scaling cannot run.");
            }

            GSWLog.Min($"GeneScaledWeapons: Patch applied. Methods patched: DrawExtras={drawExtrasPatched}, RN={rnPatched}, classic={classicPatched}, total={totalPatched}");
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
            l.Gap();
            l.CheckboxLabeled("Scale ranged weapons", ref Settings.scaleRanged);
            l.CheckboxLabeled("Scale melee weapons", ref Settings.scaleMelee);
            l.GapLine();
            l.CheckboxLabeled("Debug: Force scaling (ignore blacklists and settings)", ref Settings.debugForceScale);
            l.End();
        }

        public override string SettingsCategory() => "Gene Scaled Weapons";
    }
}

