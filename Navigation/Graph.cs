using System;
using System.Collections.Generic;
using System.Linq;
using DunGen;
using UnityEngine;

using SecurityDoor = TerminalAccessibleObject;
using NormalDoor = DoorLock;
using UnityEngine.UIElements;

namespace LostEmployee.Navigation;

// TODO : in AddConnection, make an offset corresponding to the door.
// TODO : the Apparatus room is not considered as a Room, as the corridor in front is also considered to be the same room.
//        this is why I always have 2 normal doors which are not correctly place. Need to separate the 2 rooms "manually"

class Graph {

    public Graph(Dungeon dungeon) {
        int nb_correct_nodes = 0;

        Dictionary<Tile, int> indexOf = new Dictionary<Tile, int>();

        // add all door object present in the dungeon
        List<SecurityDoor> AllSecurityDoors = GameObject.FindObjectsOfType<SecurityDoor>().ToList();
        List<NormalDoor> AllNormalDoors = GameObject.FindObjectsOfType<NormalDoor>().ToList();

        AllDoorObjects = new List<object>();
        AllDoorTypes = new List<Type>();

        for (int i = 0; i < AllSecurityDoors.Count; i++) {
            AllDoorObjects.Add((object) AllSecurityDoors[i]);
            AllDoorTypes.Add(typeof(SecurityDoor));
        }
        for (int i = 0; i < AllNormalDoors.Count; i++) {
            AllDoorObjects.Add((object) AllNormalDoors[i]);
            AllDoorTypes.Add(typeof(NormalDoor));
        }

        // Add all rooms to the graph
        AllRooms = new List<RoomNode>();
        for (int i = 0; i < dungeon.AllTiles.Count; i++) {
            indexOf.Add(dungeon.AllTiles[i], i);
            AllRooms.Add(new RoomNode(dungeon.AllTiles[i]));
        }

        // initialize all the connections between rooms.
        AllDoors = new List<DoorNode>();
        for (int i = 0; i < AllRooms.Count; i++) {
            int currentRoomID = i;
            for (int j = 0; j < dungeon.AllTiles[i].UsedDoorways.Count; j++) {
                int neighboorRoomID = indexOf[dungeon.AllTiles[i].UsedDoorways[j].ConnectedDoorway.Tile];
                if (neighboorRoomID < currentRoomID) continue;  // connection added previously.

                AddConnection(currentRoomID, neighboorRoomID, dungeon.AllTiles[i].UsedDoorways[j]);
            }
        }

        Plugin.Logger.LogInfo(string.Format("number of connections total : {0}", AllDoors.Count / 2));
        Plugin.Logger.LogInfo(string.Format("number of correct nodes : {0}", nb_correct_nodes));

        void AddConnection(int roomID1, int roomID2, Doorway doorway) {
            int DoorObjectID = -1;
            // this is kinda slow, but infortunately no way to get the door from the Doorway object and called only once per dungeon.
            // Zeeks forced me to do this :(
            for (int i = 0; i < AllDoorObjects.Count; i++) {

                if (AllDoorTypes[i] == typeof(SecurityDoor)) {
                    if (DoorInfos.IsInDoorway((SecurityDoor) AllDoorObjects[i], doorway)) {
                        DoorObjectID = i;
                        break;
                    }
                }

                if (AllDoorTypes[i] == typeof(NormalDoor)) {
                    if (DoorInfos.IsInDoorway((NormalDoor) AllDoorObjects[i], doorway)) {
                        DoorObjectID = i;
                        break;
                    }
                }
            }

            Vector3[] positions = DoorInfos.GetNodes(doorway);
            Vector3 position1;
            Vector3 position2;
            if (AllRooms[roomID1].tile.Bounds.Contains(positions[0])) {
                position1 = positions[0];
                position2 = positions[1];
            } else {
                position1 = positions[1];
                position2 = positions[0];
            }

            DoorNode door1 = new DoorNode(roomID1, AllDoors.Count+1, DoorObjectID, position1);
            DoorNode door2 = new DoorNode(roomID2, AllDoors.Count  , DoorObjectID, position2);
            AllDoors.Add(door1);
            AllDoors.Add(door2);

            AllRooms[roomID1].AllDoorNodesID.Add(AllDoors.Count-2);
            AllRooms[roomID2].AllDoorNodesID.Add(AllDoors.Count-1);
        }
    }

