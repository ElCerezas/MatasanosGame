using UnityEngine;

public class AlteredState : State
{
    private AlienStateManager alien;
    private float parasiteTimer;

    public AlteredState(StateMachine _stateMachine, AlienStateManager _alien) : base(_stateMachine)
    {
        alien = _alien;
    }

    public override void OnEnter()
    {
        parasiteTimer = 0f;
    }

    public override void OnUpdate()
    {
        parasiteTimer += Time.deltaTime;

        if (parasiteTimer >= alien.timeBetweenAttacks)
        {
            parasiteTimer = 0f;
            ParasyteAttack();
        }
    }

    private void ParasyteAttack()
    {
        alien.NotifyParasiteAttack(); 
    }
}