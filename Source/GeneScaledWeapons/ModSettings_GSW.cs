using Verse;

namespace GeneScaledWeapons
{
    public class ModSettings_GSW : ModSettings
    {
        public GSWLog.Level logLevel = GSWLog.Level.Minimal;
        public bool scaleRanged = true;
        public bool scaleMelee = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref logLevel, "logLevel", GSWLog.Level.Minimal);
            Scribe_Values.Look(ref scaleRanged, "scaleRanged", true);
            Scribe_Values.Look(ref scaleMelee, "scaleMelee", false);
        }
    }
}