    public void PrintConnections() {
        Plugin.Logger.LogInfo(string.Format("number of rooms : {0}", AllRooms.Count));


        for (int i = 0; i < AllRooms.Count; i++) {

            for (int j = 0; j < AllRooms[i].AllDoorNodesID.Count; j++) {

                DoorNode nodeA = GetDoorAt(AllRooms[i].AllDoorNodesID[j]);

                if (nodeA.DoorObjectID != -1) { // so here we now that door object ID is not set to -1
                    DoorNode nodeB = GetDoorAt(nodeA.ConnectedDoorNodeID);
                    if (nodeB.RoomNodeID < nodeA.RoomNodeID) continue;  // already printed !

                    Plugin.Logger.LogInfo(string.Format("{0} <-> {1} :", nodeA.RoomNodeID, nodeB.RoomNodeID) + DoorString(AllDoorTypes[nodeA.DoorObjectID], AllDoorObjects[nodeA.DoorObjectID]));
                }
            }
        }

        string DoorString(Type type, object door) {
            if (door == null) {
                return "(NULL)";
            }

            string line = "";
            if (type == typeof(NormalDoor)) {
                NormalDoor normal = (NormalDoor) door;

                line += "Normal Door";
                if (DoorInfos.IsLocked(normal)) line += "(LOCKED)";
                else if (DoorInfos.IsOpen(normal)) line += "(OPEN)";
                else line += "(CLOSED)";

                return line;
            } else if (type == typeof(SecurityDoor)) {
                SecurityDoor security = (SecurityDoor) door;
                line += "Security Door";
                if (DoorInfos.IsLocked(security)) line += "(CLOSED)";
                else line += "(OPEN)";

                return line;
            } else {
                return line;
            }
        }
    }

    // TODO : maybe implement that with some kind of AABB tree ?... see later if necessary or profitable.
    internal int GetRoomAt(Vector3 position) {
        for (int i = 0; i < AllRooms.Count; i++) {
            Bounds bounds = AllRooms[i].tile.Bounds;
            if (bounds.Contains(position)) return i;
        }
        return -1;
    }

    internal Tuple<Type?, object?> GetObstacleAt(int DoorID) {
        DoorNode node = GetDoorAt(DoorID);
        if (node.DoorObjectID == -1) {
            return new Tuple<Type?, object?>(null, null);
        } else {
            return new Tuple<Type?, object?>(AllDoorTypes[node.DoorObjectID], AllDoorObjects[node.DoorObjectID]);
        }
    }

    internal DoorNode GetDoorNodeAt(int index) {
        return AllDoors[index];
    }

