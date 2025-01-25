using UnityEngine;
using LostEmployee;
using LostEmployee.Infos;

namespace LostEmployee.Actions;


class Move {

    public static Action ToPosition(Vector3 position) {
        return MoveToTarget<Vector3>.CreateFromFixed(position);
    }

    public static Action ToTarget<T>(Percept<T> percept) {
        return new MoveToTarget<T>(percept);
    }

    public static Action ToTarget<T>(T target) {
        return MoveToTarget<T>.CreateFromFixed(target);
    }

    public static Action Stop() {
        return new MoveStop();
    }

    class MoveStop : Action {

        public MoveStop() {
            status = Status.PENDING;
        }

        public override void AIInterval(LostEmployeeAI agent) {
            Update(agent);
        }

        public override void Update(LostEmployeeAI agent) {
            if (! pending()) return;

            agent.agent.isStopped = true;
            status = Status.SUCCESS;
        }
    }

    class MoveToTarget<T> : Action {
        Percept<T> percept;
        bool firstRefresh = true;
        Action? currentTraversal = null;

        public MoveToTarget(Percept<T> percept) {
            this.percept = percept;
            status = Status.PENDING;
        }

        public override void AIInterval(LostEmployeeAI agent) {
            if (! pending()) return;

            if (! agent.pathFinder.SetDestination(GameType<T>.GetPosition(percept(agent)))) {
                status = Status.FAILURE;
            }
            firstRefresh = false;
        }

        public override void Update(LostEmployeeAI agent) {
            if (! pending()) return;
            if (firstRefresh) return;

            if (! agent.pathFinder.Update()) {
                Plugin.Logger.LogInfo("pathfinder failed to update");
                status = Status.FAILURE; return;
            }

            if (agent.pathFinder.IsFinished) {
                Plugin.Logger.LogInfo("path finished !");
                status = Status.SUCCESS; return;
            }

            if (agent.pathFinder.IsWaitingTraversal) {

                if (currentTraversal == null) {
                    Plugin.Logger.LogInfo("beginning traversal");
                    var obstacle = agent.pathFinder.GetObstacle();
                    currentTraversal = new Traverse(obstacle.Item1, obstacle.Item2);
                }

                currentTraversal.Update(agent);

                if (currentTraversal.succeeded()) {
                    agent.pathFinder.ClearTraversal();
                    currentTraversal = null;
                } else if (currentTraversal.failed()) {
                    status = Status.FAILURE;
                    return;
                }
            }
        }

        public static MoveToTarget<T> CreateFromFixed (T item) {
            return new MoveToTarget<T>(agent => item);
        }
    }
}