using System.Diagnostics;
using GameNetcodeStuff;
using UnityEngine;
using LethalLib;
using System;

using LostEmployee.Navigation;

using SecurityDoor = TerminalAccessibleObject;
using NormalDoor = DoorLock;
using Action = LostEmployee.Actions.Action;
using Object = UnityEngine.Object;
using AnimatorHandler = LostEmployee.Animations.AnimatorHandler;
using Inventory = LostEmployee.Objects.Inventory;
using LostEmployee.Actions;
using LostEmployee.Infos;
using UnityEngine.AI;

namespace LostEmployee {


    class LostEmployeeAI : EnemyAI
    {

        internal float ReachingDistance = 2f;
        internal PathFinder pathFinder = new PathFinder();
        internal Inventory inventory = new Inventory(1, 4, 4);
        //internal TurnCompass turnCompass = new TurnCompass();
        internal AnimatorHandler animatorHandler = new AnimatorHandler();

        bool targetFound = false;
        BoomboxItem target = null;

        Action? currentTrigger = null;
        Action currentAction = new Action();

        //internal AnimationHandler animationHandler = new AnimationHandler();

        [Conditional("DEBUG")]
        void LogIfDebugBuild(string text) {
            Plugin.Logger.LogInfo(text);
        }
        public Transform turnCompass = null!;


        T? nearest<T>() where T : Object
        {
            T? best = default;
            float mindist = 3000;
            foreach (T item in Object.FindObjectsOfType<T>()) {
                if (! GameHandler.IsAvailable<T>(item)) continue;
                float dist = Vector3.Distance(transform.position, GameHandler.GetPosition<T>(item));
                if (dist < mindist) {
                    mindist = dist;
                    best = item;
                }
            }
            return best;
        }
        public override void Start() {
            base.Start();
            pathFinder.Start(agent);    // to be removed !...
            inventory.Start(this);
            animatorHandler.Start(this);

            Pathfinding.PathFinder.Start();
            Pathfinding.PathFinder.PrintGraph();


            LogIfDebugBuild("Example Enemy Spawned");
            //creatureAnimator.SetTrigger("startWalk");
            //animationHandler.InitAnimShowTime();
            currentAction = TestWaitSequence<BoomboxItem>();
        }

        public override void Update() {
            base.Update();

            if (isEnemyDead) return;

            animatorHandler.update();

            if (animatorHandler.IsLockedInAnimation) return;

            if (currentTrigger != null) {
                currentTrigger.Update(this);

                if (! currentTrigger.pending()) {
                    currentTrigger = null;
                }
            } else {
                currentAction.Update(this);
            }

            if (!currentAction.pending()) {
                if (currentAction.succeeded()) Plugin.Logger.LogInfo("action succeeded !");
                else Plugin.Logger.LogInfo("action failed !");

                currentAction = new Action();
            }
        }

        public override void DoAIInterval() {
            
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
                return;
            };
            animatorHandler.refresh();
            currentAction.AIInterval(this);
        }

        public override void OnCollideWithPlayer(Collider other) {
            if (isEnemyDead) return;
            // should push the player if collide with him.
            SetTrigger(Interact.Push(MeetsStandardPlayerCollisionConditions(other), UnityEngine.Random.Range(10f, 30f)));
            return;

            // i need to be able to move around the guy freely so no damage !
            PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
            if (playerControllerB != null)
            {
                LogIfDebugBuild("Example Enemy Collision with Player!");
                playerControllerB.DamagePlayer(1);
            }
        }

