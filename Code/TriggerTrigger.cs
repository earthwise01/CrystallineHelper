using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Collections.Generic;

namespace vitmod {
    [CustomEntity("vitellary/triggertrigger")]
    [Tracked(false)]
    public class TriggerTrigger : Trigger {
        public TriggerTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            nodes = data.NodesOffset(offset);
            oneUse = data.Bool("oneUse", false);
            activationType = data.Enum("activationType", ActivationTypes.Flag);
            if (data.Has("invertFlag")) {
                invertCondition = data.Bool("invertFlag", false);
            } else {
                invertCondition = data.Bool("invertCondition", false);
            }
            comparisonType = data.Enum("comparisonType", ComparisonTypes.EqualTo);
            absoluteValue = data.Bool("absoluteValue", false);

            flag = data.Attr("flag", "");
            deaths = data.Int("deaths", -1);
            dashCount = data.Int("dashCount", 0);
            requiredSpeed = data.Float("requiredSpeed", 0f);
            waitTime = data.Float("timeToWait", 0f);
            coreMode = data.Enum("coreMode", Session.CoreModes.None);
            inputType = data.Enum("inputType", InputTypes.Grab);
            if (data.Has("holdInput")) {
                inputHeld = data.Bool("holdInput");
            } else {
                // account for previous behavior:
                // if it's a direction, use whether the direction is held
                // otherwise, just check if it's pressed
                switch (inputType) {
                    case InputTypes.Left:
                    case InputTypes.Right:
                    case InputTypes.Up:
                    case InputTypes.Down:
                        inputHeld = true;
                        break;
                    default:
                        inputHeld = false;
                        break;
                }
            }
            excludeTalkers = data.Bool("excludeTalkers", false);
            ifSafe = data.Bool("onlyIfSafe", false);
            includeCoyote = data.Bool("includeCoyote", false);
            includeWalljump = data.Bool("includeWallJump", false);
            resetAfterJump = data.Bool("resetAfterJump", false);
            playerState = data.Int("playerState", 0);
            collideTypes = TypeHelper.ParseTypeList(activationType != ActivationTypes.OnSolid
                ? !string.IsNullOrEmpty(data.Attr("entityType", ""))
                    ? data.Attr("entityType")
                    : data.Attr("entityTypeToCollide", "Celeste.Strawberry")
                : data.Attr("solidType", ""));
            onEntityCollideCount = data.Int("collideCount", 1);
            entitiesInside = new List<Entity>();
            Add(new HoldableCollider((Holdable holdable) => {
                if (activationType == ActivationTypes.OnHoldableEnter) {
                    entitiesInside.Add(holdable.Entity);
                }
            }));
            if (activationType == ActivationTypes.OnInteraction) {
                Add(talker = new TalkComponent(
                    new Rectangle(0, 0, (int)Width, (int)Height),
                    new Vector2(data.Int("talkBubbleX", (int)Width / 2), data.Int("talkBubbleY", 0)),
                    (player) => {
                        externalActivation = true;
                    }
                ) {
                    PlayerMustBeFacing = false
                });
            }

            delay = data.Float("delay", 0f);
            randomize = data.Bool("randomize", false);
            global = data.Bool("activateOnTransition", false);
            if (data.Has("requirePlayerInside")) {
                global = !data.Bool("requirePlayerInside", true);
            }
            if (bypassGlobal.Contains(activationType)) {
                global = true;
            }
            matchPosition = data.Bool("matchPosition", true);
            onlyOnEnter = data.Bool("onlyOnEnter", false);

