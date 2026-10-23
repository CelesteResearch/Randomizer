using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Runtime.Session;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private void LoadStoryHooks()
        {
            LoadStoryWarps();
            LoadStoryInventory();
            LoadStoryQualityOfLife();
            LoadStoryCampfire();
        }

        private void AddStoryProducerHooks(Type owner, Predicate<Instruction> producer, ILContext.Manipulator patch)
        {
            var count = 0;
            foreach (var method in StoryMethods(owner))
            {
                if (method.GetMethodBody() == null) continue;
                using var definition = new DynamicMethodDefinition(method);
                var found = false;
                foreach (var instruction in definition.Definition.Body.Instructions)
                    if (producer(instruction))
                    {
                        found = true;
                        break;
                    }

                if (!found) continue;
                try
                {
                    hooks.Add(new ILHook(method, patch));
                }
                catch (Exception failure)
                {
                    throw new InvalidOperationException(
                        "Randomizer failed real producer " + method.DeclaringType.FullName + "." + method.Name,
                        failure);
                }

                count++;
            }

            if (count == 0)
                throw new InvalidOperationException("Randomizer cannot find the real story producer in " +
                                                    owner.FullName);
        }

        private static IEnumerable<MethodInfo> StoryMethods(Type owner)
        {
            foreach (var method in owner.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                                                    BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.DeclaredOnly))
                yield return method;
            foreach (var nested in owner.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                if (nested.IsDefined(typeof(CompilerGeneratedAttribute), false))
                    foreach (var method in StoryMethods(nested))
                        yield return method;
        }

        private OriginalLevelSceneRoot ResolveStorySessionLevel(ChapterCompletionSession session)
        {
            if (engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot current && Owns(current) &&
                ReferenceEquals(current.Session, session)) return current;
            if (engine.Frame.Scenes.NextScene is OriginalLevelSceneRoot next && Owns(next) &&
                ReferenceEquals(next.Session, session)) return next;
            return null;
        }

        private static OriginalLevelSceneRoot ResolveStoryLevel(object owner)
        {
            if (owner is OriginalLevelSceneRoot level) return level;
            if (owner is OriginalCutsceneEntityState cutscene) return cutscene.Level;
            if (owner is RoomPlayerSession player) return player.Level as OriginalLevelSceneRoot;
            if (owner is SceneEntityState entity) return entity.Scene as OriginalLevelSceneRoot;
            if (owner == null || !owner.GetType().IsDefined(typeof(CompilerGeneratedAttribute), false)) return null;
            // Only generated closures/iterator state machines are traversed. Do not walk engine object graphs.
            foreach (var field in owner.GetType().GetFields(AllInstanceMethods))
            {
                var type = field.FieldType;
                if (!typeof(OriginalLevelSceneRoot).IsAssignableFrom(type) &&
                    !typeof(SceneEntityState).IsAssignableFrom(type) &&
                    !type.IsDefined(typeof(CompilerGeneratedAttribute), false)) continue;
                var child = field.GetValue(owner);
                if (ReferenceEquals(child, owner)) continue;
                var found = ResolveStoryLevel(child);
                if (found != null) return found;
            }

            return null;
        }

        private bool IsRandomizerStory(object owner)
        {
            var level = ResolveStoryLevel(owner);
            return level != null && Owns(level) && Settings(level.Session) != null;
        }

        private static void EmitStoryOwner(ILCursor cursor)
        {
            cursor.Emit(cursor.Context.Method.HasThis ? OpCodes.Ldarg_0 : OpCodes.Ldnull);
        }
    }
}
