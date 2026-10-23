using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public class Possible : Requirement
    {
        public override bool Able(Capabilities state)
        {
            return true;
        }

        public override int Complexity()
        {
            return 0;
        }

        public override bool Equals(Requirement other)
        {
            if (!(other is Possible i))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return 2222;
        }

        public override bool StrictlyBetterThan(Requirement other)
        {
            return true;
        }
    }
}
