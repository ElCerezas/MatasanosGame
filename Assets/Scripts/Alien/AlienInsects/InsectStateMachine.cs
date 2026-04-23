using Unity.Netcode;
using UnityEngine;

public class InsectStateMachine : NetworkBehaviour
{
    [SerializeField] float minimumForceToExplode;
    [SerializeField] float speed = 1f;
    [SerializeField] float attackSpeed = 5f;
    [SerializeField] float detectionRadius = 5f;
    StateMachine stateMachine;
    public NetworkVariable<InsectStatesEnum> currentActiveState = new NetworkVariable<InsectStatesEnum>(InsectStatesEnum.Wander); 

    private Rigidbody rb;
    private Vector3 direction;
    private float timer;
    private GameObject detectedPlayer;

    void Awake()
    {
        stateMachine = new StateMachine();
        rb = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        if (!IsServer || stateMachine == null) return;

        if (stateMachine.CurrentState == null)
        {
            stateMachine.Initialize(new InsectWander(stateMachine, this));
        }

        stateMachine.Update();
    }
    private void ChangeState(State newState)
    {
        stateMachine.ChangeState(newState);

        if (newState is InsectWander) currentActiveState.Value = InsectStatesEnum.Wander;
        else if (newState is InsectAttack) currentActiveState.Value = InsectStatesEnum.Attack;
    }
    public void AttackPlayer()
    {
        Vector3 direccionPlayer = (detectedPlayer.transform.position - transform.position).normalized;
        rb.linearVelocity = direccionPlayer * attackSpeed;
    }

    void CheckPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach(Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log("PlayerDetected!");
                detectedPlayer = hit.gameObject;
                ChangeState(new InsectAttack(stateMachine, this));
            }
        }
    }

    public void WanderInsect()
    {
        timer += Time.fixedDeltaTime;
        if (timer >= 2f)
        {
            NewWanderDirection();
            timer = 0f;
        }

        rb.linearVelocity = direction * speed;
        CheckPlayer();
    }
    void NewWanderDirection()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<NetworkObject>(out var netObj))
            {
                EventBus.Publish<OnInsectExplosion>(new OnInsectExplosion
                {
                    VictimID = netObj.NetworkObjectId
                });
                Destroy(gameObject);
            }
        }
        Vector3 relativeVelocity = collision.relativeVelocity;
        Debug.Log(relativeVelocity.magnitude);
        if (relativeVelocity.magnitude > minimumForceToExplode)
        {
            Destroy(gameObject);
        }
    }
}
