using System.Xml.Serialization;
using DunGen;
using GameNetcodeStuff;
using LostEmployee.Animations;
using LostEmployee.Infos;
using UnityEngine;
using UnityEngine.AI;

namespace LostEmployee.Actions;


class Interact {

    public static Action ToggleActive<T>(Percept<T> percept, bool value) {
        return Action.While(
                    agent => GameType<T>.IsActive(percept(agent)) != value,
                    Action.Sequence([
                        Turn.ToTarget(percept, 10),
                        Animation.Play("WorkingCrouched"),
                        Action.OnUpdate(agent => {
                            if (! GameType<T>.SetActive(percept(agent), value)) {
                                return Action.Status.FAILURE;
                            } else {
                                return Action.Status.SUCCESS;
                            }
                        })
                    ])
                );
    }

    public static Action ToggleActive<T>(T item, bool value) {
        return ToggleActive<T>(agent => item, value);
    }

    public static Action ToggleOpen(Percept<DoorLock> percept, bool value) {
        return Action.While(
                    agent => GameType<DoorLock>.IsOpen(percept(agent)) != value,
                    Action.Sequence([
                        Turn.ToTarget(percept, 10),
                        Animation.Play("Opening"),
                        Action.OnUpdate(agent => {
                            if (! GameType<TargetJoint2D>.SetOpen(percept(agent), value)) {
                                return Action.Status.FAILURE;
                            } else {
                                return Action.Status.SUCCESS;
                            }
                        })
                    ])
        );
    }

    public static Action ToggleOpen(DoorLock door, bool value) {
        return ToggleOpen(agent => door, value);
    }


    public static Action ToggleLocked(Percept<DoorLock> percept, bool value) {
        return Action.While(
                    agent => GameType<DoorLock>.IsLocked(percept(agent)) != value,
                    Action.Sequence([
                        Turn.ToTarget(percept, 10),
                        Animation.Play("OpeningLocking"),
                        Action.OnUpdate(agent => {
                            if (! GameType<DoorLock>.SetLocked(percept(agent), value)) {
                                return Action.Status.FAILURE;
                            } else {
                                return Action.Status.SUCCESS;
                            }
                        })
                    ])
        );
    }

    public static Action ToggleLocked(DoorLock door, bool value) {
        return ToggleLocked(agent => door, value);
    }


    public static Action Grab<T> (Percept<T> percept) {
        return Action.Sequence([
            Turn.ToTarget(percept, 10),
            Animation.Play("PickingFromGround"),
            Action.OnUpdate(agent => {
                if (! agent.inventory.Add(percept(agent))) {
                    return Action.Status.FAILURE;
                } else {
                    return Action.Status.SUCCESS;
                }
            })
        ]);
    }

    public static Action Grab<T> (T item) {
        return Grab(agent => item);
    }

    public static Action Drop<T> (Percept<T> percept, Vector3 position) {
        return Action.Sequence([
            Turn.ToTarget(position, 10),
            Animation.Play("WorkingCrouched"),
            Action.OnUpdate(agent => {
                if (! agent.inventory.Discard(position, percept(agent))) {
                    return Action.Status.FAILURE;
                } else {
                    return Action.Status.SUCCESS;
                }
            })
        ]);
    }

    public static Action Drop<T> (T item, Vector3 position) {
        return Drop(agent => item, position);
    }



    public static Action Push(Percept<PlayerControllerB> percept, float force) {
        return Action.While(
                    agent => InPushRange(agent, percept(agent)),
                    Action.Sequence([
                        Turn.ToTarget(percept, 10),
                        Action.OnUpdate(agent => DoPush(agent, percept(agent), force))
                    ])
        );

        bool InPushRange(LostEmployeeAI agent, PlayerControllerB player) {
            return Vector3.Distance(agent.transform.position, player.transform.position) <= 35f;
        }

        Action.Status DoPush(LostEmployeeAI agent, PlayerControllerB player, float force) {
            RaycastHit hit;
            if (force > 0f && !Physics.Linecast(agent.transform.position, player.transform.position + Vector3.up * 0.3f, out hit, 256, QueryTriggerInteraction.Ignore)) {
                float num = Vector3.Distance(player.transform.position, agent.transform.position);
                Vector3 vector = Vector3.Normalize(player.transform.position + Vector3.up * num - agent.transform.position) / (num * 0.35f) * force;
                
                if (vector.magnitude > 2f) {
                    if (vector.magnitude > 10f) {
                        player.CancelSpecialTriggerAnimations();
                    }
                    if (! player.inVehicleAnimation || (player.externalForceAutoFade + vector).magnitude > 50f) {
                        player.externalForceAutoFade += vector;
                    }
                }
            }
            return Action.Status.SUCCESS;
        }
    }

    public static Action Push(PlayerControllerB player, float force) {
        return Push(agent => player, force);
    }




    public static bool CanInteract<T>(LostEmployeeAI agent, T item) {
        if (Vector3.Distance(agent.transform.position, GameType<T>.GetPosition(item)) > agent.ReachingDistance) {
            if (! agent.inventory.Contains<T>(item)) {
                return false;
            }
        }
        if (! GameType<T>.IsInteractible(item)) return false;   // TODO : define IsInteractible !

        return true;
    }

    public static bool facing<T>(LostEmployeeAI agent, T item) {

        if (GameType<T>.GetPosition(item) -  agent.transform.position == Vector3.zero) return true; // we are ON the item.

        return Vector3.Angle(agent.transform.forward, GameType<T>.GetPosition(item) -  agent.transform.position) <= 10;
    }
}