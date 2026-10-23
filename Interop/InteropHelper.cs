using System;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;

namespace Celeste.Mods.Randomizer.Interop
{
    internal static class InteropHelper
    {
        internal static RandoSettings Settings(object value) => value as RandoSettings ??
                                                                throw new ArgumentException(
                                                                    $"Randomizer.Interop: received an object of type '{value?.GetType().Name ?? "null"}', expected {nameof(RandoSettings)}");
    }
}
