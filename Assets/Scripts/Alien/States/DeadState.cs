using UnityEngine;

public class DeadState : State
{
    private AlienStateManager alienStateManager;

    public DeadState(StateMachine _StateMachine, AlienStateManager _AlienStateManager) : base(_StateMachine)
    {
        alienStateManager = _AlienStateManager;
    }
    public override void OnEnter()
    {
        Debug.Log("Entrando en estado MUERTO");
        EventBus.Publish(new AlienDeath());
    }
    public override void OnUpdate()
    {
        
    }
    public override void OnExit()
    {
        
    }
}
