using Celeste.Mods.Randomizer;
using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.Generation.Configuration.Metadata;
using Celeste.Mods.Randomizer.Generation.Errors;
using Celeste.Mods.Randomizer.Settings;
using System.Collections;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Game.Menus.Overworld;
using Celeste.Simulation.UI.Menus.Text;

namespace Celeste.Mods.Randomizer.UI
{
    public abstract class GenericOui : OuiState
    {
        protected readonly ISourceEngineContext Engine;
        protected TextMenuPresentation Menu;
        protected Celeste.Scene.Entities.SceneEntityState Content;
        private InputSearchUI searchUI;
        protected RandoModule Module => RandoModule.For(Engine);
        protected RandoSettings Settings => Module.Settings;

        protected GenericOui(ISourceEngineContext engine) : base(engine)
        {
            Engine = engine;
        }

        public override IEnumerator Enter(OuiState from)
        {
            Visible = true;
            Content = ReloadMenu();
            Menu = Content as TextMenuPresentation;
            Overworld.SourceScene.QueueSourceEntity(Content);
            if (Menu != null && this is OuiRandoSettings)
            {
                if (searchUI == null)
                {
                    searchUI = new InputSearchUI(Overworld);
                    Overworld.SourceScene.QueueSourceEntity(searchUI);
                }

                searchUI.RegisterMenuEvents(Menu);
            }

            float offscreen = IsDeeperThan(from) ? 2880f : -2880f;
            for (float p = 0; p < 1; p += Engine.DeltaTime * 4f)
            {
                Content.Position =
                    new Vector2(
                        offscreen + (960 - offscreen) *
                        ((Celeste.Core.Mathematics.Easing.ISourceEasingEnvironment)Engine).EasingArithmetic.CubeOut(p),
                        Content.Position.Y);
                yield return null;
            }

            Content.Position = new Vector2(960, Content.Position.Y);
            Content.Active = true;
        }

        public override IEnumerator Leave(OuiState next)
        {
            Content.Active = false;
            float offscreen = IsDeeperThan(next) ? 2880f : -2880f;
            for (float p = 0; p < 1; p += Engine.DeltaTime * 4f)
            {
                Content.Position =
                    new Vector2(
                        960 + (offscreen - 960) * ((Celeste.Core.Mathematics.Easing.ISourceEasingEnvironment)Engine)
                        .EasingArithmetic.CubeIn(p), Content.Position.Y);
                yield return null;
            }

            searchUI?.Hide();
            Overworld.SourceScene.RemoveEntity(Content);
            Content = null;
            Menu = null;
            Visible = false;
        }

        protected abstract Celeste.Scene.Entities.SceneEntityState ReloadMenu();
        protected virtual bool IsDeeperThan(OuiState other) => !(other is GenericOui);
    }
}
