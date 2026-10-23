using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.State
{
    public sealed class GenerationAttachedState
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();
        public void Set<T>(string key, T value) => values[key] = value;
        public T Get<T>(string key) => values.TryGetValue(key, out object value) ? (T)value : default;
    }
}
