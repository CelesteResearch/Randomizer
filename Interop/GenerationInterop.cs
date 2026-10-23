using System;
using System.Threading.Tasks;
using Celeste.Game.Execution.Areas;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using MonoMod.ModInterop;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.Randomizer.Interop
{
    [RuntimeTypeAlias("Celeste.Mod.Randomizer.Interoperability.GenerationInterop")]
    [ModExportName("Randomizer.GenerationInterop")]
    public sealed class GenerationInterop
    {
        private readonly Func<RandoSettings, Task<OriginalAreaKey>> start;
        private readonly Action<OriginalAreaKey> launch;
        private readonly Action<RandoSettings> persist;
        private readonly Action retire;
        private Task<OriginalAreaKey> task;
        private RandoSettings settings;
        private OriginalAreaKey? generatedArea;
        private bool running;

        public GenerationInterop(Func<RandoSettings, Task<OriginalAreaKey>> start, Action<OriginalAreaKey> launch,
            Action<RandoSettings> persist, Action retire)
        {
            this.start = start;
            this.launch = launch;
            this.persist = persist;
            this.retire = retire;
        }

        public bool Generate(object settings)
        {
            var actual = InteropHelper.Settings(settings);
            var started = start(actual);
            if (started == null) return false;
            task = started;
            this.settings = actual;
            running = true;
            generatedArea = null;
            return true;
        }

        public bool GenerationInProgress() => running;
        public bool ReadyToLaunch() => generatedArea.HasValue;
        public OriginalAreaKey? GetGeneratedArea() => generatedArea;

        public bool EnterGeneratedArea()
        {
            if (!generatedArea.HasValue) return false;
            launch(generatedArea.Value);
            generatedArea = null;
            return true;
        }

        // The original Builder.Check runs on the game thread and persists settings for every terminal status.
        internal void Update()
        {
            if (task == null || !task.IsCompleted) return;
            // Save failures propagate before retiring the original builder, so the next update can retry.
            persist(settings);
            var completed = task;
            if (completed.Status == TaskStatus.RanToCompletion) generatedArea = completed.Result;
            else _ = completed.Exception;
            task = null;
            settings = null;
            running = false;
            retire();
        }

        internal void Unload()
        {
            task = null;
            settings = null;
            generatedArea = null;
            running = false;
        }
    }
}
