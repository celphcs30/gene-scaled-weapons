using Verse;

namespace GeneScaledWeapons
{
    public static class GSWLog
    {
        public enum Level { Off, Minimal, Verbose, Trace }
        public static Level LevelSetting = Level.Minimal;

        static string P(string msg) => "[GeneScaledWeapons] " + msg;

        public static void Min(string msg)
        {
            if (LevelSetting >= Level.Minimal) Log.Message(P(msg));
        }

        public static void Verb(string msg)
        {
            if (Prefs.DevMode && LevelSetting >= Level.Verbose) Log.Message(P(msg));
        }

        public static void Trace(string msg)
        {
            if (Prefs.DevMode && LevelSetting >= Level.Trace) Log.Message(P(msg));
        }

        public static void WarnOnce(string msg, int key)
        {
            if (LevelSetting != Level.Off) Log.WarningOnce(P(msg), key);
        }

        public static void Error(string msg)
        {
            if (LevelSetting != Level.Off) Log.Error(P(msg));
        }
    }
}
