using Celeste.Mods.Randomizer;
using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.Generation.Configuration.Metadata;
using Celeste.Mods.Randomizer.Generation.Errors;
using Celeste.Mods.Randomizer.Settings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Celeste.Core.Scheduling;
using Celeste.Everest.UI.Settings;
using Celeste.Game.Menus.Overworld;
using Celeste.Game.Menus.Title;
using Celeste.Simulation.UI.Menus.Text;

namespace Celeste.Mods.Randomizer.UI
{
    public sealed partial class OuiRandoSettings : GenericOui
    {
        private enum OptionsPages
        {
            Ruleset,
            Basic,
            Levels,
            Advanced,
            Cosmetic,
            Last
        }

        private OptionsPages page = OptionsPages.Basic;

        private readonly List<TextMenuPresentation.Item>[] pages =
        {
            new List<TextMenuPresentation.Item>(), new List<TextMenuPresentation.Item>(),
            new List<TextMenuPresentation.Item>(), new List<TextMenuPresentation.Item>(),
            new List<TextMenuPresentation.Item>()
        };

        private readonly List<Action> synchronizers = new List<Action>();
        private TextMenuPresentation.Button startButton;
        private EverestMenuEaseInSubHeaderExt hashText;
        private EverestMenuEaseInSubHeaderExt errorText;
        private Task<AreaKey> generation;
        private CancellationTokenSource cancellation;
        private RandoModule generationOwner;
        private bool entering;
        private int pageIndex;
        private float journalEase;

        public OuiRandoSettings(ISourceEngineContext engine) : base(engine)
        {
        }

        public override bool IsStart(OverworldSequence overworld, OverworldStartMode start)
        {
            if (start != (OverworldStartMode)55) return false;
            OwnedComponents.Add(new SimulationCoroutine(Engine, Enter(null)));
            return true;
        }

        public override void Render(Celeste.Core.Rendering.ISourceRenderContext context)
        {
            base.Render(context);
            if (journalEase <= 0) return;
            float ease =
                ((Celeste.Core.Mathematics.Easing.ISourceEasingEnvironment)Engine).EasingArithmetic
                .CubeOut(journalEase);
            var position = new System.Numerics.Vector2(128 * ease, 952);
            var color = Celeste.Core.Display.PackedColor.White * ease;
            var gui = ((Celeste.Graphics.Content.Gui.ISourceGuiAtlasResources)Engine).GuiAtlas;
            Celeste.Graphics.Rendering.SourceAtlasDrawing.DrawCentered(context, this, gui["menu/journal"], position,
                color);
            var input = ((Celeste.Game.Controls.Source.IOriginalGameInputProcessEnvironment)Engine).InputSource;
            Celeste.Graphics.Rendering.SourceAtlasDrawing.Draw(context, this, input.GuiButton(input.MenuJournal),
                position, System.Numerics.Vector2.Zero, color);
        }

        protected override bool IsDeeperThan(OuiState other) =>
            !(other is OuiRandoRecords) && !(other is EverestTextEntryState);

        protected override Celeste.Scene.Entities.SceneEntityState ReloadMenu()
        {
            page = OptionsPages.Basic;
            foreach (var items in pages) items.Clear();
            synchronizers.Clear();
            var menu = new DisablableTextMenu(Engine);
            menu.Add(new TextMenuPresentation.Header(Module.Clean("MODOPTIONS_RANDOMIZER_HEADER")));
            startButton = new TextMenuPresentation.Button(Module.Clean("MODOPTIONS_RANDOMIZER_START"));
            startButton.Pressed(GenerateOrCancel);
            hashText = new EverestMenuEaseInSubHeaderExt("", true, menu)
                { HeightExtra = -10f, Offset = new System.Numerics.Vector2(30, -5) };
            errorText = new EverestMenuEaseInSubHeaderExt("", false, menu)
                { HeightExtra = -10f, Offset = new System.Numerics.Vector2(30, -5) };
            menu.Add(startButton);
            menu.Add(hashText);
            menu.Add(errorText);
            var pageToggle = new TextMenuPresentation.Slider(Module.Clean("MODOPTIONS_RANDOMIZER_OPTIONS"),
                i => Module.Clean("MODOPTIONS_RANDOMIZER_OPTIONS_" + ((OptionsPages)i).ToString().ToUpperInvariant()),
                0, (int)OptionsPages.Last - 1, (int)page);
            pageToggle.Change(i =>
            {
                page = (OptionsPages)i;
                Synchronize();
                menu.Position = new System.Numerics.Vector2(menu.Position.X, menu.ScrollTargetY);
            });
            menu.Add(pageToggle);
            pageIndex = menu.Items.IndexOf(pageToggle);
            BuildBasic();
            BuildMaps();
            BuildRules();
            BuildAdvanced();
            BuildCosmetic();
            foreach (var items in pages)
            foreach (var item in items)
                menu.Add(item);
            menu.OnCancel = () =>
            {
                if (generation != null || entering) return;
                if (menu.Selection > pageIndex)
                {
                    menu.Selection = pageIndex;
                    Module.Play("event:/ui/main/button_back");
                    return;
                }

                Module.PersistSettings();
                Module.Play("event:/ui/main/button_back");
                Overworld.Goto<MainMenuSequence>();
            };
            menu.OnPause = () =>
            {
                if (!entering && generation == null)
                {
                    Module.Play("event:/ui/main/button_select");
                    menu.Selection = 1;
                    menu.Current.OnPressed();
                }
            };
            menu.Selection = 1;
            Menu = menu;
            Synchronize();
            return menu;
        }

