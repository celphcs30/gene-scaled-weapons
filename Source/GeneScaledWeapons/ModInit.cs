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

            // Patch DrawEquipmentAndApparelExtras (1.6 method) - always try this first
            var drawExtrasType = AccessTools.TypeByName("Verse.PawnRenderUtility");
            var drawExtrasMethod = drawExtrasType != null ? AccessTools.Method(drawExtrasType, "DrawEquipmentAndApparelExtras") : null;
            int drawExtrasPatched = 0;
            if (drawExtrasMethod != null)
            {
                try
                {
                    var prefixMethod = AccessTools.Method(typeof(Patches.Patch_DrawExtras), "Prefix");
                    var finalizerMethod = AccessTools.Method(typeof(Patches.Patch_DrawExtras), "Finalizer");
                    var transpilerMethod = AccessTools.Method(typeof(Patches.Patch_DrawExtras), "Transpiler");
                    harmony.Patch(drawExtrasMethod,
                        prefix: prefixMethod != null ? new HarmonyMethod(prefixMethod) : null,
                        finalizer: finalizerMethod != null ? new HarmonyMethod(finalizerMethod) : null,
                        transpiler: transpilerMethod != null ? new HarmonyMethod(transpilerMethod) : null);
                    drawExtrasPatched = 1;
                    GSWLog.Trace("Patched PawnRenderUtility.DrawEquipmentAndApparelExtras");
                }
                catch (System.Exception e)
                {
                    GSWLog.WarnOnce($"Failed to patch DrawEquipmentAndApparelExtras: {e}", drawExtrasMethod.GetHashCode());
                }
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

