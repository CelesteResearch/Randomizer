using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Celeste.Mods.Randomizer.Generation
{
    public partial class RandoLogic
    {
        public List<StaticRoom> AllRooms => Context.AllRooms;
        public Dictionary<string, List<AreaKey>> LevelSets => Context.LevelSets;
        public Dictionary<RandoSettings.AreaKeyNotStupid, int> LevelCount => Context.LevelCount;
        public HashSet<RandoSettings.AreaKeyNotStupid> LazyLoaded => Context.LazyLoaded;

        public List<Hole> FindHoles(LevelData data)
        {
            List<Hole> result = new List<Hole>();
            Regex regex = new Regex("\\r\\n|\\n\\r|\\n|\\r");
            string[] lines = regex.Split(data.Solids);

            // returns whether the given tile is clear
            bool lookup(int x, int y)
            {
                if (y >= lines.Length)
                {
                    return true;
                }

                string line = lines[y];
                if (x >= line.Length)
                {
                    return true;
                }

                return line[x] == '0';
            }

            int curHole = -1;
            bool clear;
            //GenerationDiagnostics.Log("findholes", $"{data.TileBounds.Width} x {data.TileBounds.Height} => {data.Solids.Length}");
            //GenerationDiagnostics.Log("findholes", $"\n{data.Solids}");
            for (int i = 0; i < data.TileBounds.Width; i++)
            {
                clear = lookup(i, 0);
                if (clear && curHole == -1)
                {
                    curHole = i;
                }
                else if (!clear && curHole != -1)
                {
                    result.Add(new Hole(ScreenDirection.Up, curHole, i - 1, false));
                    curHole = -1;
                }
            }

            if (curHole != -1)
            {
                result.Add(new Hole(ScreenDirection.Up, curHole, data.TileBounds.Width - 1, true));
            }

            curHole = -1;
            for (int i = 0; i < data.TileBounds.Height; i++)
            {
                clear = lookup(data.TileBounds.Width - 1, i);
                if (clear && curHole == -1)
                {
                    curHole = i;
                }
                else if (!clear && curHole != -1)
                {
                    result.Add(new Hole(ScreenDirection.Right, curHole, i - 1, false));
                    curHole = -1;
                }
            }

            if (curHole != -1)
            {
                result.Add(new Hole(ScreenDirection.Right, curHole, data.TileBounds.Height - 1, true));
            }

            curHole = -1;
            for (int i = 0; i < data.TileBounds.Height; i++)
            {
                clear = lookup(0, i);
                if (clear && curHole == -1)
                {
                    curHole = i;
                }
                else if (!clear && curHole != -1)
                {
                    result.Add(new Hole(ScreenDirection.Left, curHole, i - 1, false));
                    curHole = -1;
                }
            }

            if (curHole != -1)
            {
                result.Add(new Hole(ScreenDirection.Left, curHole, data.TileBounds.Height - 1, true));
            }

            curHole = -1;
            for (int i = 0; i < data.TileBounds.Width; i++)
            {
                clear = lookup(i, data.TileBounds.Height - 1);
                if (clear && curHole == -1)
                {
                    curHole = i;
                }
                else if (!clear && curHole != -1)
                {
                    result.Add(new Hole(ScreenDirection.Down, curHole, i - 1, false));
                    curHole = -1;
                }
            }

            if (curHole != -1)
            {
                result.Add(new Hole(ScreenDirection.Down, curHole, data.TileBounds.Width - 1, true));
            }

            return result;
        }

        private List<StaticRoom> ProcessMap(RandoConfigFile configFile, MapData map, List<RandoConfigRoom> config)
        {
            var result = new List<StaticRoom>();
            var resultMap = new Dictionary<string, StaticRoom>();
            var levelMapping = new Dictionary<string, LevelData>();
            foreach (LevelData level in map.Levels)
            {
                if (level.Dummy)
                {
                    continue;
                }

                levelMapping[level.Name] = level;
            }

            foreach (var roomConfig in config)
            {
                if (!levelMapping.TryGetValue(roomConfig.Room, out var level))
                {
                    throw new Exception(
                        $"Nonexistent room {roomConfig.Room} referenced by randomizer config {configFile.ConfigPath} in {configFile.ModName}");
                }

                var holes = Context.Generator.FindHoles(level);
                var room = new StaticRoom(Context, map.Area, configFile, roomConfig, level, holes);
                result.Add(room);
                if (resultMap.ContainsKey(level.Name))
                {
                    resultMap[level.Name] = null;
                }
                else
                {
                    resultMap[level.Name] = room;
                }
            }

            foreach (var room in result)
            {
                room.ProcessWarps(resultMap);
            }

            return result;
        }

        private void ProcessArea(AreaData area)
        {
            RandoConfigFile config = Context.LoadConfig(area, Context.LazyLoading);
            if (config == null)
            {
                return;
            }

            for (int i = 0; i < area.Mode.Length; i++)
            {
                if (area.Mode[i] == null)
                {
                    continue;
                }

                var mapConfig = config.GetRooms((int)i);
                if (mapConfig == null)
                {
                    continue;
                }

                // Mark map as available
                // Use SID (minus level name) for levelsets to avoid collisions
                AreaKey key = Context.Key(area, i);
                string SID = Context.GetArea(key).SID;
                string levelSetID = SID.Substring(0, SID.LastIndexOf('/'));
                if (Context.LevelSets.TryGetValue(levelSetID, out var keyList))
                {
                    keyList.Add(key);
                }
                else
                {
                    Context.LevelSets.Add(levelSetID, new List<AreaKey> { key });
                }

                if (mapConfig.Count == 0)
                {
                    LazyLoaded.Add(new RandoSettings.AreaKeyNotStupid(Context, key));
                }
                else
                {
                    Context.AllRooms.AddRange(ProcessMap(config, area.Mode[i].MapData, mapConfig));
                }
            }
        }

        private void LazyReload(IEnumerable<AreaKey> keys)
        {
            foreach (var key in keys)
            {
                Context.CheckCancellation();
                var notstupid = new RandoSettings.AreaKeyNotStupid(Context, key);
                if (LazyLoaded.Contains(notstupid))
                {
                    LazyLoaded.Remove(notstupid);
                    var configfile = Context.ReloadConfig(key);
                    var mapping = configfile.GetRooms(key.Mode);
                    Context.AllRooms.AddRange(ProcessMap(configfile, Context.GetArea(key).Mode[(int)key.Mode].MapData,
                        mapping));
                }
            }

            CountRooms();
        }

        private void CountRooms()
        {
            Context.LevelCount.Clear();
            foreach (var room in Context.AllRooms)
            {
                var notstupid = new RandoSettings.AreaKeyNotStupid(Context, room.Area);
                if (Context.LevelCount.TryGetValue(notstupid, out int c))
                {
                    Context.LevelCount[notstupid] = c + 1;
                }
                else
                {
                    Context.LevelCount[notstupid] = 1;
                }
            }

            foreach (var notstupid in LazyLoaded)
            {
                var stupid = Context.Key(Context.GetArea(notstupid.SID), notstupid.Mode);
                var count = Context.GetArea(stupid).Mode[(int)stupid.Mode].MapData.Levels.Count;
                if (Context.LevelCount.TryGetValue(notstupid, out int c))
                {
                    Context.LevelCount[notstupid] = c + count;
                }
                else
                {
                    Context.LevelCount[notstupid] = count;
                }
            }
        }

        public void ProcessAreas()
        {
            if (Context.AllRooms.Count != 0)
            {
                return;
            }

            GenerationDiagnostics.Log("randomizer", "Processing level data...");
            foreach (var area in Context.GenerationAreas)
            {
                Context.CheckCancellation();
                ProcessArea(area);
            }

            CountRooms();
        }
    }
}
