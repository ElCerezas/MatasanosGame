using UnityEngine;

public class InsectAttack : State
{
    private InsectStateMachine insect;

    public InsectAttack(StateMachine _stateMachine, InsectStateMachine _insect) : base(_stateMachine)
    {
        insect = _insect;
    }

    public override void OnEnter()
    {
    }

    public override void OnUpdate()
    {
        insect.AttackPlayer();
    }
}
