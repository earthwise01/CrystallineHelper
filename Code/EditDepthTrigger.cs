using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace vitmod {
    [CustomEntity("vitellary/editdepthtrigger")]
    public class EditDepthTrigger : Trigger {
        public EditDepthTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            newDepth = data.Int("depth", 0);
            affectedTypes = TypeHelper.ParseTypeList(data.Attr("entitiesToAffect", ""));
            debug = data.Bool("debug", false);
            initializeInAwake = data.Bool("initializeInAwake", false);
            updateOnEntry = data.Bool("updateOnEntry", false);
            if (updateOnEntry && data.Bool("cacheValidEntities", false)) {
                validEntitiesCache = new();
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            if (!initializeInAwake) {
                HandleEntitiesOnLoad();
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (initializeInAwake) {
                HandleEntitiesOnLoad();
            }
        }

        public override void Update() {
            base.Update();

            if (!updateOnEntry) {
                return;
            }

            if (validEntitiesCache is { } cache) {
                foreach (Entity entity in cache) {
                    HandleEntity(entity, fillCache: false);
                }
            } else {
                foreach (Entity entity in Scene.Entities) {
                    HandleEntity(entity, fillCache: false);
                }
            }
        }

        private void HandleEntitiesOnLoad() {
            foreach (Entity entity in Scene.Entities) {
                HandleEntity(entity, fillCache: true);

                if (debug && entity.CollideCheck(this)) {
                    Logger.Info("CrystallineHelper/EditDepthTrigger", $"{entity.SourceData?.Name ?? "[N/A]"} / {entity.GetType().FullName}: {entity.Depth}");
                }
            }
        }

        private void HandleEntity(Entity entity, bool fillCache) {
            if (affectedTypes.Contains(entity)) {
                if (fillCache) {
                    validEntitiesCache?.Add(entity);
                }

                if (entity.CollideCheck(this)) {
                    entity.Depth = newDepth;
                }
            }
        }

        private readonly TypeHelper.TypeAndSidList affectedTypes;
        private readonly List<Entity> validEntitiesCache;

        private readonly int newDepth;
        private readonly bool debug;
        private readonly bool initializeInAwake;
        private readonly bool updateOnEntry;
    }
}
