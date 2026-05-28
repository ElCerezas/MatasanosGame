using System;
using Unity.Netcode;
using UnityEngine;

public class AlienStateManager : NetworkBehaviour
{
    StateMachine stateMachine;
    State currentState;
    public NetworkVariable<AlienStateEnum> currentActiveState = new NetworkVariable<AlienStateEnum>();

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
    [SerializeField] private float DesangradoThreshold = 10f;
    public Estornudo estornudoComponent;

    [Header("Calmant Settings")]
    [SerializeField] private float calmantDuration = 5f;
    [SerializeField] private float maxCalmant = 100f;
    [SerializeField] private NetworkVariable<float> currentCalmant = new NetworkVariable<float>(0f);
    
    [Header("Animations")]
    [SerializeField] public AnimationClip Estornudo;
    [SerializeField] public AnimationClip Quejido;
    [SerializeField] public AnimationClip Chupon;
    [SerializeField] public AnimationClip Masticar;
    [SerializeField] public AnimationClip Parasitador;
    [SerializeField] public AnimationClip Inquieto;
    [SerializeField] public AnimationClip Inquieto_IN;
    [SerializeField] public AnimationClip Inquieto_OUT;
    [SerializeField] public AnimationClip Calmado;
    [SerializeField] public AnimationClip Calmado_IN;
    [SerializeField] public AnimationClip Calmado_OUT;
    [SerializeField] public AnimationClip Desangrado;
    [SerializeField] public AnimationClip Desangrado_IN;
    [SerializeField] public AnimationClip Desangrado_OUT;
    [SerializeField] public AnimationClip QuejidoConstante;
    [SerializeField] public AnimationClip QuejidoConstante_IN;
    [SerializeField] public AnimationClip QuejidoConstante_OUT;
    

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

