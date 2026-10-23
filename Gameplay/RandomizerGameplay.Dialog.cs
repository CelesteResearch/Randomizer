using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.UI.Dialogue.Textboxes;
using Celeste.Localization.Languages;
using Celeste.Localization.Lookup;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Simulation.Dialogs.Nodes;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private RandoSettings ActiveSettings => engine.SaveContext.CurrentSave?.CurrentSession is { } session
            ? Settings(session)
            : null;

        private void LoadDialogHooks()
        {
            hooks.Add(new Hook(typeof(DialogCatalog).GetMethod(nameof(DialogCatalog.Get)),
                (Func<Func<DialogCatalog, string, DialogLanguage, string>, DialogCatalog, string, DialogLanguage,
                    string>)GetDialog));
            hooks.Add(new Hook(typeof(DialogCatalog).GetMethod(nameof(DialogCatalog.Clean)),
                (Func<Func<DialogCatalog, string, DialogLanguage, string>, DialogCatalog, string, DialogLanguage,
                    string>)CleanDialog));
            hooks.Add(new Hook(typeof(TextboxState).GetConstructor(new[]
                {
                    typeof(string), typeof(ISourceEngineContext),
                    typeof(DialogLanguage), typeof(Func<IEnumerator>[])
                }),
                (Action<Action<TextboxState, string, ISourceEngineContext, DialogLanguage, Func<IEnumerator>[]>,
                    TextboxState, string, ISourceEngineContext, DialogLanguage, Func<IEnumerator>[]>)RandomizeTextbox));
        }

        private string GetDialog(Func<DialogCatalog, string, DialogLanguage, string> orig,
            DialogCatalog catalog, string key, DialogLanguage language)
            => ReferenceEquals(catalog, engine.Dialog)
                ? PlayMadlibs(s => orig(catalog, s, language), key)
                : orig(catalog, key, language);

        private string CleanDialog(Func<DialogCatalog, string, DialogLanguage, string> orig,
            DialogCatalog catalog, string key, DialogLanguage language)
            => ReferenceEquals(catalog, engine.Dialog)
                ? PlayMadlibs(s => orig(catalog, s, language), key)
                : orig(catalog, key, language);

        private string PlayMadlibs(Func<string, string> orig, string key)
        {
            if (!engine.Dialog.Has("RANDO_" + key) || ActiveSettings is not { } settings) return orig(key);
            string text = orig("RANDO_" + key);
            int index = 0;
            while (text.Contains("(RANDO:"))
            {
                int start = text.IndexOf("(RANDO:");
                int descriptionStart = start + "(RANDO:".Length;
                int end = text.IndexOf(')', start);
                string description = text.Substring(descriptionStart, end - descriptionStart);
                text = text.Remove(start, end + 1 - start).Insert(start,
                    MadlibBlank(description, settings.Hash + settings.EndlessLevel, key + (index * 55).ToString()));
                index++;
            }

            return text;
        }

        private string MadlibBlank(string description, string hash, string seed)
        {
            if (description.Contains(":"))
            {
                var split = description.Split(':');
                description = split[0];
                seed = split[1];
            }

            int count;
            try
            {
                count = int.Parse(engine.Dialog.Get("RANDOHEART_" + description + "_COUNT"));
            }
            catch (FormatException)
            {
                throw new Exception("Bad key: RANDOHEART_" + description + "_COUNT");
            }

            var random = new Random((int)RandoSettings.djb2(hash + seed));
            string result = engine.Dialog.Get("RANDOHEART_" + description + "_" + random.Next(count).ToString());
            return char.IsLower(description[0]) ? result.ToLower() : result;
        }

        private void RandomizeTextbox(
            Action<TextboxState, string, ISourceEngineContext, DialogLanguage, Func<IEnumerator>[]> orig,
            TextboxState textbox, string dialog, ISourceEngineContext owner, DialogLanguage language,
            Func<IEnumerator>[] events)
        {
            if (!ReferenceEquals(owner, engine) || ActiveSettings == null ||
                !context.RandomDialogMappings.TryGetValue(dialog.ToLower(), out string replacement))
            {
                orig(textbox, dialog, owner, language, events);
                return;
            }

            var font = ((IOriginalGameTextEnvironment)engine).Text.Fonts.Get(engine.Dialog.Language.FontFace);
            float lineHeight = font.Get(engine.Dialog.Language.FontFaceSize).LineHeight - 1;
            int lines = (int)(240f / lineHeight);
            float padding = (272 - lineHeight * lines) / 2;
            int width = (int)(1688 - padding * 2 - 240 - 32);
            var original = OriginalFancyTextParser.Parse(engine.Dialog.Get(dialog, language), width, lines, 0f, null,
                OriginalFancyTextContext.Create((IOriginalFancyTextProcess)engine, language));
            var triggers = original.Nodes.OfType<DialogTrigger>().ToList();
            orig(textbox, replacement, owner, language, events);
            int index = 0;
            foreach (var trigger in textbox.Nodes.OfType<DialogTrigger>())
            {
                if (index >= triggers.Count) trigger.Index = -1;
                else
                {
                    trigger.Index = triggers[index].Index;
                    trigger.Label = triggers[index].Label;
                    trigger.Silent = triggers[index].Silent;
                    index++;
                }
            }

            if (index < triggers.Count) textbox.Nodes.AddRange(triggers.GetRange(index, triggers.Count - index));
        }
    }
}
