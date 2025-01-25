using System.Runtime.CompilerServices;
using LostEmployee.Animations;

namespace LostEmployee.Actions;

class Misc {

    public static Action Idle() {
        return new Action();
    }
    public static Action Die() {
        return Action.Sequence([
            Animation.Play("Death"),
            Action.OnUpdate(agent => {
                agent.isEnemyDead = true;
                return Action.Status.SUCCESS;
            })
        ]);
    }

    public static Action Print(string msg) {
        return new MiscPrint(msg);
    }

    class MiscDie : Action {

        bool animationDone = false;

        public MiscDie() {
            status = Status.PENDING;
        }

        public override void Update(LostEmployeeAI agent) {
            if(! pending()) return;

            if (! animationDone) {
                agent.agent.isStopped = true;
                agent.animatorHandler.playOneShot(AnimationOneShot.Death);
                animationDone = true;
                return;
            }
            agent.isEnemyDead = true;
            status = Status.SUCCESS;
        }
    }

    class MiscPrint : Action {
        string msg;

        public MiscPrint(string msg) {
            this.msg = msg;
            status = Status.PENDING;
        }

        public override void Update(LostEmployeeAI agent){
            if (! pending()) return;
            
            Plugin.Logger.LogInfo(msg);
            status = Status.SUCCESS;
        }
    }

}