        currentCalmant.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new OnCalmantChanged
            {
                AlienID = NetworkObjectId,
                CurrentCalmant = newVal,
                MaxCalmant = maxCalmant
            });
        };

        currentActiveState.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new OnAlienStateChanged
            {
                AlienID = NetworkObjectId,
                NewState = newVal
            });
        };

        EventBus.Subscribe<OnBloodBagEmpty>(OnBloodBagEmptyReceived);
        EventBus.Subscribe<OnBloodBagSnapped>(OnBloodBagConnected);
        EventBus.Subscribe<TaraHealedEvent>(OnTaraHealed);
        EventBus.Subscribe<OnWoundFocoHealStarted>(WoundIsBeingHealed);
        EventBus.Subscribe<OnWoundFocoHealEnded>(WoundHealingEnded);
        EventBus.Subscribe<OnDienteUnSnap>(OnDienteUnSnapped);
    }

    private void OnDienteUnSnapped(OnDienteUnSnap e)
    {
        if (e.UnSnappedTooth.CompareTag("GoodTeeth"))
        {
            estornudoComponent.ExecuteEstornudo();
        }
    }

    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<OnInject>(OnInjectionReceived);
        EventBus.Unsubscribe<OnBloodBagEmpty>(OnBloodBagEmptyReceived);
        EventBus.Unsubscribe<OnBloodBagSnapped>(OnBloodBagConnected);
        EventBus.Unsubscribe<TaraHealedEvent>(OnTaraHealed);
        EventBus.Unsubscribe<OnWoundFocoHealStarted>(WoundIsBeingHealed);
        EventBus.Unsubscribe<OnWoundFocoHealEnded>(WoundHealingEnded);
        EventBus.Unsubscribe<OnDienteUnSnap>(OnDienteUnSnapped);
        base.OnNetworkDespawn();
    }

    void Awake()
    {
        stateMachine = new StateMachine();
        estornudoComponent = GetComponent<Estornudo>();
    }

    void Update()
    {
        if (!IsServer || stateMachine == null) return;

        if (stateMachine.CurrentState == null)
        {
            var initialState = new InquietoState(stateMachine);
            stateMachine.Initialize(initialState);
            currentActiveState.Value = AlienStateEnum.Inquieto;
        }

        HandleCalmant();
        HandleDamageOverTime();
        stateMachine.Update();
    }

    private void HandleCalmant()
    {
        if (currentCalmant.Value > 0)
        {
            float decayPerSecond = maxCalmant / calmantDuration;
            currentCalmant.Value -= decayPerSecond * Time.deltaTime;

            if (currentCalmant.Value < 0)
            {
                currentCalmant.Value = 0f;
                UpdateCalmantStatusClientRpc(false);
                ChangeState(new InquietoState(stateMachine));
            }
        }
    }

    private void ChangeState(State newState)
    {
        if (currentActiveState.Value == AlienStateEnum.Desangrado) return;
        stateMachine.ChangeState(newState);
        if (newState is CalmState) currentActiveState.Value = AlienStateEnum.Calmado;
        else if (newState is AlteredState) currentActiveState.Value = AlienStateEnum.Alterado;
        else if (newState is CriticalState) currentActiveState.Value = AlienStateEnum.Critico;
        else if (newState is InquietoState) currentActiveState.Value = AlienStateEnum.Inquieto;
        else if (newState is EstornudoState) currentActiveState.Value = AlienStateEnum.Estornudo;
        else if (newState is DesangradoState) currentActiveState.Value = AlienStateEnum.Desangrado;
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

        float multiplier = !isBloodbagFull ? criticalDamageMultiplier : 1f;
        //Debug.Log($"Alien {NetworkObjectId} taking damage: {damage} with multiplier: {multiplier}");
        currentHealth.Value -= damage * multiplier;

        if (currentHealth.Value <= DesangradoThreshold && currentActiveState.Value != AlienStateEnum.Desangrado)
        {
            ChangeState(new DesangradoState(stateMachine, this));
        }

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

    public void ApplyInyeccion(LiquidType type)
    {
        Debug.Log("Hello");
        if (!IsServer || currentActiveState.Value == AlienStateEnum.Desangrado) return;
        Debug.Log("Hello1");
        switch (type)
        {
            case LiquidType.Calmante:
                UpdateCalmantStatusClientRpc(true);

                currentCalmant.Value = maxCalmant;

                if (!(stateMachine.CurrentState is CalmState))
                {
                    ChangeState(new CalmState(this.stateMachine));
                }
                break;
            default:
                ChangeState(new AlteredState(this.stateMachine, this));
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
            ChangeState(new InquietoState(this.stateMachine));
        }
    }

    private void OnTaraHealed(TaraHealedEvent data)
    {
        if (currentActiveState.Value == AlienStateEnum.Inquieto)
        {
            Debug.Log("Alien state changed to Altered due to Tara healed");
            ChangeState(new AlteredState(this.stateMachine, this));
        }
    }

    public void WoundCreated()
    {
        if (currentActiveState.Value == AlienStateEnum.Desangrado) return;
        ChangeState(new AlteredState(this.stateMachine, this));
    }
    private void WoundIsBeingHealed(OnWoundFocoHealStarted started)
    {
        if (currentActiveState.Value == AlienStateEnum.Desangrado) return;
        ChangeState(new QuejidoConstanteState(this.stateMachine));
    }
    private void WoundHealingEnded(OnWoundFocoHealEnded ended)
    {
        if (currentActiveState.Value == AlienStateEnum.Desangrado) return;
        ChangeState(new CalmState(this.stateMachine));
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

    [ClientRpc]
    private void UpdateCalmantStatusClientRpc(bool isCalmantActive)
    {
        if (isCalmantActive)
        {
            EventBus.Publish(new OnAlienCalmantUsed { AlienID = NetworkObjectId });
        }
        else
        {
            EventBus.Publish(new OnCalmantEnded { AlienID = NetworkObjectId });
        }
    }
}