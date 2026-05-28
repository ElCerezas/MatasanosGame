using UnityEngine;

public class CalmState : State
{
    private AlienStateManager alien;
    private Animation animation;
    
    public CalmState(StateMachine _StateMachine) : base(_StateMachine)
    {
        
    }

    public override void OnEnter()
    {
        Debug.Log("Entrando en estado CALMADO");
        animation.clip = alien.Calmado;
        animation.Play();
    }
    public override void OnUpdate()
    {
        
    }
    public override void OnExit()
    {
        
    }

}
