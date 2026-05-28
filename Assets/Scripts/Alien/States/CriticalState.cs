using UnityEngine;

public class CriticalState : State
{
    private AlienStateManager alien;
    private Animation animation;
    
    public CriticalState(StateMachine _StateMachine) : base(_StateMachine)
    {
        
    }

    public override void OnEnter()
    {
        Debug.Log("Entrando en estado CRÍTICO");
        animation.clip = alien.Desangrado;
        animation.Play();
    }
    public override void OnUpdate()
    {
        
    }
    public override void OnExit()
    {
        
    }

}
