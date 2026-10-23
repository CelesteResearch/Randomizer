using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Graph
{
    public class UnlinkedCollectable
    {
        public StaticCollectable Static;
        public LinkedNode Node;

        public override bool Equals(object obj)
        {
            if (!(obj is UnlinkedCollectable col))
            {
                return false;
            }

            return this.Static == col.Static && this.Node == col.Node;
        }

        public override int GetHashCode()
        {
            return 8765 ^ this.Static.GetHashCode() ^ this.Node.GetHashCode();
        }

        public override string ToString()
        {
            return this.Static.ToString();
        }
    }
}
