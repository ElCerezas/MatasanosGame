using UnityEngine;

public class InquietoState : State
{
    public InquietoState(StateMachine _StateMachine) : base(_StateMachine)
    {
    }

    public override void OnEnter()
    {
        Debug.Log("Entrando en estado INQUIETO");   
    }
    public override void OnUpdate()
    {
        
    }
    public override void OnExit()
    {
        
    }

}
