using UnityEngine;

public class DesangradoState : State
{
    public DesangradoState(StateMachine _StateMachine) : base(_StateMachine)
    {
        
    }
    public override void OnEnter()
    {
       Debug.Log("Entrando en estado DESANGRADO");
    }
    public override void OnUpdate()
    {
        
    }
    public override void OnExit()
    {
        
    }
}
