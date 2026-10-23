using System.Collections.Generic;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Construction
{
    public sealed class GenerationEntitySpec
    {
        public string Name;
        public int ID;
        public LevelData Level;
        public Vector2 Position;
        public int Width;
        public int Height;
        public Vector2[] Nodes;
        public Dictionary<string, object> Values;
    }
}