    /// <summary>
    /// Compute the sequence of rooms and doors to go from the start position to the end position optimally.
    /// We do so by using the Dikjstra algorithm with a Binary Heap.
    /// </summary>
    /// <param name="startRoomID"></param>
    /// <param name="endRoomID"></param>
    /// <param name="function"></param>
    /// <returns></returns>
    public List<int[]>? Path(Vector3 start, Vector3 finish, CostFunction? function = null) {
        int startRoomID = GetRoomAt(start);
        int endRoomID = GetRoomAt(finish);

        if (startRoomID == -1 || endRoomID == -1) return null;  // not in the dungeon !
        if (startRoomID == endRoomID) return new List<int[]>();  // already in the same room !

        //Plugin.Logger.LogInfo(string.Format("pathing from room {0} to {1}", startRoomID, endRoomID));

        if (function == null) function = new CostFunction();

        int[] entrance = new int[AllRooms.Count];
        float[] cost = new float[AllRooms.Count];

        for (int i = 0; i < AllRooms.Count; i++) {
            entrance[i] = -1;
            cost[i] = (i == startRoomID) ? 0 : float.MaxValue;
        }

        BinaryHeap heap = new BinaryHeap(AllRooms.Count);
        heap.Add(startRoomID, cost[startRoomID]);

        int currentRoomID;
        while (! heap.isEmpty()) {

            currentRoomID = heap.ExtractBest();
            //Plugin.Logger.LogInfo(string.Format("current room ID : {0}", currentRoomID));

            if (currentRoomID == endRoomID) break;  // the path is complete !

            for (int i = 0; i < AllRooms[currentRoomID].AllDoorNodesID.Count; i++) {

                Vector3 posEntrance = (currentRoomID == startRoomID)? start : GetDoorAt(entrance[currentRoomID]).position;

                DoorNode doorExit = GetDoorAt(AllRooms[currentRoomID].AllDoorNodesID[i]);

                Vector3 posExit = doorExit.position;


                float travelCost = function.TravelCost(AllRooms[currentRoomID].tile, posEntrance, posExit);
                float traversCost = CostFunction.FREE;
                if (doorExit.DoorObjectID != -1) {
                    traversCost = function.TraversalCost(AllDoorTypes[doorExit.DoorObjectID], AllDoorObjects[doorExit.DoorObjectID]);
                }

                if (travelCost == CostFunction.IMPASSABLE || traversCost == CostFunction.IMPASSABLE) continue;

                DoorNode node = GetDoorAt(AllRooms[currentRoomID].AllDoorNodesID[i]);
                int neighboorRoomID = AllDoors[node.ConnectedDoorNodeID].RoomNodeID;

                if (cost[neighboorRoomID] > cost[currentRoomID] + travelCost + traversCost) {
                    //Plugin.Logger.LogInfo(string.Format("updating neighbor {0}",neighboorRoomID));
                    cost[neighboorRoomID] = cost[currentRoomID] + travelCost + traversCost;
                    entrance[neighboorRoomID] = node.ConnectedDoorNodeID;

                    if (! heap.Contains(neighboorRoomID)) {
                        heap.Add(neighboorRoomID, cost[neighboorRoomID]);
                    } else {
                        heap.Update(neighboorRoomID, cost[neighboorRoomID]);
                    }
                }
            }
        }

        if (entrance[endRoomID] == -1) return null; // the end room cannot be reached !

        List<int[]> path = new List<int[]>();

        currentRoomID = endRoomID;
        while (currentRoomID != startRoomID) {
            DoorNode nodeA = GetDoorAt(entrance[currentRoomID]);
            DoorNode nodeB = GetDoorAt(nodeA.ConnectedDoorNodeID);

            int[] nodesID = new int[2];
            nodesID[0] = nodeA.ConnectedDoorNodeID;
            nodesID[1] = entrance[currentRoomID];

            path.Add(nodesID);

            currentRoomID = nodeB.RoomNodeID;
        }

        path.Reverse();
        return path;
    }


    RoomNode GetRoomAt(int index) {
        return AllRooms[index];
    }

    DoorNode GetDoorAt(int index) {
        return AllDoors[index];
    }

    internal class RoomNode {
        internal Tile tile;
        internal List<int> AllDoorNodesID;

        public RoomNode(Tile tile) {
            this.tile = tile;
            this.AllDoorNodesID = new List<int>();
        }
    }

    internal class DoorNode {
        internal int RoomNodeID;
        internal int ConnectedDoorNodeID;
        internal int DoorObjectID;
        internal Vector3 position;

        public DoorNode(int RoomNodeID, int ConnectedDoorNodeID, int DoorObjectID, Vector3 position) {
            this.RoomNodeID = RoomNodeID;
            this.ConnectedDoorNodeID = ConnectedDoorNodeID;
            this.DoorObjectID = DoorObjectID;
            this.position = position;
        }
    }

    List<RoomNode> AllRooms;
    List<DoorNode> AllDoors;

    // unfortunate but necessary...
    List<Type> AllDoorTypes;
    List<object> AllDoorObjects;

}