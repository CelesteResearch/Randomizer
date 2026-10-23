using Celeste.Game.Execution.Areas;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Simulation.Runtime.Session;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private ChapterCompletionSession cachedSettingsSession;
        private int cachedSettingsArea, cachedSettingsMode;
        private RandoSettings cachedSettings;
        private bool settingsCached;

        public void InvalidateSettingsCache()
        {
            settingsCached = false;
            cachedSettingsSession = null;
            cachedSettings = null;
        }

        private RandoSettings ResolveSettings(ChapterCompletionSession session)
        {
            if (settingsCached && ReferenceEquals(cachedSettingsSession, session)
                               && cachedSettingsArea == session.AreaID &&
                               cachedSettingsMode == session.Mode) return cachedSettings;
            cachedSettingsSession = session;
            cachedSettingsArea = session.AreaID;
            cachedSettingsMode = session.Mode;
            cachedSettings = context.GetArea(new OriginalAreaKey { ID = session.AreaID, Mode = session.Mode }) is
                { } area
                ? context.State(area).Get<RandoSettings>("RandoSettings")
                : null;
            settingsCached = true;
            return cachedSettings;
        }
    }
}