        public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            if(isEnemyDead){
                return;
            }
            enemyHP -= force;
           // creatureAnimator.SetTrigger("HitEnemy");
           // stun animation?
            if (IsOwner) {
                if (enemyHP <= 0 && !isEnemyDead) {
                    // currentAction = Misc.
                    currentAction = Misc.Die();

                    // Our death sound will be played through creatureVoice when KillEnemy() is called.
                    // KillEnemy() will also attempt to call creatureAnimator.SetTrigger("KillEnemy"),
                    // so we don't need to call a death animation ourselves.

                    // We need to stop our search coroutine, because the game does not do that by default.
                    StopCoroutine(searchCoroutine);
                    //animatorHandler.KillEnemy();
                    KillEnemyOnOwnerClient();
                }
            }
        }

        Action TestWaitSequence<T>() where T : UnityEngine.Object {
            T? currenttarget = nearest<T>();
            
            if (currenttarget == null) {
                Plugin.Logger.LogInfo("no target !");
                return new Action();
            }

            Plugin.Logger.LogInfo("room start : " + Pathfinding.PathFinder.RoomAt(transform.position));
            Plugin.Logger.LogInfo("room finish : " + Pathfinding.PathFinder.RoomAt(GameType<T>.GetPosition(currenttarget)));

            var obstacles = Pathfinding.PathFinder.ComputePath(agent.transform.position, GameType<T>.GetPosition(currenttarget));
            Pathfinding.PathFinder.Print(obstacles);
            NavMeshPath path = new NavMeshPath();
            bool canPathfind = NavMesh.CalculatePath(agent.transform.position, obstacles[0].front, NavMesh.AllAreas, path);
            if (! canPathfind) {
                Plugin.Logger.LogInfo("cannot pathfind to first obstacle !");
            } else {
                Plugin.Logger.LogInfo("can pathfind to first obstacle");
            }

            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[0];
            Vector3 position = agent.transform.position;
            return new Sequence([
                //new Union ([
                //    Misc.Idle(), 
                //    Turn.ToTarget<PlayerControllerB>(player),
                //    Wait.ForCondition(agent => agent.target != null)
                //], [false, false, true]),
                Misc.Print("pathfinding"),
                Move.ToTarget<T>(currenttarget),
                Misc.Print("toggle Inactive"),
                Interact.ToggleActive<T>(currenttarget, false),
                Misc.Print("grabbing"),
                Interact.Grab<T>(currenttarget),
                Misc.Print("pathfinding back"),
                Move.ToPosition(position),
                Misc.Print("dropping"),
                Interact.Drop<T>(currenttarget, position),
                Misc.Print("toggle Active"),
                Interact.ToggleActive<T>(currenttarget, true)
            ]);
        }

        Action TestTravelSequence<T>() where T : UnityEngine.Object {
            T? currenttarget = nearest<T>();
            
            if (currenttarget == null) {
                Plugin.Logger.LogInfo("no target !");
                return new Action();
            }

             Vector3 position = agent.transform.position;

            return new Sequence([
                Travel.ToTarget<T>(currenttarget, ReachingDistance),
                Interact.ToggleActive<T>(currenttarget, false),
                Interact.Grab<T>(currenttarget),
                Travel.ToTarget<Vector3>(position, ReachingDistance),
                Interact.Drop<T>(currenttarget, position)
            ]);
        }

        public void SetTrigger(Action trigger) {
            currentTrigger = trigger;
        }

    }

    // indicate to pathfinding that we can, indeed, go through locked doors.
    // I put that here as the cost function and function used to open doors
    // Are probably going to depend, in the future, of the agent emotional state.
    class ThroughLockedDoors : CostFunction {

        public override float TraversalCost(System.Type? type, object? door) {
            if (door == null) return CostFunction.FREE;
            
            if (type == typeof(SecurityDoor)) {
                SecurityDoor security = (SecurityDoor) door;
                return DoorInfos.IsLocked(security) ? CostFunction.IMPASSABLE : CostFunction.FREE;
            }

            if (type == typeof(NormalDoor)) {
                return CostFunction.FREE;
            }

            return CostFunction.IMPASSABLE;
        }
    }

    class Traverse : Action {
        Type? type;
        object? obstacle;

        Action? subAction = null;

        public Traverse(Type? type, object? obstacle) {
            this.type = type;
            this.obstacle = obstacle;
            status = Status.PENDING;
        }

        public override void Update(LostEmployeeAI agent) {
            if (! pending()) return;

            if (type == null) { // nothing here !
                status = Status.SUCCESS; return;
            }

            if (type == typeof(NormalDoor)) {
                NormalDoor door = (NormalDoor) obstacle;

                if (subAction == null) {
                    subAction = Action.Sequence([
                        Interact.ToggleLocked(door, false),
                        Interact.ToggleOpen(door, true)
                    ]);
                }

                subAction.Update(agent);
                if (subAction.succeeded()) {
                    status = Status.SUCCESS;
                }

                if (subAction.failed()) {
                    status = Status.FAILURE;
                }
            }
            status = Status.SUCCESS; return;
        }
    }
}
