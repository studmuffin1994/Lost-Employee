using LostEmployee.Infos;
using UnityEngine;

namespace LostEmployee.Actions;

class Turn {
    public static Action ToTarget(Vector3 position, float stopWithin = -1) {
        return new TurnToTarget<Vector3>(position, 90, stopWithin);
    }

    public static Action ToTarget<T>(T target, float stopWithin = -1) {
        return new TurnToTarget<T>(target, 90, stopWithin);
    }

    public static Action ToTarget<T> (Percept<T> percept, float stopWithin = -1) {
        return new TurnToTarget<T>(percept, 90, stopWithin);
    }

    class TurnToTarget<T> : Action {
        Percept<T> percept;
        float speed;

        float stopWithin;
        public TurnToTarget(Percept<T> percept, float speed, float stopWithin = -10) {
            this.percept = percept;
            this.speed = speed;
            this.status = Status.PENDING;
            this.stopWithin = stopWithin;
        }

        public TurnToTarget(T item, float speed, float stopWithin = -1) {
            this.percept = agent => item;
            this.speed = speed;
            this.status = Status.PENDING;
            this.stopWithin = stopWithin;
        }

        public override void Update(LostEmployeeAI agent) {
            if (! pending()) return;

            Vector3 position = GameType<T>.GetPosition(percept(agent));

            Vector3 direction = position - agent.transform.position;
            direction.y = 0;

            if (direction == Vector3.zero) {
                status = Status.SUCCESS;
                return;
            }


            if (Vector3.Angle(agent.transform.forward, position - agent.transform.position) < stopWithin) {
                status = Status.SUCCESS;
                return;
            }




            
            direction = direction.normalized;

            Vector3 newDirection = Vector3.RotateTowards(agent.transform.forward, direction, speed * Time.deltaTime, 0.0f);
            agent.transform.rotation = Quaternion.LookRotation(newDirection);

        }
    }
}