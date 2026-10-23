using Celeste.Game.Content.Maps.Levels;
using Celeste.Savestates;

namespace Celeste.Mods.Randomizer.Runtime
{
    public sealed partial class EngineGenerationContext
    {
        public override OriginalLevelRoomData CloneRoom(OriginalLevelRoomData source)
        {
            // Clone the actual parsed graph, retaining post-parse edits, aliases, nested mod attributes,
            // and entity-to-room cycles. Never re-read Source. The reference map belongs only to this copy.
            return new StateClone().Clone(source);
        }
    }
}
