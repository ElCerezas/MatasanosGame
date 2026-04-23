using System;
using Unity.Netcode;
using UnityEngine;

public class AlienStateManager : NetworkBehaviour
{
    StateMachine stateMachine;
    State currentState;
    public NetworkVariable<AlienStateEnum> currentActiveState = new NetworkVariable<AlienStateEnum>(); //Para poder sincronizar animaciones y otras cosas

    [Header("Health Settings")]
    [SerializeField] private NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f);
    [SerializeField] private float maxHealth = 100f;
    private ulong bloodBagID;
    private bool isBloodbagFull = false;

    [Header("Damage Settings")]
    [SerializeField] private float damageAmount = 0.5f;
    [SerializeField] private float criticalDamageMultiplier = 2f;
    [SerializeField] private float damageRate = 1f;
    private float damageTimer = 0f;
    [Header("State Settings")]
    public float timeBetweenAttacks { get; private set; } = 3f;

    [Header("Calmant Settings")]
    [SerializeField] private float calmantDuration = 5f;
    private float calmantTimer = 0f;
    private bool isCalmantActive = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) currentHealth.Value = maxHealth;
        EventBus.Subscribe<OnInject>(OnInjectionReceived);
        currentHealth.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new OnAlienHealthChanged
            {
                AlienID = NetworkObjectId,
                CurrentHealth = newVal,
                MaxHealth = maxHealth
            });
        };
        EventBus.Subscribe<OnBloodBagEmpty>(OnBloodBagEmptyReceived);
        EventBus.Subscribe<OnBloodBagSnapped>(OnBloodBagConnected);
    }
    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<OnInject>(OnInjectionReceived);
        EventBus.Unsubscribe<OnBloodBagEmpty>(OnBloodBagEmptyReceived);
        base.OnNetworkDespawn();
    }

    void Awake()
    {
        stateMachine = new StateMachine();
    }

    void Update()
    {
        if (!IsServer || stateMachine == null) return;

        if (stateMachine.CurrentState == null)
        {
            stateMachine.Initialize(new AlteredState(stateMachine, this));
        }

        HandleStateLogic();
        HandleDamageOverTime();
        stateMachine.Update();
    }

    private void HandleStateLogic()
    {
        if (!IsServer) return;

        if (isCalmantActive)
        {
            calmantTimer += Time.deltaTime;
            if (calmantTimer >= calmantDuration)
            {
                isCalmantActive = false;
                calmantTimer = 0;
                Debug.Log("Calmante agotado.");
            }
        }

        //Si la bolsa está vacía, SIEMPRE debe estar en CRÍTICO
        if (!isBloodbagFull)
        {
            if (!(stateMachine.CurrentState is CriticalState))
            {
                ChangeState(new CriticalState(this.stateMachine));
            }
            return;
        }

        // Si la bolsa está llena, decidimos entre CALMADO o ALTERADO
        if (isCalmantActive)
        {
            // Si hay calmante y no estamos en estado Calmado, cambiamos
            if (!(stateMachine.CurrentState is CalmState))
            {
                ChangeState(new CalmState(this.stateMachine));
            }
        }
        else
        {
            // Si NO hay calmante y no estamos en estado Alterado, cambiamos
            if (!(stateMachine.CurrentState is AlteredState))
            {
                ChangeState(new AlteredState(stateMachine, this));
            }
        }
    }
    private void ChangeState(State newState)
    {
        stateMachine.ChangeState(newState);

        if (newState is CalmState) currentActiveState.Value = AlienStateEnum.Calmado;
        else if (newState is AlteredState) currentActiveState.Value = AlienStateEnum.Alterado;
        else if (newState is CriticalState) currentActiveState.Value = AlienStateEnum.Critico;
    }

    private void HandleDamageOverTime()
    {
        damageTimer += Time.deltaTime;

        if (damageTimer >= damageRate)
        {
            damageTimer -= damageRate;
            TakeDamage(damageAmount);
        }
    }
    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        float multiplier = (stateMachine.CurrentState is CriticalState) ? criticalDamageMultiplier : 1f;
        currentHealth.Value -= damage * multiplier;

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            HandleDeath();
        }
    }
    private void HandleDeath()
    {
        EventBus.Publish(new OnAlienDeath { AlienID = NetworkObjectId });
    }
    private void OnInjectionReceived(OnInject inject)
    {
        if (inject.VictimID != NetworkObjectId) return;
        ApplyInyeccion(inject.Type);
    }
    public void ApplyInyeccion(InyeccionType type)
    {
        if (!IsServer) return;
        switch (type)
        {
            case InyeccionType.Calmante:
                isCalmantActive = true;
                calmantTimer = 0f;

                if (stateMachine.CurrentState is AlteredState)
                {
                    ChangeState(new CalmState(this.stateMachine));
                }
                break;
            case InyeccionType.Estimulante:

                break;
        }
    }
    public void OnBloodBagConnected(OnBloodBagSnapped e)
    {
        bloodBagID = e.BloodBagID;
        isBloodbagFull = true;
    }

    private void OnBloodBagEmptyReceived(OnBloodBagEmpty e)
    {
        if (e.BloodBagID != bloodBagID) return;
        if (IsServer)
        {
            isBloodbagFull = false;
            ChangeState(new CriticalState(this.stateMachine));
        }
    }

    public void NotifyParasiteAttack()
    {
        if (!IsServer) return;

        NotifyParasiteClientRpc();
    }

    [ClientRpc]
    private void NotifyParasiteClientRpc()
    {
        EventBus.Publish(new OnAlienParasiteAttack { AlienID = NetworkObjectId });
    }
}
