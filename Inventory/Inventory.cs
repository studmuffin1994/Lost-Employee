using LethalLib;
using LostEmployee.Infos;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


namespace LostEmployee.Objects;

class Inventory {

    public Inventory(int turretCapacity, int landmineCapacity, int itemCapacity) {
        this.turretCapacity = turretCapacity;
        this.landmineCapacity = landmineCapacity;
        this.itemCapacity = itemCapacity;

        turrets = new List<Turret>();
        landmines = new List<Landmine>();
        items = new List<GrabbableObject>();
    }

    public void Start (LostEmployeeAI agent) {
        this.agent = agent;
    }

    public bool Add<T> (T item) {
        switch (item) {
            case Turret turret :
                if (turrets.Count + 1 > turretCapacity) return false;
                if (! AttachToAgent<Turret>(turret)) return false;
                turrets.Add(turret); 
                return true;
            case Landmine landmine :
                if (landmines.Count + 1 > landmineCapacity) return false;
                if (! AttachToAgent<Landmine>(landmine)) return false;
                landmines.Add(landmine); 
                return true;
            case GrabbableObject loot :
                if (items.Count + 1 > itemCapacity) return false;
                if (! AttachToAgent<GrabbableObject>(loot)) return false;
                items.Add(loot); 
                return true;
            default :
                return false;
        }
    }

    public bool Discard<T>(Vector3 position, T? item = default) {
        int index;
        switch (item) {
            case Turret turret:
                index = (item == null) ? turrets.Count -1 : turrets.IndexOf(turret);
                if (index == -1) return false;
                if (! DetachFromAgent<Turret>(turret, position)) return false;
                turrets.RemoveAt(index);
                return true;
            case Landmine landmine:
                index = (item == null) ? landmines.Count -1 : landmines.IndexOf(landmine);
                if (index == -1) return false;
                if (! DetachFromAgent<Landmine>(landmine, position)) return false;
                landmines.RemoveAt(index);
                return true;
            case GrabbableObject loot:
                index = (item == null) ? items.Count -1 : items.IndexOf(loot);
                if (index == -1) return false;
                if (! DetachFromAgent<GrabbableObject>(loot, position)) return false;
                items.RemoveAt(index);
                return true;
            default :
                return false;
        }
    }

    public bool Contains<T>(T item) {
        switch (item) {
            case Turret turret :
                return turrets.Contains(turret);
            case Landmine landmine :
                return landmines.Contains(landmine);
            case GrabbableObject loot :
                return items.Contains(loot);
            default :
                return false;
        }
    }

    public int Count<T>() {
        if (typeof(T) == typeof(Landmine)) return landmines.Count;
        if (typeof(T) == typeof(Turret)) return turrets.Count;
        if (typeof(T) == typeof(GrabbableObject)) return items.Count;

        return 0;
    }

    public int Count() {
        return landmines.Count + turrets.Count + items.Count;
    }

    public bool IsFull<T>() {
        if (typeof(T) == typeof(Landmine)) return landmines.Count == landmineCapacity;
        if (typeof(T) == typeof(Turret)) return turrets.Count == turretCapacity;
        if (typeof(T) == typeof(GrabbableObject)) return items.Count == itemCapacity;
        return true;
    }

    int turretCapacity, landmineCapacity, itemCapacity;

    List<Turret> turrets;
    List<Landmine> landmines;
    List<GrabbableObject> items;

    LostEmployeeAI agent;

    // TODO : ENABLE / DISABLE MESHRENDERER OF GRABBABLEOBJECT.
    private bool AttachToAgent<T>(T item) {
        switch (item) {
            case Landmine landmine :
                return GameType<Landmine>.Despawn(landmine, true);
            case Turret turret :
                return GameType<Turret>.Despawn(turret, true);
            case GrabbableObject loot :
                loot.parentObject = agent.transform;
                loot.hasHitGround = false;
                loot.GrabItemFromEnemy(agent);
                loot.EnablePhysics(false);
                HoarderBugAI.grabbableObjectsInMap.Remove(loot.gameObject);
                return true;
            default :
                return false;
        }
    }

    private bool DetachFromAgent<T>(T item, Vector3 position) {
        switch (item) {
            case Landmine landmine :
                return GameType<Landmine>.Respawn(landmine, position);
            case Turret turret :
                return GameType<Turret>.Respawn(turret, position);
            case GrabbableObject loot :
                loot.parentObject = null;
	            loot.transform.SetParent(StartOfRound.Instance.propsContainer, true);
	            loot.EnablePhysics(true);
	            loot.fallTime = 0f;
	            loot.startFallingPosition = loot.transform.parent.InverseTransformPoint(loot.transform.position);
	            loot.targetFloorPosition = loot.transform.parent.InverseTransformPoint(position);
                loot.targetFloorPosition.y += loot.itemProperties.verticalOffset;
	            loot.floorYRot = -1;
	            loot.DiscardItemFromEnemy();
                return true;
            default :
                return false;
        }
    }
}