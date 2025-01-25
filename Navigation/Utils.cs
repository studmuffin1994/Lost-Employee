using UnityEngine;
using System.Collections.Generic;
using DunGen;
using System;
using System.Reflection;

using SecurityDoor = TerminalAccessibleObject;
using NormalDoor = DoorLock;
using UnityEngine.UIElements.UIR;
using UnityEngine.AI;


/// <summary>
/// A binary heap ( https://en.wikipedia.org/wiki/Binary_heap ) where we
/// exploit the fact that the elements are integers to perform the operations update and contain faster.
/// </summary>
class BinaryHeap {
    int capacity;
    int size;

    Tuple<int, float>[] array;
    int[] indexOf;

    public BinaryHeap(int capacity) {
        this.capacity = capacity;
        size = 0;

        array = new Tuple<int, float>[capacity + 1];
        indexOf = new int[capacity];
        for (int i = 0; i < capacity; i++) {
            indexOf[i] = -1;
        }
    }

    public bool Contains(int element) {
        return indexOf[element] != -1;
    }

    public bool isEmpty() {
        return size == 0;
    }

    public void Add(int element, float value) {

        array[size+1] = new Tuple<int, float>(element, value);
        indexOf[element] = size+1;
                    
        size++;

        HeapifyBottomToTop(size);
    }

    public void Update(int element, float value) {
        int index = indexOf[element];

        if (value < array[index].Item2) {
            array[index] = new Tuple<int, float>(element, value);
            HeapifyBottomToTop(index);
        } else {
            array[index] = new Tuple<int, float>(element, value);
            HeapifyTopToBottom(index);
        }
    }

    public int ExtractBest() {
        int element = array[1].Item1;
        indexOf[element] = -1;

        array[1] = array[size];
        indexOf[array[1].Item1] = 1;
        size--;

        HeapifyTopToBottom(1);

        return element;
    }

    void HeapifyBottomToTop(int index) {
        if (index <= 1) return;

        int parent = index / 2;

        if (array[index].Item2 < array[parent].Item2) {
            Swap(index, parent);
            HeapifyBottomToTop(parent);
        }
    }

    void HeapifyTopToBottom(int index) {
        int left = index * 2;
        int right = (index * 2) + 1;

        int best = index;

        if (left <= size) {
            if (array[left].Item2 < array[best].Item2) {
                 best = left;
            }
        }

        if (right <= size) {
            if (array[right].Item2 < array[best].Item2) {
                best = right;
            }
        }

        if (best != index) {
            Swap(index, best);
            HeapifyTopToBottom(best);
        }
    }

    void Swap(int indexA, int indexB) {
        int elementA = array[indexA].Item1;
        int elementB = array[indexB].Item1;

        Tuple<int, float> temp = array[indexA];
        array[indexA] = array[indexB];
        array[indexB] = temp;

        indexOf[elementA] = indexB;
        indexOf[elementB] = indexA;
    }
}


/// <summary>
/// Unfortunately, the implementation of the game doesn't allow us to get the value of certain fields easily.
/// DoorInfos regroup a set of utility function allowing us to access the relevant values.
/// </summary>
class DoorInfos {

    // return 2 positions in 2 side of the doorway...
    public static Vector3[] GetNodes(Doorway doorway) {
        float offset = 2f;

        Vector3[] nodes = new Vector3[2] {
            doorway.transform.position + offset * doorway.transform.forward,
            doorway.transform.position - offset * doorway.transform.forward
        };
        return nodes;
    }

    public static bool IsInDoorway(SecurityDoor door, Doorway doorway) {
        return door.transform.position == doorway.transform.position;
    }

    public static bool IsInDoorway(NormalDoor door, Doorway doorway) {
        return Vector3.Distance(door.transform.position, doorway.transform.position) <= 3f;
    }

    public static bool IsPoweredOn(SecurityDoor door) {
        FieldInfo infos = door.GetType().GetField("isPoweredOn", BindingFlags.NonPublic | BindingFlags.Instance);
        return (bool) infos.GetValue(door);
    }

    public static bool IsOpen(SecurityDoor door) {
        if (! IsPoweredOn(door)) return true;

        FieldInfo infos = door.GetType().GetField("isDoorOpen", BindingFlags.NonPublic | BindingFlags.Instance);
        return (bool) infos.GetValue(door);
    }

    public static bool IsOpen(NormalDoor door) {
        FieldInfo infos = door.GetType().GetField("isDoorOpened", BindingFlags.NonPublic | BindingFlags.Instance);
        return (bool) infos.GetValue(door);
    }


    public static bool IsLocked(SecurityDoor door) {
        return ! IsOpen(door);
    }

    public static bool IsLocked(NormalDoor door) {
        return door.isLocked;
    }
}