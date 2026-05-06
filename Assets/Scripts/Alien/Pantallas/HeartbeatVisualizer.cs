using UnityEngine;

public class PlaneHeartbeatVisualizer : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Referencias")]
    [SerializeField] private Transform planeTransform;

    [Header("Parámetros de la onda")]
    [SerializeField] private float bpm = 75f;
    [Range(0.1f, 3f)]
    [SerializeField] private float amplitude = 1f;
    [SerializeField] private bool flatline = false;

    [Header("Visualización")]
    [SerializeField] private Color waveColor = Color.red;
    [Range(0.05f, 0.5f)]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private int pointsPerLine = 500;

    [Header("Pantalla")]
    [SerializeField] private float planeWidth = 10f;
    [SerializeField] private bool autoCycles = true; 
    [SerializeField] private int targetCyclesAt60BPM = 4;
    [SerializeField] private float scrollSpeed = 1f;
    [SerializeField] private float baselineNoise = 0.01f;
    
    private int cyclesVisible = 2;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = waveColor;
        lineRenderer.endColor = waveColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = pointsPerLine;

        if (planeTransform == null)
            planeTransform = transform;
    }

    void Update()
    {
        lineRenderer.startColor = waveColor;
        lineRenderer.endColor = waveColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        GenerateHeartbeatWave();
    }

    void GenerateHeartbeatWave()
    {
        if (autoCycles)
        {
            // A 60 BPM mostrará targetCyclesAt60BPM
            //Contra más BPM más ciclos se mostrarán
            cyclesVisible = Mathf.Max(1, Mathf.RoundToInt((bpm / 60f) * targetCyclesAt60BPM));
        }

        Vector3[] positions = new Vector3[pointsPerLine];

        for (int i = 0; i < pointsPerLine; i++)
        {
            float normalizedX = (float)i / (pointsPerLine - 1);
            float x = Mathf.Lerp(-planeWidth / 2f, planeWidth / 2f, normalizedX);
            float z = 0f;

            if (flatline)
            {
                z = Random.Range(-baselineNoise, baselineNoise);
            }
            else
            {

                float phase = (normalizedX * cyclesVisible) + (Time.time * scrollSpeed);
                phase = Mathf.Repeat(phase, 1.0f);

                // Ondas Gaussianas
                float pWave = Mathf.Exp(-Mathf.Pow((phase - 0.25f) * 25f, 2f)) * 0.15f;
                float qWave = -Mathf.Exp(-Mathf.Pow((phase - 0.45f) * 60f, 2f)) * 0.20f;
                float rWave = Mathf.Exp(-Mathf.Pow((phase - 0.50f) * 40f, 2f)) * 1.00f;
                float sWave = -Mathf.Exp(-Mathf.Pow((phase - 0.55f) * 60f, 2f)) * 0.30f;
                float tWave = Mathf.Exp(-Mathf.Pow((phase - 0.75f) * 15f, 2f)) * 0.25f;

                float heartbeatZ = (pWave + qWave + rWave + sWave + tWave) * amplitude;
                z = heartbeatZ + Random.Range(-baselineNoise, baselineNoise);
            }

            Vector3 localPosition = new Vector3(x, 0, z);
            positions[i] = planeTransform.TransformPoint(localPosition);
        }

        lineRenderer.positionCount = pointsPerLine;
        lineRenderer.SetPositions(positions);
    }

    public void SetBPM(float newBPM)
    {
        bpm = Mathf.Clamp(newBPM, 30, 300);
    }

    public void SetWaveColor(Color newColor)
    {
        waveColor = newColor;
    }

    public void SetAmplitude(float newAmplitude)
    {
        amplitude = Mathf.Clamp(newAmplitude, 0.1f, 3f);
    }

    public void SetFlatline(bool enable)
    {
        flatline = enable;
    }

    public void ToggleFlatline()
    {
        flatline = !flatline;
    }

    public void SetScrollSpeed(float newSpeed)
    {
        scrollSpeed = Mathf.Clamp(newSpeed, 0.1f, 3f);
    }

    public void SetAutoCycles(bool enable)
    {
        autoCycles = enable;
    }

    public void SetTargetCyclesAt60BPM(int newTarget)
    {
        targetCyclesAt60BPM = Mathf.Max(1, newTarget);
    }

    public void SetCyclesVisible(int newCycles)
    {
        autoCycles = false;
        cyclesVisible = Mathf.Max(1, newCycles);
    }
}