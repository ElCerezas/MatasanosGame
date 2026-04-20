using Unity.Netcode;
using UnityEngine;

public class AlienStateManager : NetworkBehaviour
{
    StateMachine stateMachine;
    State currentState;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] float calmantDuration = 5f;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        stateMachine = new StateMachine();
    }

    private void ChangeState(State newState)
    {
        stateMachine.ChangeState(newState);
    }
    private void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
        }
    }
    private void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public void ApplyInyeccion(InyeccionType type)
    {
        Debug.Log($"Applying inyeccion of type {type} to alien");
        switch (type)
        {
            case InyeccionType.Calmante:
                //ChangeState(new CalmState(this, calmantDuration));
                break;
            case InyeccionType.Estimulante:
                
                break;
        }
    }
}
