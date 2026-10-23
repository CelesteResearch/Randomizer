using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public class Capabilities
    {
        public NumDashes Dashes;
        public NumDashes RefillDashes;
        public Difficulty PlayerSkill;
        public bool HasKey;
        public bool BypassFlags;
        public FlagSet Flags;

        public FlagState GetFlag(string name)
        {
            return this.Flags.TryGetValue(name, out var result) ? result : FlagState.Unset;
        }

        public Capabilities Copy()
        {
            return new Capabilities
            {
                Dashes = this.Dashes,
                RefillDashes = this.RefillDashes,
                PlayerSkill = this.PlayerSkill,
                HasKey = this.HasKey,
                Flags = this.Flags,
            };
        }

        public Capabilities WithoutKey()
        {
            var result = this.Copy();
            result.HasKey = false;
            return result;
        }

        public Capabilities WithFlags(FlagSet flags)
        {
            var result = this.Copy();
            result.Flags = flags;
            return result;
        }

        public Capabilities WithoutFlags()
        {
            var result = this.Copy();
            result.BypassFlags = true;
            return result;
        }
    }
}
