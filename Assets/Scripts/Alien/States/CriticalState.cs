using UnityEngine;

public class CriticalState : State
{
    public CriticalState(StateMachine _StateMachine) : base(_StateMachine)
    {
        
    }

    public override void OnEnter()
    {
        Debug.Log("Entrando en estado CRÍTICO");
    }
    public override void OnUpdate()
    {
        
    }
    public override void OnExit()
    {
        
    }

}
