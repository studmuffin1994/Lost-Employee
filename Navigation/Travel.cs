
using LostEmployee;
using LostEmployee.Actions;

class Travel : Action {


    public Action ToTarget<T>(Percept<T> target, float radius) {
        return new Action();
    }

    public Action ToTarget<T>(T target, float radius) {
        return ToTarget(agent => target, radius);
    }



    class TravelToTarger : Action {


        public override void AIInterval(LostEmployeeAI agent) {
            base.AIInterval(agent);
        }

        public override void Update(LostEmployeeAI agent) {
            agent.pathFinder.Update();

            if (agent.pathFinder.IsWaitingTraversal) {
                if (currentTraversal == null) {
                }
            }
        }

        Action Traversal( ) {
            return new Action();
        }

        Action ? currentTraversal = null;
    }
}