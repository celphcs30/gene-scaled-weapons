using Verse;

namespace GeneScaledWeapons
{
    public class ModSettings_GSW : ModSettings
    {
        public GSWLog.Level logLevel = GSWLog.Level.Minimal;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref logLevel, "logLevel", GSWLog.Level.Minimal);
        }
    }
}
