using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Levels;

namespace Celeste.Mods.Randomizer.Runtime
{
    internal sealed class GeneratedMapLoadPolicy : IOriginalChapterMapLoadPolicy
    {
        public void Load(OriginalChapterMapData map)
        {
            // Upstream menuLifecycle.DontLoadRandoMaps deliberately preserves generated content on Load/Reload.
            // Generated maps have no backing map file and must not consume another generation seed here.
        }

        public OriginalLevelRoomData Get(OriginalChapterMapData map, string levelName)
        {
            foreach (var level in map.Levels)
                if (level.Name == levelName)
                    return level;
            return null;
        }
    }
}
