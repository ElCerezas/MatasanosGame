using Unity.VisualScripting;
using UnityEngine;

public class StateMachine
{
    public State CurrentState;
    public void Initialize(State _InitialState)
    {
        CurrentState = _InitialState;
        CurrentState.OnEnter();
    }

    public void ChangeState(State _NewState)
    {
        if (_NewState == null) return;
        CurrentState?.OnExit();
        CurrentState = _NewState;
        CurrentState.OnEnter();
    }
    public void Update()
    {
        CurrentState?.OnUpdate();
    }
}
