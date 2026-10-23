using System.Diagnostics;

namespace Celeste.Mods.Randomizer.Generation.Diagnostics
{
    public static class GenerationDiagnostics
    {
        public static void Log(string tag, string message) => Trace.WriteLine(message, tag);
        public static void Log(GenerationLogLevel level, string tag, string message) => Trace.WriteLine(message, tag);
    }
}
