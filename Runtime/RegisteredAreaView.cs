using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Everest.Content.Maps.Registry;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Runtime;

namespace Celeste.Mods.Randomizer.Runtime
{
    internal sealed class RegisteredAreaView : IList<EverestArea>
    {
        private readonly GameApplication application;

        public RegisteredAreaView(GameApplication application) => this.application = application;

        public int Count => application.AreaRegistry.Areas.Count;
        public bool IsReadOnly => true;

        public EverestArea this[int index]
        {
            get => (EverestArea)application.AreaRegistry.Areas[index];
            set => throw new NotSupportedException();
        }

        public IEnumerator<EverestArea> GetEnumerator()
            => Enumerate(application.AreaRegistry.Areas).GetEnumerator();

        private static IEnumerable<EverestArea> Enumerate(List<OriginalAreaData> areas)
        {
            foreach (var area in areas) yield return (EverestArea)area;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int IndexOf(EverestArea item)
        {
            var areas = application.AreaRegistry.Areas;
            for (int index = 0; index < areas.Count; index++)
                if (EqualityComparer<EverestArea>.Default.Equals((EverestArea)areas[index], item))
                    return index;
            return -1;
        }

        public bool Contains(EverestArea item) => IndexOf(item) >= 0;

        public void CopyTo(EverestArea[] array, int arrayIndex)
        {
            var areas = application.AreaRegistry.Areas;
            for (int index = 0; index < areas.Count; index++) array[arrayIndex + index] = (EverestArea)areas[index];
        }

        public void Add(EverestArea item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public void Insert(int index, EverestArea item) => throw new NotSupportedException();
        public bool Remove(EverestArea item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
    }
}