            Add(new TransitionListener {
                OnOut = (f) => DeactivateTriggers(Scene?.Tracker.GetEntity<Player>())
            });
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            triggers = new List<Trigger>();
            foreach (Vector2 node in nodes) {
                var lastCollideable = new Dictionary<Trigger, bool>();
                foreach (Trigger trig in scene.Tracker.GetEntities<Trigger>()) {
                    lastCollideable.Add(trig, trig.Collidable);
                    trig.Collidable = true;
                }

                var trigger = scene.CollideFirst<Trigger>(node);

                foreach (Trigger trig in scene.Tracker.GetEntities<Trigger>()) {
                    trig.Collidable = lastCollideable[trig];
                }

                if (trigger == null) {
                    trigger = scene.Tracker.GetNearestEntity<Trigger>(node);
                }
                if (trigger != this && trigger != null) {
                    triggers.Add(trigger);
                    trigger.Collidable = false;
                }
            }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            if (!global) {
                TryActivate(player);
                if (activated && oneUse) {
                    TryRemove();
                }
            }
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (!global) {
                TryDeactivate(player, false);
            }
        }

        public override void Update() {
            base.Update();

            var player = Scene.Tracker.GetEntity<Player>();

            if (player == null) {
                return;
            }

            UpdateConditions(player);

            if (!onlyOnEnter) {
                if (global || PlayerIsInside) {
                    TryActivate(player);
                    TryDeactivate(player, true);
                } else {
                    TryDeactivate(player, false);
                }
            }

            if (activated) {
                if (oneUse) {
                    TryRemove();
                } else {
                    UpdateTriggers(player);
                }
            }

            if (resetActivation) {
                externalActivation = false;
                resetActivation = false;
            }
        }

        public void UpdateConditions(Player player) {
            if (player.Speed == Vector2.Zero) {
                hasWaited += Engine.DeltaTime;
            } else {
                hasWaited = 0f;
            }

            List<Entity> entitiesToRemove = new List<Entity>();
            foreach (Entity entity in entitiesInside) {
                if (!entity.CollideCheck(this)) {
                    entitiesToRemove.Add(entity);
                }
            }
            foreach (Entity entity in entitiesToRemove) {
                entitiesInside.Remove(entity);
            }
        }

        public void TryActivate(Player player) {
            if (activating || (activated && !deactivating))
                return;

            if (GetActivateCondition(player)) {
                if (delay > 0f) {
                    activating = true;
                    Add(Alarm.Create(Alarm.AlarmMode.Oneshot, () => {
                        activating = false;
                        ActivateTriggers(player);
                    }, delay, true));
                } else {
                    ActivateTriggers(player);
                }
            }
        }

        public void TryDeactivate(Player player, bool inside) {
            if (deactivating || (!activated && !activating))
                return;

            if (!inside || !GetActivateCondition(player)) {
                if (delay > 0f) {
                    deactivating = true;
                    Add(Alarm.Create(Alarm.AlarmMode.Oneshot, () => {
                        deactivating = false;
                        DeactivateTriggers(player);
                    }, delay, true));
                } else {
                    DeactivateTriggers(player);
                }
            }
        }

