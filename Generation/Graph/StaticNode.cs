using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Graph
{
    public class StaticNode
    {
        public string Name;
        public List<StaticEdge> Edges = new List<StaticEdge>();
        public List<StaticCollectable> Collectables = new List<StaticCollectable>();
        public StaticRoom ParentRoom;
        public List<RandoConfigInternalEdge> WarpConfig = new List<RandoConfigInternalEdge>();
        public List<Tuple<string, bool>> FlagSetters = new List<Tuple<string, bool>>();

        public override string ToString()
        {
            return $"{this.ParentRoom}:{this.Name}";
        }

        public StaticEdge WarpEdge => new StaticEdge
        {
            FromNode = this,
            ReqIn = new Possible(),
            ReqOut = new Possible(),
        };
    }
}