        private void BuildBasic()
        {
            var seed = new TextMenuPresentation.Button(
                Module.Clean("MODOPTIONS_RANDOMIZER_SEED") + ": " + Settings.Seed);
            seed.Pressed(() =>
            {
                Module.Play("event:/ui/main/savefile_rename_start");
                Overworld.Goto<EverestTextEntryState>().Init<OuiRandoSettings>(Settings.Seed,
                    value => Settings.Seed = value, RandoModule.MAX_SEED_CHARS);
            });
            pages[1].Add(seed);
            synchronizers.Add(() =>
            {
                seed.Visible &= Settings.SeedType == SeedType.Custom;
                seed.Label = Module.Clean("MODOPTIONS_RANDOMIZER_SEED") + ": " + Settings.Seed;
            });
            AddEnum(1, "SEEDTYPE", () => Settings.SeedType, value => Settings.SeedType = value, (int)SeedType.Last,
                false);
            AddEnum(1, "LENGTH", () => Settings.Length, value => Settings.Length = value, (int)MapLength.Last);
            AddEnum(1, "NUMDASHES", () => Settings.Dashes, value => Settings.Dashes = value, (int)NumDashes.Last);
            AddEnum(1, "LOGIC", () => Settings.Algorithm, value => Settings.Algorithm = value, (int)LogicType.Last);
            var lives = new TextMenuPresentation.Slider(Module.Clean("MODOPTIONS_RANDOMIZER_LIVES"),
                i => i == 0 ? Module.Clean("MODOPTIONS_RANDOMIZER_LIVES_INFINITE") : i.ToString(), 0, 50,
                Settings.EndlessLives);
            lives.Change(value => Settings.EndlessLives = value);
            pages[1].Add(lives);
            synchronizers.Add(() =>
            {
                lives.Index = Settings.EndlessLives;
                lives.Visible &= Settings.Algorithm == LogicType.Endless;
                lives.Disabled = Locked;
            });
            AddEnum(1, "DIFFICULTY", () => Settings.Difficulty, value => Settings.Difficulty = value,
                (int)Difficulty.Last);
        }

        private bool Locked => !string.IsNullOrEmpty(Settings.Rules);

        private void AddEnum<T>(int target, string key, Func<T> get, Action<T> set, int count,
            bool lockWithRules = true) where T : struct, Enum
        {
            var slider = new TextMenuPresentation.Slider(Module.Clean("MODOPTIONS_RANDOMIZER_" + key),
                i => Module.Clean("MODOPTIONS_RANDOMIZER_" + key + "_" + Enum.GetName(typeof(T), i).ToUpperInvariant()),
                0, count - 1, Convert.ToInt32(get()));
            slider.Change(i =>
            {
                set((T)Enum.ToObject(typeof(T), i));
                Synchronize();
            });
            pages[target].Add(slider);
            synchronizers.Add(() =>
            {
                slider.Index = Convert.ToInt32(get());
                slider.Disabled = lockWithRules && Locked;
            });
        }

        private void AddToggle(int target, string key, Func<bool> get, Action<bool> set, bool lockWithRules = true)
        {
            var toggle = new TextMenuPresentation.OnOff(Module.Clean("MODOPTIONS_RANDOMIZER_" + key), get(), Engine);
            toggle.Change(value =>
            {
                set(value);
                Synchronize();
            });
            pages[target].Add(toggle);
            synchronizers.Add(() =>
            {
                toggle.Index = get() ? 1 : 0;
                toggle.Disabled = lockWithRules && Locked;
            });
        }

        private void BuildAdvanced()
        {
            AddEnum(3, "STRAWBERRIES", () => Settings.Strawberries, value => Settings.Strawberries = value,
                (int)StrawberryDensity.Last);
            AddEnum(3, "DIFFICULTYCURVE", () => Settings.DifficultyEagerness,
                value => Settings.DifficultyEagerness = value, (int)DifficultyEagerness.Last);
            AddToggle(3, "REPEATROOMS", () => Settings.RepeatRooms, value => Settings.RepeatRooms = value);
            AddToggle(3, "ENTERUNKNOWN", () => Settings.EnterUnknown, value => Settings.EnterUnknown = value);
            AddToggle(3, "GOLDENBERRY", () => Settings.SpawnGolden, value => Settings.SpawnGolden = value, false);
            AddToggle(3, "VARIANTS", () => Settings.Variants, value => Settings.Variants = value);
        }

