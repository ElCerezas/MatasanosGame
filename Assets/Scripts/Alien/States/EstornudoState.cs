using UnityEngine;

public class EstornudoState : State
{
    private Estornudo estornudo;

    public EstornudoState(StateMachine _StateMachine, Estornudo _Estornudo) : base(_StateMachine)
    {
        this.estornudo = _Estornudo;
    }

    public override void OnEnter()
    {
        Debug.Log("Entrando en estado ESTORNUDO");
        estornudo.ExecuteEstornudo();
    }
    public override void OnUpdate()
    {

    }
    public override void OnExit()
    {

    }
}
