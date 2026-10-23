using System;
using System.Reflection;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Rendering;
using Celeste.Everest.UI.ModOptions;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Menus.Overworld;
using Celeste.Game.UI.Controls;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Simulation.UI.Menus.Text;

namespace Celeste.Mods.Randomizer.UI
{
    public sealed class InputSearchUI : SceneEntityState
    {
        private static readonly FieldInfo OverworldInputEase =
            typeof(OverworldSequence).GetField("inputEase", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly OverworldSequence overworld;
        private readonly WigglerComponent wiggler;
        private bool showSearchUI;
        private float wiggleDelay, inputEase;

        public InputSearchUI(OverworldSequence overworld) : base(
            ((ISourceEntitySourceDataEnvironment)overworld.EngineContext).EntitySourceDataRegistration)
        {
            this.overworld = overworld;
            var engine = overworld.EngineContext;
            var tags = ((IOriginalGameTagsEnvironment)engine).Tags;
            Tags = tags.HUD.Value | tags.PauseUpdate.Value;
            Depth = -10000;
            Components.Add(wiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(0.4f, 4f));
        }

        public void RegisterMenuEvents(TextMenuPresentation menu)
        {
            showSearchUI = true;
            Action search = EverestModOptionsState.AddSearchBox(menu, overworld);
            menu.Remove(menu.Items[menu.Items.Count - 1]);
            menu.OnClose += Hide;
            menu.OnUpdate += () =>
            {
                if (((IOriginalGameInputProcessEnvironment)overworld.EngineContext).InputSource.QuickRestart
                    .Pressed) search();
            };
        }

        public void Hide() => showSearchUI = false;

        public override void Update()
        {
            var engine = overworld.EngineContext;
            if (((IOriginalGameInputProcessEnvironment)engine).InputSource.QuickRestart.Pressed && wiggleDelay <= 0)
            {
                wiggler.Start();
                wiggleDelay = 0.5f;
            }

            wiggleDelay -= engine.DeltaTime;
            inputEase = ScalarMath.Approach(inputEase, showSearchUI ? 1 : 0, engine.DeltaTime * 4,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            base.Update();
        }

        public override void Render(ISourceRenderContext context)
        {
            if (inputEase <= 0) return;
            var engine = overworld.EngineContext;
            var key = ((IOriginalGameInputProcessEnvironment)engine).InputSource.QuickRestart;
            float offset = (float)OverworldInputEase.GetValue(overworld) > 0 ? 48 : 0;
            string label = ((IOriginalGameTextEnvironment)engine).Text.Dialog.Clean("MAPLIST_SEARCH");
            float width = OriginalButtonUi.Width(engine, label, key);
            var position = new Vector2(1880, 1024 - offset);
            position.X += (40 + width * 0.5f + 32) *
                          (1 - ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeOut(inputEase));
            OriginalButtonUi.Render(context, this, engine, position, label, key, 0.5f, 1, wiggler.Value * 0.05f);
        }
    }
}
