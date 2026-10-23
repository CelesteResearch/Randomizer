using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public class FlagRequirement : Requirement
    {
        public readonly string Flag;
        public readonly bool Set;

        public FlagRequirement(string Flag, bool Set)
        {
            this.Flag = Flag;
            this.Set = Set;
        }

        public override bool Able(Capabilities state)
        {
            if (state.BypassFlags)
            {
                return true;
            }

            var stateFlag = state.GetFlag(this.Flag);
            if (this.Set)
            {
                return stateFlag == FlagState.Set || stateFlag == FlagState.UnsetToSet || stateFlag == FlagState.Both;
            }
            else
            {
                return stateFlag == FlagState.Unset || stateFlag == FlagState.SetToUnset || stateFlag == FlagState.Both;
            }
        }

        public override bool Equals(Requirement other)
        {
            if (!(other is FlagRequirement i))
            {
                return false;
            }

            return this.Flag == i.Flag && this.Set == i.Set;
        }

        public override int GetHashCode()
        {
            var result = 6666;
            foreach (var ch in this.Flag)
            {
                result ^= ch;
                result = (result << 10) | (result >> 22);
            }

            result ^= this.Set ? 1234 : 5678;
            return result;
        }
    }
}
