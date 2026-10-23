using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public enum FlagState
    {
        Set,
        Unset,
        Both,
        One,
        SetToUnset,
        UnsetToSet,
    }
}