        private void BuildCosmetic()
        {
            AddEnum(4, "SHINE", () => Settings.Lights, value => Settings.Lights = value, (int)ShineLights.Last);
            AddEnum(4, "DARK", () => Settings.Darkness, value => Settings.Darkness = value, (int)Darkness.Last);
            AddToggle(4, "DECORATIONS", () => Settings.RandomDecorations, value => Settings.RandomDecorations = value,
                false);
            AddToggle(4, "COLORS", () => Settings.RandomColors, value => Settings.RandomColors = value, false);
            AddToggle(4, "BACKGROUNDS", () => Settings.RandomBackgrounds, value => Settings.RandomBackgrounds = value,
                false);
        }

        private void BuildRules()
        {
            var custom = new EverestMenuButtonExt(Module.Clean("MODOPTIONS_RANDOMIZER_RULES_CUSTOM"));
            custom.Pressed(() =>
            {
                Settings.Rules = "";
                Synchronize();
            });
            pages[0].Add(custom);
            synchronizers.Add(() => custom.Icon = Settings.Rules == "" ? "menu/poemarrow" : "");
            var rules = new List<RandoMetadataRuleset>(Module.Context.Metadata.Rulesets);
            rules.Sort((a, b) => a.Name.CompareTo(b.Name));
            foreach (var rule in rules)
            {
                var button = new EverestMenuButtonExt(rule.LongName);
                button.Pressed(() =>
                {
                    Settings.Rules = rule.Name;
                    Settings.Enforce();
                    Synchronize();
                });
                pages[0].Add(button);
                synchronizers.Add(() => button.Icon = Settings.Rules == rule.Name ? "menu/poemarrow" : "");
            }
        }

        private void Synchronize()
        {
            for (int i = 0; i < pages.Length; i++)
                foreach (var item in pages[i])
                    item.Visible = i == (int)page;
            foreach (var sync in synchronizers) sync();

            hashText.Title = "v" + Module.VersionString +
                             (Settings.SeedType == SeedType.Custom ? " #" + Settings.Hash : "");
            if (generation != null) hashText.Title += " " + Module.Clean("MODOPTIONS_RANDOMIZER_GENERATING");
            Menu?.RecalculateSize();
        }

        private void GenerateOrCancel()
        {
            if (entering) return;
            if (generation == null && Module.GenerationTask != null)
            {
                Module.GenerationCancellation.Cancel();
                return;
            }

            if (generation != null)
            {
                cancellation.Cancel();
                return;
            }

            Settings.EndlessLevel = 0;
            Settings.Enforce();
            errorText.FadeVisible = false;
            cancellation = new CancellationTokenSource();
            generationOwner = Module;
            generationOwner.GenerationCancellation = cancellation;
            Module.Context.CancellationToken = cancellation.Token;
            var settings = Settings.Copy();
            settings.Context = Module.Context;
            generation = Task.Run(() => new RandoLogic(settings.Context).GenerateMap(settings), cancellation.Token);
            generationOwner.GenerationTask = generation;
            startButton.Label = Module.Clean("MODOPTIONS_RANDOMIZER_CANCEL");
            ((DisablableTextMenu)Menu).DisableMovement = true;
            Synchronize();
        }

        public override void SceneEnd(object scene)
        {
            generationOwner?.RetireMenuGeneration(true);
            generationOwner = null;
            generation = null;
            cancellation = null;
            base.SceneEnd(scene);
        }

        public override void Update()
        {
            base.Update();
            journalEase = Celeste.Core.Mathematics.ScalarMath.Approach(journalEase, Menu?.Active == true ? 1 : 0,
                Engine.DeltaTime * 4,
                ((Celeste.Core.Mathematics.Extrema.ISourceExtremaEnvironment)Engine).ExtremaArithmetic);
            if (generation == null)
            {
                if (Menu?.Focused == true && !entering &&
                    ((Celeste.Game.Controls.Source.IOriginalGameInputProcessEnvironment)Engine).InputSource.MenuJournal
                    .Pressed)
                {
                    Module.Play("event:/ui/world_map/journal/select");
                    Overworld.Goto<OuiRandoRecords>();
                }

                return;
            }

            if (!generation.IsCompleted) return;
            var completed = generation;
            generation = null;
            try
            {
                var area = completed.GetAwaiter().GetResult();
                entering = true;
                Module.LaunchIntoRandoArea(area);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception error)
            {
                entering = false;
                errorText.Title = error is GenerationError
                    ? error.Message
                    : "Encountered an error - Check log.txt for details";
                errorText.FadeVisible = true;
                Module.LogError(error);
            }
            finally
            {
                generationOwner.RetireMenuGeneration(false);
                generationOwner = null;
                cancellation = null;
                startButton.Label = Module.Clean("MODOPTIONS_RANDOMIZER_START");
                ((DisablableTextMenu)Menu).DisableMovement = false;
                Synchronize();
            }
        }
    }
}