        public bool GetActivateCondition(Player player) {
            if (externalActivation) {
                return !invertCondition;
            }

            bool result = false;
            switch (activationType) {
                case ActivationTypes.Flag:
                    result = string.IsNullOrEmpty(flag) || SceneAs<Level>().Session.GetFlag(flag);
                    break;
                case ActivationTypes.Dashing:
                    result = player.DashAttacking;
                    break;
                case ActivationTypes.DashCount:
                    result = Compare(player.Dashes, dashCount);
                    break;
                case ActivationTypes.DeathsInRoom:
                    result = Compare(SceneAs<Level>().Session.DeathsInCurrentLevel, deaths);
                    break;
                case ActivationTypes.DeathsInLevel:
                    result = Compare(SceneAs<Level>().Session.Deaths, deaths);
                    break;
                case ActivationTypes.OnHoldableEnter:
                    result = entitiesInside.Count > 0;
                    break;
                case ActivationTypes.GrabHoldable:
                    result = player.Holding != null && player.Holding.IsHeld;
                    break;
                case ActivationTypes.SpeedX:
                    result = Compare(player.Speed.X, requiredSpeed);
                    break;
                case ActivationTypes.SpeedY:
                    result = Compare(player.Speed.Y, requiredSpeed);
                    break;
                case ActivationTypes.Jumping:
                    result = player.AutoJumpTimer > 0f;
                    break;
                case ActivationTypes.Crouching:
                    result = player.Ducking;
                    break;
                case ActivationTypes.TimeSinceMovement:
                    result = Compare(hasWaited, waitTime);
                    break;
                case ActivationTypes.CoreMode:
                    result = player.SceneAs<Level>().CoreMode == coreMode;
                    break;
                case ActivationTypes.OnEntityCollide:
                    result = Compare(onEntityCollidedCount, onEntityCollideCount);
                    break;
                case ActivationTypes.OnSolid:
                    Rectangle playerCollision = player.Collider.Bounds;
                    playerCollision.Inflate(1, 3);
                    foreach (Solid solid in Scene.CollideAll<Solid>(playerCollision)) {
                        if ((collideTypes.IsEmpty || collideTypes.Contains(solid)) && player.IsRiding(solid)) {
                            result = true;
                            break;
                        }
                    }
                    if (collideTypes.IsEmpty && player.OnGround()) {
                        result = true;
                    }
                    break;
                case ActivationTypes.OnEntityEnter:
                    foreach (Entity entity in Scene.Entities) {
                        if (entity.CollideCheck(this) && collideTypes.Contains(entity)) {
                            result = true;
                            break;
                        }
                    }
                    break;
                case ActivationTypes.OnInput:
                    if (inputType != InputTypes.Any) {
                        result = CheckInput(inputType, inputHeld);
                    } else {
                        foreach (InputTypes input in Enum.GetValues<InputTypes>()) {
                            if (CheckInput(input, inputHeld)) {
                                result = true;
                                break;
                            }
                        }
                    }
                    break;
                case ActivationTypes.OnGrounded:
                    result = player.OnGround();
                    if (ifSafe)
                        result &= player.OnSafeGround;
                    if (includeCoyote)
                        result |= player.jumpGraceTimer > 0f;
                    break;
                case ActivationTypes.OnPlayerState:
                    result = player.StateMachine.State == playerState;
                    break;
                default:
                    result = false;
                    break;
            }

            if (invertCondition) {
                result = !result;
            }

            return result;
        }

        private bool Compare(float a, float b) {
            if (absoluteValue) {
                a = Math.Abs(a);
                b = Math.Abs(b);
            }
            switch (comparisonType) {
                case ComparisonTypes.LessThan:
                    return a < b;
                case ComparisonTypes.GreaterThan:
                    return a > b;
                default:
                    return a == b;
            }
        }

        private void CleanTriggers() {
            triggers.RemoveAll((trigger) => trigger.Scene == null);

            if (chosenTrigger?.Scene == null) {
                chosenTrigger = null;
            }
        }

        private void ActivateTriggers(Player player) {
            DeactivateTriggers(player);
            CleanTriggers();

            activated = true;

            if (!randomize) {
                foreach (Trigger trigger in triggers) {
                    if (trigger.PlayerIsInside) {
                        trigger.OnLeave(player);
                    }
                    trigger.OnEnter(player);
                }
            } else if (triggers.Count > 0) {
                Calc.PushRandom(VitModule.GetSeed(SceneAs<Level>()));
                chosenTrigger = Calc.Choose(Calc.Random, triggers);
                Calc.PopRandom();

                if (chosenTrigger.PlayerIsInside) {
                    chosenTrigger.OnLeave(player);
                }
                chosenTrigger.OnEnter(player);
            }
        }

        private void DeactivateTriggers(Player player) {
            CleanTriggers();

            activated = false;

            foreach (Trigger trigger in triggers) {
                if (trigger.PlayerIsInside) {
                    trigger.OnLeave(player);
                }
            }

            chosenTrigger = null;
        }

        private void UpdateTriggers(Player player) {
            CleanTriggers();

            foreach (Trigger trigger in triggers) {
                if (matchPosition) {
                    if (!global) {
                        trigger.Position = Position;
                        trigger.Collider.Width = Width;
                        trigger.Collider.Height = Height;
                    } else {
                        var level = SceneAs<Level>();
                        trigger.Position = new Vector2(level.Bounds.X, level.Bounds.Y);
                        trigger.Collider.Width = level.Bounds.Width;
                        trigger.Collider.Height = level.Bounds.Height;
                    }
                }

                if (!randomize) {
                    trigger.OnStay(player);
                }
            }

            chosenTrigger?.OnStay(player);
        }

