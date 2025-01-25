
using UnityEngine;
using System.Collections.Generic;
using LostEmployee.Infos;
using LostEmployee.Pathfinding;

namespace LostEmployee.Actions;

class Travel {

    public static Action ToTarget<T> (Percept<T> percept, float range) {
        return new TravelToTarget<T>(percept, range);
    }

    public static Action ToTarget<T> (T target, float range) {
        return new TravelToTarget<T>(agent => target, range);
    }
}


class TravelToTarget<T> : Action {
    Percept<T> percept;
    float range;
    int currentObstacle;
    List<PathFinder.Obstacle>? obstacles;

    Action? currentTraversal = null;
    bool firstDecision = true;



    public TravelToTarget(Percept<T> percept, float range) {
        this.percept = percept;
        this.range = range;
        this.status = Status.PENDING;
    }

    public TravelToTarget(T target, float range) {
        this.percept = agent => target;
        this.range = range;
        status = Status.PENDING;
    }

    public override void AIInterval(LostEmployeeAI agent) {
        if (! pending()) return; 


        var obstacles = Pathfinding.PathFinder.ComputePath(agent.transform.position, GameType<T>.GetPosition(percept(agent)), new LostEmployeePathfindingParams());
        if (obstacles == null) {    // cannot pathfind !
            status = Status.FAILURE;
            return;
        }

        // current obstacle is the first obstacle not considered as open by the agent
        for (currentObstacle = 0; currentObstacle < obstacles.Count; currentObstacle++) {
            if (! IsOpen(obstacles[currentObstacle])) break;
        }

        firstDecision = false;
    }

    public override void Update(LostEmployeeAI agent) {
        if (! pending()) return;
        if (firstDecision) return;

        if (obstacles == null) {
            status = Status.FAILURE;
            return;
        }

        Vector3 currentDestination;

        if (currentObstacle < obstacles.Count) {
            
            currentDestination = obstacles[currentObstacle].front;
            
            if (Reached(agent, currentDestination)) {
                if (currentTraversal == null) {
                    currentTraversal = Traverse(obstacles[currentObstacle]);
                }
                currentTraversal.Update(agent);
                if (currentTraversal.succeeded()) {
                    ClearTraversal(agent);
                }
                if (currentTraversal.failed()) {
                    status = Status.FAILURE;
                    return;
                }
            }
        } else {
            currentDestination = GameType<T>.GetPosition(percept(agent));
            if (Vector3.Distance(agent.transform.position, currentDestination) < range) {
                status = Status.SUCCESS;
                return;
            }
        }
    }

    bool IsOpen(PathFinder.Obstacle obstacle) {
        if (obstacle.type == null || obstacle.instance == null) return true;

        if (obstacle.type == typeof(DoorLock)) {
            return PathFinder.Helper.IsOpen((DoorLock) obstacle.instance);
        }

        if (obstacle.type == typeof(TerminalAccessibleObject)) {
            return PathFinder.Helper.IsOpen((TerminalAccessibleObject) obstacle.instance);
        }

        return false;
    }

    void ClearTraversal(LostEmployeeAI agent) {
        if (obstacles == null) return;

        currentObstacle++;
        currentTraversal = null;

        if (currentObstacle < obstacles.Count) {
            agent.agent.SetDestination(obstacles[currentObstacle].front);
        } else {
            agent.agent.SetDestination(GameType<T>.GetPosition(percept(agent)));
        }
    }

    bool Reached(LostEmployeeAI agent, Vector3 position) {
        if (! agent.agent.pathPending) {
            if (agent.agent.remainingDistance <= agent.agent.stoppingDistance) {
                if (! agent.agent.hasPath || agent.agent.velocity.sqrMagnitude == 0f) {
                    return true;
                }
            }
        }
        return false;
    }

    Action Traverse (PathFinder.Obstacle obstacle) {
        if (obstacle.type == null || obstacle.instance == null) {
            return Action.Success();
        }

        if (obstacle.type == typeof(DoorLock)) {
            DoorLock door = (DoorLock) obstacle.instance;
            return Action.Sequence([
                Interact.ToggleLocked(door, false),
                Interact.ToggleOpen(door, true)
            ]);
        }

        return Action.Failure();
    }
}

class LostEmployeePathfindingParams : PathFinder.Parameters {

    // Lost Employee can open locked door and consider them as passable.
    public override float CostTraversal(System.Type? type, object? instance) {
        if (type == null || instance == null) return PathFinder.Cost.FREE;

        if (type == typeof(DoorLock)) {
            return PathFinder.Cost.FREE;
        }

        if (type == typeof(TerminalAccessibleObject)) {
            TerminalAccessibleObject door = (TerminalAccessibleObject) instance;

            return PathFinder.Helper.IsOpen(door) ? PathFinder.Cost.FREE : PathFinder.Cost.IMPASSABLE;
        }

        return PathFinder.Cost.IMPASSABLE;
    }
}