using UnityEngine;

public class CalmState : State
{
    public CalmState(StateMachine _StateMachine) : base(_StateMachine)
    {
        
    }

    public override void OnEnter()
    {
        Debug.Log("Entrando en estado CALMADO");
    }
    public override void OnUpdate()
    {
        
    }
    public override void OnExit()
    {
        
    }

}
