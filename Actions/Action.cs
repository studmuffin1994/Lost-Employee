using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using UnityEngine;
using UnityEngine.AI;

namespace LostEmployee.Actions;


internal delegate T Percept<T> (LostEmployeeAI agent);

class Action {

    public enum Status {
        PENDING,
        SUCCESS,
        FAILURE
    }

    public Status status {get; protected set; }
    public Action() { this.status = Status.PENDING; }
    public virtual void AIInterval(LostEmployeeAI agent) {}

    public virtual void Update(LostEmployeeAI agent) {}
    public bool pending() {return status == Status.PENDING;}
    public bool succeeded() {return status == Status.SUCCESS;}
    public bool failed() {return status == Status.FAILURE;}

    public Action Clone() {
        return (Action) MemberwiseClone();
    }

    public static Action Success() {
        Action action = new Action();
        action.status = Status.SUCCESS;
        return action;
    }

    public static Action Failure() {
        Action action = new Action();
        action.status = Status.FAILURE;
        return action;
    }

    public static Action Sequence(IEnumerable<Action> actions, bool continueOnFailure = false) {
        return new Sequence(actions, continueOnFailure);
    }

    public static Action While(Percept<bool> condition, Action action) {
        return new While(condition, action);
    }

    public static Action Switch(Percept<int> rule, Action[] actions) {
        return new Choose(rule, actions);
    }

    public static Action IfElse(Percept<bool> rule, Action[] actions) {
        return new Choose(rule, actions);
    }

    public static Action OnUpdate(Percept<Status> code) {
        return new OnUpdate(code);
    }

    public static Action OnAIInterval(Percept<Status> code) {
        return new OnAIInterval(code);
    }

}

class Sequence : Action {
    readonly IEnumerator<Action> actions;
    bool continueOnFailure;

    public Sequence(IEnumerable<Action> actions, bool continueOnFailure = false) {
        this.continueOnFailure = continueOnFailure;
        this.actions = actions.GetEnumerator();
        if (! this.actions.MoveNext()) {
            status = Status.SUCCESS;
        } else {
            status = Status.PENDING;
        }
    }

    public override void AIInterval(LostEmployeeAI agent) {
        if (! pending()) return;

        actions.Current.AIInterval(agent);

        if (actions.Current.failed()) {
            if (! continueOnFailure) {
                status = Status.FAILURE;
                return;
            }
        }

        if (!actions.Current.pending()) {
            if (! actions.MoveNext()) {
                status = Status.SUCCESS;
            }
        }
    }

    public override void Update(LostEmployeeAI agent) {
        if (! pending()) return;

        actions.Current.Update(agent);

        if (actions.Current.failed()) {
            if (! continueOnFailure) {
                status = Status.FAILURE;
                return;
            }
        }

        if (! actions.Current.pending()) {
            if (! actions.MoveNext()) {
                status = Status.SUCCESS;
            }
        }
    }
}

/// <summary>
/// A Loop is a Sequence of action repeating infinitely. 
/// </summary>
class Loop : Sequence {

    public Loop(IEnumerable<Action> actions, bool continueOnFailure = false) : base(RepeatForever(actions), continueOnFailure) {}

    static IEnumerable<Action> RepeatForever(IEnumerable<Action> actions) {
        while (true) {
            foreach (Action action in actions) {
                yield return action.Clone();
            }
        }
    }
}

/// <summary>
/// update the action while the percept is true.
/// 
/// </summary>
class While : Action {

    Percept<bool> condition;
    Action action;

    public While(Percept<bool> condition, Action action) {
        this.condition = condition;
        this.action = action;
        status = Status.PENDING;
    }

    public override void AIInterval(LostEmployeeAI agent) {
        if (! pending()) return;

        if (! condition(agent)) {
            status = Status.SUCCESS;
            return;
        } else {
            action.AIInterval(agent);
            if (! action.pending()) {
                status = action.status;
            }
        }
    }

    public override void Update(LostEmployeeAI agent) {
        if (! pending()) return;

        if (! condition(agent)) {
            status = Status.SUCCESS;
            return;
        } else {
            action.Update(agent);
            if (! action.pending()) {
                status = action.status;
            }
        }
    }
}


class Choose : Action {
    Percept<int> rule;
    Action[] actions;

    int choice = -1;

    public Choose(Percept<int> rule, Action[] actions) {
        this.rule = rule;
        this.actions = actions;
        status = Status.PENDING;
    }

    public Choose(Percept<bool> rule, Action[] actions) {
        this.rule = agent => rule(agent) == true ? 1 : 0;
        this.actions = actions;
    }

    public override void AIInterval(LostEmployeeAI agent) {
        if (! pending()) return;

        if (choice == -1) {
            choice = rule(agent);
        }
        actions[choice].AIInterval(agent);
        status = actions[choice].status;
    }

    public override void Update(LostEmployeeAI agent){
        if (! pending()) return;

        if (choice == -1) {
            choice = rule(agent);
        }
        actions[choice].Update(agent);
        status = actions[choice].status;
    }
}


class OnUpdate : Action {

    Percept<Status> code;

    public OnUpdate(Percept<Status> code) {
        this.code = code;
        this.status = Status.PENDING;
    }

    public override void Update(LostEmployeeAI agent) {
        if (! pending()) return;

        status = code(agent);
    }
}

class OnAIInterval : Action {
    Percept<Status> code;

    public OnAIInterval(Percept<Status> code) {
        this.code = code;
        this.status = Status.PENDING;
    }

    public override void AIInterval(LostEmployeeAI agent) {
        if (! pending()) return;

        status = code(agent);
    }
}