using System;
using Unity.Netcode;
using UnityEngine;

public class AlienStateManager : NetworkBehaviour
{
    private StateMachine stateMachine;
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
    [SerializeField] private NetworkVariable<float> currentCalmant = new NetworkVariable<float>(100f);

    [Header("Debug")]
    [TextArea(6, 12)]
    [SerializeField] private string _debugStatus = "No iniciado";

    [Header("Animations")]
    [SerializeField] private Animator alienAnimator;

    private void UpdateDebugStatus()
    {
        _debugStatus =
            $"=== ALIEN {NetworkObjectId} ===\n" +
            $"Rol:           {(IsServer ? "SERVER" : "CLIENT")}\n" +
            $"Estado:        {currentActiveState.Value}\n" +
            $"HP:            {currentHealth.Value:F1} / {maxHealth}\n" +
            $"Calmant:       {currentCalmant.Value:F1} / {maxCalmant}\n" +
            $"BloodBag:      {(isBloodbagFull ? $"Conectada (ID {bloodBagID})" : "Sin bloodbag")}\n" +
            $"Multiplicador: {(!isBloodbagFull ? $"{criticalDamageMultiplier}x (crítico)" : "1x (normal)")}\n" +
            $"DmgTimer:      {damageTimer:F2} / {damageRate}";
    }

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
        EventBus.Subscribe<OnBloodBagDetached>(OnBloodBagDetachedReceived);
        EventBus.Subscribe<TaraHealedEvent>(OnTaraHealed);
        EventBus.Subscribe<OnWoundFocoHealStarted>(WoundIsBeingHealed);
        EventBus.Subscribe<OnWoundFocoHealEnded>(WoundHealingEnded);
        EventBus.Subscribe<OnDienteUnSnap>(OnDienteUnSnapped);

        UpdateDebugStatus();
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
        EventBus.Unsubscribe<OnBloodBagDetached>(OnBloodBagDetachedReceived);
        EventBus.Unsubscribe<TaraHealedEvent>(OnTaraHealed);
        EventBus.Unsubscribe<OnWoundFocoHealStarted>(WoundIsBeingHealed);
        EventBus.Unsubscribe<OnWoundFocoHealEnded>(WoundHealingEnded);
        EventBus.Unsubscribe<OnDienteUnSnap>(OnDienteUnSnapped);
        base.OnNetworkDespawn();
    }

    private void Awake()
    {
        stateMachine = new StateMachine();
        estornudoComponent = GetComponent<Estornudo>();
    }

    private void Update()
    {
        if (alienAnimator != null)
        {
            float healthPercent = maxHealth > 0 ? currentHealth.Value / maxHealth : 0f;
            float calmantPercent = maxCalmant > 0 ? currentCalmant.Value / maxCalmant : 0f;

            alienAnimator.SetFloat("Sleep%", calmantPercent);
            alienAnimator.SetFloat("Health", healthPercent);
        }

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
        UpdateDebugStatus();
    }

    private void HandleCalmant()
    {
        if (currentCalmant.Value > 0)
        {
            float decayPerSecond = maxCalmant / calmantDuration;
            currentCalmant.Value -= decayPerSecond * Time.deltaTime;

            if (currentCalmant.Value < 50)
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
        currentHealth.Value -= damage * multiplier;

        if (currentHealth.Value <= DesangradoThreshold && currentActiveState.Value != AlienStateEnum.Desangrado)
        {
            ChangeState(new DesangradoState(stateMachine, this));
        }

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            HandleDeathClientRpc();
        }
    }

    [ClientRpc]
    private void HandleDeathClientRpc()
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
        if (!IsServer || currentActiveState.Value == AlienStateEnum.Desangrado) return;
        switch (type)
        {
            case LiquidType.Calmante:
                UpdateCalmantStatusClientRpc(true);
                currentCalmant.Value = maxCalmant;
                if (!(stateMachine.CurrentState is CalmState))
                    ChangeState(new CalmState(this.stateMachine));
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

    private void OnBloodBagDetachedReceived(OnBloodBagDetached e)
    {
        if (e.BloodBagID != bloodBagID) return;
        if (!IsServer) return;
        isBloodbagFull = false;
        bloodBagID = 0;
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
        if (currentCalmant.Value > 0)
        {
            ChangeState(new CalmState(this.stateMachine));
        }
        else
        {
            ChangeState(new AlteredState(this.stateMachine, this));
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