namespace LostEmployee.Actions;


class Animation {

    public static Action Play(string animation, float lockFor = 0f) {
        return new AnimationPlay(animation, lockFor);
    }


    class AnimationPlay : Action {
        string animation;
        float lockFor = 0;
        bool launched = false;

        public AnimationPlay(string animation, float lockFor=0f) {
            this.animation = animation;
            this.lockFor = lockFor;
            status = Status.PENDING;
        }

        public override void Update(LostEmployeeAI agent) {
            if (! pending()) return;

            if (! launched) {
                agent.animatorHandler.playOneShot(animation);
                launched = true;
            }
            if (agent.creatureAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= lockFor) {
                status = Status.SUCCESS;
            }
        }
    }
}