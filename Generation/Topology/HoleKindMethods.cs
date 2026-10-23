using System;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Topology
{
    public static class HoleKindMethods
    {
        public static HoleKind FromString(String str)
        {
            switch (str.ToLower())
            {
                case "none":
                    return HoleKind.None;
                case "in":
                    return HoleKind.In;
                case "out":
                    return HoleKind.Out;
                case "inout":
                    return HoleKind.InOut;
                case "unknown":
                    return HoleKind.Unknown;
                default:
                    throw new Exception("Bad hole kind " + str);
            }
        }
    }
}
