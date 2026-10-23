using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Celeste.Core.Reflection.Interop;
using Celeste.Game.Execution.Areas;
using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.Interop;

namespace Celeste.Mods.Randomizer
{
    public partial class RandoModule
    {
        private readonly List<IDisposable> interopLeases = new List<IDisposable>();
        private readonly VariantResetImports variantResetImports = new VariantResetImports();
        private GenerationInterop generationExports;


        private void LoadInterop()
        {
            var process = ((ISourceModInteropEnvironment)SourceEngine).ModInterop;
            generationExports = new GenerationInterop(StartInteropGeneration, LaunchIntoRandoArea,
                settings =>
                {
                    SavedData.SavedSettings = settings;
                    SaveSettings();
                },
                () => RetireMenuGeneration(false));
            interopLeases.Add(process.RegisterOwned(
                new SourceModInteropActualTypeBinding(typeof(VariantResetImports), variantResetImports)));
            interopLeases.Add(process.RegisterOwned(
                new SourceModInteropActualTypeBinding(typeof(SettingsInterop), new SettingsInterop(() => Context))));
            interopLeases.Add(
                process.RegisterOwned(
                    new SourceModInteropActualTypeBinding(typeof(GenerationInterop), generationExports)));
        }

        internal void UpdateInterop() => generationExports?.Update();

        private Task<OriginalAreaKey> StartInteropGeneration(RandoSettings settings)
        {
            if (GenerationTask != null) return null;
            EnsureGeneratedContent();
            var cancellation = new CancellationTokenSource();
            GenerationCancellation = cancellation;
            Context.CancellationToken = cancellation.Token;
            settings.Context = Context;
            GenerationTask = Task.Run(() =>
            {
                settings.Enforce();
                return new RandoLogic(Context).GenerateMap(settings);
            }, cancellation.Token);
            return GenerationTask;
        }

        private void UnloadInterop()
        {
            generationExports?.Unload();
            generationExports = null;
            List<Exception> failures = null;
            for (int i = interopLeases.Count - 1; i >= 0; i--)
            {
                try
                {
                    interopLeases[i].Dispose();
                }
                catch (Exception failure)
                {
                    (failures ??= new List<Exception>()).Add(failure);
                }
            }

            interopLeases.Clear();
            if (failures != null) throw new AggregateException("Randomizer interop cleanup failed.", failures);
        }
    }
}
