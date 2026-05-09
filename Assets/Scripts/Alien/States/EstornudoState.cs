using UnityEngine;

public class EstornudoState : State
{
    public EstornudoState(StateMachine _StateMachine) : base(_StateMachine)
    {
        
    }

    public override void OnEnter()
    {
       Debug.Log("Entrando en estado ESTORNUDO");
    }
    public override void OnUpdate()
    {
        
    }
    public override void OnExit()
    {
        
    }
}
