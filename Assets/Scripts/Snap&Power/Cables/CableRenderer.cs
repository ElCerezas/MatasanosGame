using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class CableRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CableConstraint constraint;

    [Header("Simulation")]
    [SerializeField] int segments = 12;
    [SerializeField] int solverIterations = 8;
    [SerializeField] float gravity = 9.8f;
    [SerializeField] float segmentDamping = 0.98f;

    [Header("Visuals")]
    [SerializeField] float cableWidth = 0.02f;

    LineRenderer lr;
    Vector3[] positions;
    Vector3[] prevPositions;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.startWidth = cableWidth;
        lr.endWidth = cableWidth;
        lr.useWorldSpace = true;
    }

    void Update()
    {
        if (constraint == null) return;
        if (positions == null)
        {
            Vector3 a = constraint.NetPlugPos.Value;
            Vector3 b = constraint.NetSocketPos.Value;
            if (a == Vector3.zero && b == Vector3.zero) return;
            InitParticles(a, b);
            return;
        }

        SimulateCable();
        UpdateLineRenderer();
    }

    void InitParticles(Vector3 start, Vector3 end)
    {
        positions = new Vector3[segments + 1];
        prevPositions = new Vector3[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            positions[i] = Vector3.Lerp(start, end, t);
            prevPositions[i] = positions[i];
        }
    }

    void SimulateCable()
    {
        Vector3 anchorA = constraint.NetPlugPos.Value;
        Vector3 anchorB = constraint.NetSocketPos.Value;

        float segLen = Mathf.Max(Vector3.Distance(anchorA, anchorB) / segments, 0.05f);

        // 1. Verlet
        for (int i = 1; i < segments; i++)
        {
            Vector3 vel = (positions[i] - prevPositions[i]) * segmentDamping;
            prevPositions[i] = positions[i];
            positions[i] += vel;
            positions[i] += Vector3.down * (gravity * Time.deltaTime * Time.deltaTime);
        }
        for (int i = 0; i < solverIterations; i++)
        {
            positions[0] = anchorA;
            positions[segments] = anchorB;

            for (int s = 0; s < segments; s++)
            {
                Vector3 delta = positions[s + 1] - positions[s];
                float len = delta.magnitude;
                if (len < 0.0001f) continue;

                float error = (len - segLen) / len;
                Vector3 corr = delta * 0.5f * error;

                if (s != 0) positions[s] += corr;
                if (s + 1 != segments) positions[s + 1] -= corr;
            }
        }
    }

    void UpdateLineRenderer()
    {
        for (int i = 0; i <= segments; i++)
            lr.SetPosition(i, positions[i]);
    }
}