        private void TryRemove() {
            switch (activationType) {
                case ActivationTypes.OnInteraction:
                    Collidable = false;
                    Add(new Coroutine(InteractExit()));
                    break;
                default:
                    RemoveSelf();
                    break;
            }
        }

        private IEnumerator InteractExit() {
            if (talker == null) {
                RemoveSelf();
                yield break;
            }
            talker.Enabled = false;
            while (talker.UI.slide > 0) {
                yield return null;
            }
            RemoveSelf();
            yield break;
        }

        private bool CheckInput(InputTypes inputType, bool held) {
            switch (inputType) {
                case InputTypes.Grab:
                    return (held ? Input.Grab.Check : Input.Grab.Pressed);
                case InputTypes.Jump:
                    return (held ? Input.Jump.Check : Input.Jump.Pressed);
                case InputTypes.Dash:
                    return (held ? Input.Dash.Check : Input.Dash.Pressed);
                case InputTypes.Interact:
                    if (excludeTalkers && TalkComponent.PlayerOver != null) {
                        return false;
                    }
                    return (held ? Input.Talk.Check : Input.Talk.Pressed);
                case InputTypes.CrouchDash:
                    return (held ? Input.CrouchDash.Check : Input.CrouchDash.Pressed);
                default:
                    Vector2 aim = Input.Aim.Value.FourWayNormal();
                    if (held || (aim != Input.Aim.PreviousValue.FourWayNormal())) {
                        if (inputType == InputTypes.Left && aim.X < 0f)
                            return true;
                        else if (inputType == InputTypes.Right && aim.X > 0f)
                            return true;
                        else if (inputType == InputTypes.Down && aim.Y > 0f)
                            return true;
                        else if (inputType == InputTypes.Up && aim.Y < 0f)
                            return true;
                    }
                    break;
            }
            return false;
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.Player.Jump += Player_Jump;
            On.Celeste.Player.WallJump += Player_WallJump;
            On.Celeste.PlayerCollider.Check += PlayerCollider_Check;
            IL.Monocle.Engine.Update += Engine_Update;
        }

        internal static void Unload() {
            On.Celeste.Player.Jump -= Player_Jump;
            On.Celeste.Player.WallJump -= Player_WallJump;
            On.Celeste.PlayerCollider.Check -= PlayerCollider_Check;
            IL.Monocle.Engine.Update -= Engine_Update;
        }

