using UnityEngine;

public class DesangradoState : State
{
    private AlienStateManager alienStateManager;
    private Animation animation;

    public DesangradoState(StateMachine _StateMachine, AlienStateManager _AlienStateManager) : base(_StateMachine)
    {
        alienStateManager = _AlienStateManager;
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
