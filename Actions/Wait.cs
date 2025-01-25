using UnityEngine;

namespace LostEmployee.Actions;


class Wait {
    public static Action ForDuration(float seconds) {
        return new WaitForDuration(seconds);
    }
    public static Action ForCondition(Percept<bool> condition, float interval = 0f) {
        return new WaitForCondition(condition, interval);
    }

    class WaitForDuration : Action {
        float remaining;

        public WaitForDuration(float seconds) {
            remaining = seconds;
            status = Status.PENDING;
        }

        public override void Update(LostEmployeeAI agent) {
            if (! pending()) return;

            remaining -= Time.deltaTime;
            if (remaining <= 0) status = Status.SUCCESS;
        }

        /*
        public override void Update(LostEmployeeAI agent) {
            if (! pending()) return;

            remaining -= Time.deltaTime;
            if (remaining <= 0) status = Status.SUCCESS;
        }
        */
    }

    class WaitForCondition : Action {
        Percept<bool> condition;
        float remaining;
        float interval;

        public WaitForCondition(Percept<bool> condition, float timeBtwChecks) {
            this.condition = condition;
            this.interval = timeBtwChecks;
            this.remaining = 0;
            status = Status.PENDING;
        }

        public override void Update(LostEmployeeAI agent) {
            if (! pending()) return;

            remaining -= Time.deltaTime;
            if (remaining <= 0) {
                remaining = interval;
                if (condition(agent)) {
                    status = Status.SUCCESS;
                }
            }
        }

        /*
        public override void Update(LostEmployeeAI agent) {
            if (! pending()) return;

            remaining -= Time.deltaTime;
            if (remaining <= 0) {
                remaining = interval;
                if (condition(agent)) {
                    status = Status.SUCCESS;
                }
            }
        }
        */
    }
}