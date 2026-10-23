using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public class DashRequirement : Requirement
    {
        public readonly NumDashes Dashes;

        public DashRequirement(NumDashes dashes)
        {
            this.Dashes = dashes;
        }

        public override bool Able(Capabilities state)
        {
            return state.Dashes >= this.Dashes;
        }

        public override Requirement Conflicts(Capabilities state)
        {
            if (this.Able(state))
            {
                return new Possible();
            }

            if (state.Dashes == NumDashes.Zero || true)
            {
                // TODO actually think about this
                return new Impossible();
            }

            return this;
        }

        public override bool Equals(Requirement other)
        {
            if (!(other is DashRequirement i))
            {
                return false;
            }

            return this.Dashes == i.Dashes;
        }

        public override int GetHashCode()
        {
            return 3333 ^ (int)this.Dashes;
        }

        public override bool StrictlyBetterThan(Requirement other)
        {
            if (base.StrictlyBetterThan(other))
            {
                return true;
            }

            if (other is DashRequirement d)
            {
                return this.Dashes < d.Dashes;
            }

            return false;
        }
    }
}
