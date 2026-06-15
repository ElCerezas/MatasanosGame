using UnityEngine;

public class InsectWander : State
{
    private InsectStateMachine insect;
    
    // Obstacle avoidance
    private float obstacleDetectionDistance = 1.5f;
    private float avoidanceForce = 2f;
    private int raycastLayers;
    private Vector3 avoidanceDirection = Vector3.zero;
    private float avoidanceTimer = 0f;
    
    // Stuck detection
    private Vector3 lastPosition;
    private float stuckCheckTimer = 0f;
    private float stuckCheckInterval = 1f;
    private float stuckThreshold = 0.1f;

    public InsectWander(StateMachine _stateMachine, InsectStateMachine _insect) : base(_stateMachine)
    {
        insect = _insect;
        raycastLayers = LayerMask.GetMask("Default"); // Ajusta según tus layers
    }

    public override void OnEnter()
    {
        lastPosition = insect.transform.position;
    }

    public override void OnUpdate()
    {
        DetectObstacles();
        CheckIfStuck();
        insect.WanderInsect();
    }

    private void DetectObstacles()
    {
        avoidanceTimer -= Time.deltaTime;
        
        // Raycast directo
        if (Physics.Raycast(insect.transform.position, insect.GetCurrentDirection(), obstacleDetectionDistance))
        {
            if (avoidanceTimer <= 0f)
            {
                avoidanceDirection = GetAvoidanceDirection();
                avoidanceTimer = 0.5f; 
            }
            insect.SetWanderDirection(avoidanceDirection);
        }
    }

    private Vector3 GetAvoidanceDirection()
    {
        Vector3[] directions = new Vector3[3]
        {
            Quaternion.Euler(0, 90, 0) * insect.GetCurrentDirection(),
            Quaternion.Euler(0, -90, 0) * insect.GetCurrentDirection(),
            -insect.GetCurrentDirection()
        };

        foreach (var dir in directions)
        {
            if (!Physics.Raycast(insect.transform.position, dir, obstacleDetectionDistance))
            {
                return dir.normalized;
            }
        }

        return -insect.GetCurrentDirection().normalized;
    }

    private void CheckIfStuck()
    {
        stuckCheckTimer += Time.deltaTime;
        
        if (stuckCheckTimer >= stuckCheckInterval)
        {
            float distanceMoved = Vector3.Distance(insect.transform.position, lastPosition);
            
            if (distanceMoved < stuckThreshold)
            {
                insect.ForceNewWanderDirection();
            }
            
            lastPosition = insect.transform.position;
            stuckCheckTimer = 0f;
        }
    }
}