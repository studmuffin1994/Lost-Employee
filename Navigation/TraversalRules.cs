namespace LostEmployee.Navigation;

using SecurityDoor = TerminalAccessibleObject;
using NormalDoor = DoorLock;

using DunGen;
using UnityEngine;
using System;

/// <summary>
/// CostFunction is a virtual class used to set the parameter of the pathfinding. 
/// This allow you to define which obstacles and rooms the agent can traverse and at what cost,
/// and define the heuristic used during pathfinding.
/// </summary>
class CostFunction {


    public static readonly float IMPASSABLE = float.MaxValue;
    public static readonly float FREE = 0;


    /// <summary>
    /// If EarlyStop is set to true, the pathfind might find a good enough solution rather than the optimal one.
    /// </summary>
    protected bool EarlyStop = true;

    /// <summary>
    /// the Heuristic function is used to influence which room the pathfinder will consider traversing
    /// first when multiple rooms may lead to the target. The smaller the Heuristic is for a room, the more it is prioritized.
    /// A fitting heuristic might considerably speed up the pathfinding process.
    /// The default Heuristic prioritize rooms closest to the current destination. ( TO BE IMPLEMENTED )
    /// </summary>
    /// <returns></returns>
    public virtual float Heuristic( ) {
        return CostFunction.FREE;
    }

    public virtual float TraversalCost (Type? type, object? door) {
        if (door == null) return FREE;

        if (type == typeof(SecurityDoor)) {
            SecurityDoor security = (SecurityDoor) door;
            return DoorInfos.IsLocked(security) ? IMPASSABLE : FREE;
        }

        if (type == typeof(NormalDoor)) {
            NormalDoor normal = (NormalDoor) door;
            return DoorInfos.IsLocked(normal) ? IMPASSABLE : FREE;
        }

        return FREE;
    }

    public virtual float TravelCost(Tile tile, Vector3 startPos, Vector3 endPos) {
        return Vector3.Distance(startPos, endPos);
    }
}