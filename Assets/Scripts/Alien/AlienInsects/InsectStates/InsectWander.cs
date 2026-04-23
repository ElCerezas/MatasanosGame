using UnityEngine;

public class InsectWander : State
{
    private InsectStateMachine insect;

    public InsectWander(StateMachine _stateMachine, InsectStateMachine _insect) : base(_stateMachine)
    {
        insect = _insect;
    }

    public override void OnEnter()
    {
    }

    public override void OnUpdate()
    {
        insect.WanderInsect();
    }

}
