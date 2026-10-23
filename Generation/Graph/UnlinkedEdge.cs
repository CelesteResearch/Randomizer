using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Graph
{
    public class UnlinkedEdge
    {
        public readonly StaticEdge Static;
        public readonly LinkedNode Node;

        public UnlinkedEdge(LinkedNode node, StaticEdge edge)
        {
            this.Static = edge;
            this.Node = node;
        }

        public override string ToString()
        {
            return $"{this.Static}@{this.Node.Room.Position}";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UnlinkedEdge e))
            {
                return false;
            }

            return this.Static == e.Static && this.Node == e.Node;
        }

        public override int GetHashCode()
        {
            return 1234 ^ this.Static.GetHashCode() ^ this.Node.GetHashCode();
        }
    }
}