        private static void Player_Jump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx) {
            orig(self, particles, playSfx);

            if (self == null) {
                return;
            }

            foreach (TriggerTrigger trigger in self.SceneAs<Level>().Tracker.GetEntities<TriggerTrigger>()) {
                if (trigger.activationType == ActivationTypes.Jumping) {
                    trigger.externalActivation = true;
                    if (trigger.resetAfterJump) {
                        trigger.resetActivation = true;
                    } else {
                        self.Add(new Coroutine(trigger.JumpRoutine(self, trigger), true));
                    }
                }
            }
        }

        private static void Player_WallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir) {
            orig(self, dir);

            if (self == null) {
                return;
            }

            foreach (TriggerTrigger trigger in self.SceneAs<Level>().Tracker.GetEntities<TriggerTrigger>()) {
                if (trigger.activationType == ActivationTypes.Jumping && trigger.includeWalljump) {
                    trigger.externalActivation = true;
                    if (trigger.resetAfterJump) {
                        trigger.resetActivation = true;
                    } else {
                        self.Add(new Coroutine(trigger.JumpRoutine(self, trigger), true));
                    }
                }
            }
        }

        private IEnumerator JumpRoutine(Player player, TriggerTrigger trigger) {
            while (!player.OnGround()) {
                yield return null;
            }
            trigger.externalActivation = false;
        }

        private static bool PlayerCollider_Check(On.Celeste.PlayerCollider.orig_Check orig, PlayerCollider self, Player player) {
            bool result = orig(self, player);

            if (self.Scene?.Tracker.GetEntities<TriggerTrigger>() is not { Count: > 0 } triggers) {
                return result;
            }

            foreach (TriggerTrigger trigger in triggers) {
                if (trigger.activationType != ActivationTypes.OnEntityCollide) {
                    continue;
                }

                if (!trigger.collideTypes.Contains(self.Entity)) {
                    continue;
                }

                if (result) {
                    if (trigger.playerCollidingEntities.Add(self.Entity)) {
                        trigger.onEntityCollidedCount++;
                    }
                } else {
                    trigger.playerCollidingEntities.Remove(self.Entity);
                }
            }

            return result;
        }

        private static void Engine_Update(ILContext il) {
            // code taken from communal helper's AbstractInputController
            // https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/Entities/Misc/AbstractInputController.cs

            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdsfld<Engine>("FreezeTimer"),
                instr => instr.MatchCall<Engine>("get_RawDeltaTime"))) {
                cursor.EmitDelegate<Action>(UpdateFreezeInput);
            }
        }

        private static void UpdateFreezeInput()
        {
            foreach (TriggerTrigger trigger in Engine.Scene.Tracker.GetEntities<TriggerTrigger>()) {
                if (trigger.activationType == ActivationTypes.OnInput && !trigger.inputHeld && trigger.CheckInput(trigger.inputType, false)) {
                    trigger.externalActivation = true;
                    trigger.resetActivation = true;
                }
            }
        }

        #endregion

        private readonly bool global;
        private bool activated;

        private readonly Vector2[] nodes;
        private readonly bool oneUse;
        private readonly ActivationTypes activationType;
        private readonly string flag;
        private readonly int deaths;
        private readonly int dashCount;
        private readonly float requiredSpeed;
        private readonly float waitTime;
        private float hasWaited;
        private readonly Session.CoreModes coreMode;
        private readonly InputTypes inputType;
        private readonly bool inputHeld;
        private readonly bool excludeTalkers;
        private readonly bool ifSafe;
        private readonly bool includeCoyote;
        private readonly bool includeWalljump;
        private readonly bool resetAfterJump;
        private readonly int playerState;
        private readonly TalkComponent talker;
        private readonly List<Entity> entitiesInside;
        private readonly TypeHelper.TypeAndSidList collideTypes;
        private readonly int onEntityCollideCount;
        private readonly HashSet<Entity> playerCollidingEntities = new();
        private int onEntityCollidedCount;
        private bool externalActivation;
        private bool resetActivation;
        private readonly bool invertCondition;
        private readonly ComparisonTypes comparisonType;
        private readonly bool absoluteValue;
        private readonly float delay;
        private readonly bool randomize;
        private readonly bool matchPosition;
        private readonly bool onlyOnEnter;

        private List<Trigger> triggers;
        private bool activating;
        private bool deactivating;
        private Trigger chosenTrigger;

        public enum ActivationTypes {
            Flag,
            Dashing,
            DashCount,
            DeathsInRoom,
            DeathsInLevel,
            GrabHoldable,
            SpeedX,
            SpeedY,
            Jumping,
            Crouching,
            TimeSinceMovement,
            OnHoldableEnter,
            CoreMode,
            OnEntityCollide,
            OnInteraction,
            OnSolid,
            OnEntityEnter,
            OnInput,
            OnGrounded,
            OnPlayerState,
        };
        public enum InputTypes {
            Left,
            Right,
            Up,
            Down,
            Jump,
            Dash,
            Grab,
            Interact,
            CrouchDash,
            Any,
        };
        private static readonly List<ActivationTypes> bypassGlobal = new List<ActivationTypes>() {
            ActivationTypes.OnHoldableEnter,
            ActivationTypes.OnInteraction,
            ActivationTypes.OnEntityEnter,
        };
        private enum ComparisonTypes {
            LessThan,
            EqualTo,
            GreaterThan,
        };
    }
}
