using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.Randomizer.Gameplay.Reflection
{
    internal static class RandomizerOwnerAccess
    {
        internal static Func<T, ISourceEngineContext> Create<T>()
        {
            var method = new DynamicMethod("Randomizer.Owner." + typeof(T).Name,
                typeof(ISourceEngineContext), new[] { typeof(T) }, typeof(T), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, typeof(T).GetField("engine", BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Ret);
            return (Func<T, ISourceEngineContext>)method.CreateDelegate(typeof(Func<T, ISourceEngineContext>));
        }
    }
}
