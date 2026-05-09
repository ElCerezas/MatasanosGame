using UnityEngine;

public class QuejidoConstanteState : State
{
    public QuejidoConstanteState(StateMachine _StateMachine) : base(_StateMachine)
    {
        
    }

    public override void OnEnter()
    {
        Debug.Log("Entrando en estado QUEJIDO CONSTANTE");
    }
    public override void OnUpdate()
    {
        
    }
    public override void OnExit()
    {
        
    }
}
