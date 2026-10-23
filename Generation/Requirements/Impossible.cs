using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public class Impossible : Requirement
    {
        public override bool Able(Capabilities state)
        {
            return false;
        }

        public override int Complexity()
        {
            return int.MaxValue;
        }

        public override bool Equals(Requirement other)
        {
            if (!(other is Impossible i))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return 1111;
        }

        public override bool StrictlyBetterThan(Requirement other)
        {
            return false;
        }
    }
}
