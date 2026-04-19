using UnityEngine;

public abstract class State
{
    protected StateMachine StateMachine;

    public State(StateMachine _StateMachine)
    {
        this.StateMachine = _StateMachine;
    }

    public virtual void OnEnter() {}
    public virtual void OnUpdate() {}
    public virtual void OnExit() {}
}
