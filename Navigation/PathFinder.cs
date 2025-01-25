using UnityEngine;
using DunGen;
using System.Collections.Generic;
using UnityEngine.AI;
using System;

using SecurityDoor = TerminalAccessibleObject;
using NormalDoor = DoorLock;

// TODO : Test with recalculation on refresh.
//          It can bug out if we "ClearTraversal" : we open the door and the agent
//          Remain in the same tile. If we recompute the path without giving him time to traverse
//          then it will always attempt to go in front of the door.

//          For now, my solution is to not recompute the path if the position remain the same as before.
//          This give us the problem of the path becoming irrelevant if something was to change during travel time...
//          Will need to perform code injection in order for it to work (as it would become a listener of door & ...).

namespace LostEmployee.Navigation;


class Obstacle {
    public Vector3 pos1;
    public Vector3 pos2;

    public Type obstacleType;

    public object? obstacle;

    public Obstacle(Vector3 pos1, Vector3 pos2, Type obstacleType, object? obstacle) {
        this.pos1 = pos1;
        this.pos2 = pos2;
        this.obstacleType = obstacleType;
        this.obstacle = obstacle;
    }
}


/// <summary>
/// In order for your custom EnemyAI to pathfind, it need to contain an instance of PathFinder.
/// The usage is simple : where you would call agent.SetDestination, call instead pathfinder.SetDestination 
/// (where pathfinder is the instance of Pathfinder in your class and agent the NavMeshAgent in EnemyAI).
/// 
/// Here how pathfinding with it work in a nutshell : 
/// 
/// Instead of using the Unity Navmesh to compute a path from start to finish, the PathFinder compute the sequence of Tiles
/// the agent must traverse to go to the finish position optimally. When the agent enter a tile, the pathfinder
/// use the Unity Navmesh to compute the path leading to the next tile in the traject. This ensure that the Unity Navmesh is only used
/// to compute relatively small path which, in therm, should save you computation time.
/// 
/// While calling SetDestination without any CostFunction argument would yield the same behavior than a normal call to the Unity Navmesh,
/// adding a CostFunction argument allow you to define what room and doors can traverse and at what cost. This allow, for example, your AI
/// to pathfind through locked doors, avoid or prefer to go through certain rooms, etc... See CostFunction for more details.
/// </summary>

class PathFinder {

    public static void Start() {
        if (dungeon == null || dungeon != RoundManager.Instance.dungeonGenerator.Generator.CurrentDungeon) {
            dungeon = RoundManager.Instance.dungeonGenerator.Generator.CurrentDungeon;
            graph = new Graph(dungeon);
        }
    }

    public void Start(NavMeshAgent agent) {
        PathFinder.Start();
        this.agent = agent;

        Plugin.Logger.LogInfo(string.Format("agent stopping distance : {0}", agent.stoppingDistance));
    }

    public void PrintPath(NavMeshAgent agent, Vector3 position, CostFunction? func  = null) {
        List<int[]>? path = graph.Path(agent.transform.position, position, func);

        if (path == null) {
            Plugin.Logger.LogInfo("no path !");
            return;
        }
        
        for (int i = 0; i < path.Count; i++) {
            int[] doornodes = path[i];

            string msg = string.Format("{0} -> {1} through ", graph.GetDoorNodeAt(doornodes[0]).RoomNodeID, graph.GetDoorNodeAt(doornodes[1]).RoomNodeID);

            Tuple<Type?, object?> obstacle = graph.GetObstacleAt(doornodes[0]);
            if (obstacle.Item2 == null) {
                msg += "NOTHING";
            } else {
                if (obstacle.Item1 == typeof(SecurityDoor)) msg += "SECURITY DOOR";
                if (obstacle.Item1 == typeof(NormalDoor)) msg += "DOOR";
            }
            Plugin.Logger.LogInfo(msg);
        }
    }


    /// <summary>
    /// Set the agent destination to the given position. 
    /// </summary>
    /// <param name="position">target destination</param>
    /// <param name="rules">cost function to be optimised </param>
    /// <returns> true if the destination was correctly set, false otherwise </returns>
    public bool SetDestination(Vector3 position, CostFunction? func = null) {
        if (graph == null) return false;
        if (position == currentDestination) return true;    // is going here anyway, don't compute it again !...

        List<int[]>? path = graph.Path(agent.transform.position, position, func);
        
        if (path == null) {
            return false; // cannot pathfind
        }

        currentDestination = position;
        currentPath = new Queue<int[]>(path);

        _isWaitingTraversal = false;
        _isFinished = false;
        agent.isStopped = false;
        return SetNextDestination();
    }

    // to do : also finished if agent reached target.
    public bool Update( ) {
        if (IsWaitingTraversal) return true;
        if (IsFinished) return true;

        if (LocalDestinationReached(fullstop)) {
            if (currentLocalDestination == currentDestination) {
                _isFinished = true;
                return true;
            }

            Tuple<Type?, object?> obstacle = graph.GetObstacleAt(currentDoorway[0]);

            if (obstacle.Item2 == null) {
                return SetNextDestination();
            } else {
                _isWaitingTraversal = true;
                return true;
            }
        }
        return true;
    }

    public Tuple<Type?, object?> GetObstacle() {
        return graph.GetObstacleAt(currentDoorway[0]);
    }

    public void ClearTraversal() {
        if (! IsWaitingTraversal) return;

        _isWaitingTraversal = false;
        SetNextDestination();
    }

    bool SetNextDestination() {
        if (currentPath.Count == 0) {
            currentLocalDestination = currentDestination;
            agent.autoBraking = true;
            fullstop = true;
        } else {
            currentDoorway = currentPath.Dequeue();
            currentLocalDestination = graph.GetDoorNodeAt(currentDoorway[0]).position;

            Tuple<Type?, object?> obstacle = graph.GetObstacleAt(currentDoorway[0]);

            if (obstacle.Item2 == null) {
                agent.autoBraking = false;
                fullstop = false;
            } else {
                agent.autoBraking = true;
                fullstop = true;
            }
        }

        if (! agent.SetDestination(currentLocalDestination)) {
            return false;
        }
        return true;
    }

    // fullstop is used to avoid the agent 'stopping' at each time even in front
    // of nothing.
    bool LocalDestinationReached(bool fullstop = false) {
        if (! fullstop) {
            return agent.remainingDistance <= 1f;   // maybe need to finetune with velocity ?...
        }

        if (! agent.pathPending) {
            if (agent.remainingDistance <= agent.stoppingDistance) {
                if (! agent.hasPath || agent.velocity.sqrMagnitude == 0f) {
                    return true;
                }
            }
        }
        return false;
    }

    static Graph? graph = null;
    static Dungeon? dungeon = null;

    Queue<int[]> currentPath = new Queue<int[]>();

    int[] currentDoorway = new int[2];

    Vector3 currentLocalDestination;
    Vector3 currentDestination;

    NavMeshAgent agent = null!;

    private bool fullstop = false;

    private bool _isFinished;
    private bool _isWaitingTraversal;

    public bool IsFinished => _isFinished;
    public bool IsWaitingTraversal => _isWaitingTraversal;
}