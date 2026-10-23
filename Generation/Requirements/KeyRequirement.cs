using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public class KeyRequirement : Requirement
    {
        public readonly int KeyholeID;

        public KeyRequirement(int keyholeID)
        {
            this.KeyholeID = keyholeID;
        }

        public override bool Able(Capabilities state)
        {
            return state.HasKey;
        }

        public override bool Equals(Requirement other)
        {
            if (!(other is KeyRequirement i))
            {
                return false;
            }

            return this.KeyholeID == i.KeyholeID;
        }

        public override int GetHashCode()
        {
            return 5555;
        }
    }
}
