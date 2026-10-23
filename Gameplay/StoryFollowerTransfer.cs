using Celeste.Core.Components;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Following;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.Randomizer.Gameplay
{
    internal sealed class StoryFollowerTransfer
    {
        private readonly OriginalLevelSceneRoot level;
        private readonly RoomPlayerSession player;
        private readonly LeaderState leader;

        public StoryFollowerTransfer(OriginalLevelSceneRoot level)
        {
            this.level = level;
            player = level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession).FullName);
            leader = player.Leader;
            foreach (var follower in leader.Followers)
                if (follower.Entity is ISourceMutableEntityPosition position &&
                    follower.Entity is ISceneEntityTagFields tags)
                {
                    position.Position -= player.Position;
                    tags.Tags |= OriginalSceneTags.Global;
                    level.Session.DoNotLoad.Add(follower.ParentEntityID);
                }

            for (var i = 0; i < leader.PastPoints.Count; i++) leader.PastPoints[i] -= player.Position;
        }

        public void Restore()
        {
            foreach (var follower in leader.Followers)
                if (follower.Entity is ISourceMutableEntityPosition position &&
                    follower.Entity is ISceneEntityTagFields tags)
                {
                    position.Position += player.Position;
                    tags.Tags &= ~OriginalSceneTags.Global;
                    level.Session.DoNotLoad.Remove(follower.ParentEntityID);
                }

            for (var i = 0; i < leader.PastPoints.Count; i++) leader.PastPoints[i] += player.Position;
            leader.TransferFollowers();
        }
    }
}
