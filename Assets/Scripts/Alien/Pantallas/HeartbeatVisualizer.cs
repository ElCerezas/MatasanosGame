using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlaneHeartbeatVisualizer : NetworkBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Referencias")]
    [SerializeField] private Transform planeTransform;
    [SerializeField] private TextMeshProUGUI bpmText;

    [Header("Audio (Sincronizado)")]
    [SerializeField] FMODUnity.EventReference normalHeartbeat;
    [SerializeField] FMODUnity.EventReference fastHeartbeat;
    [SerializeField] GameObject flatlineObject;

    [Header("Parámetros de la onda")]
    [SerializeField] private float bpm = 75f;
    [Range(0.1f, 3f)]
    [SerializeField] private float amplitude = 1f;
    [SerializeField] private bool flatline = false;

    [Header("Visualización")]
    private Color waveColor = Color.red;
    [Range(0.05f, 0.5f)]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private int pointsPerLine = 500;

    [Header("Pantalla")]
    [SerializeField] private float planeWidth = 10f;
    [SerializeField] private bool autoCycles = true;
    [SerializeField] private int targetCyclesAt60BPM = 4;
    [SerializeField] private float scrollSpeed = 1f;
    [SerializeField] private float baselineNoise = 0.01f;

    [Header("Colores")]
    [SerializeField] private Color calmantColor = Color.blue;
    [SerializeField] private Color desangradoColor = Color.red;
    [SerializeField] private Color defaultColor = Color.red;

    private int cyclesVisible = 2;
    private bool hasPlayedDeathSound = false;

    public override void OnNetworkSpawn()
    {
        EventBus.Subscribe<OnAlienCalmantUsed>(CalmantApplied);
        EventBus.Subscribe<OnCalmantEnded>(CalmantEnded);
        EventBus.Subscribe<OnAlienStateChanged>(AlienStateChanged);
        EventBus.Subscribe<OnAlienDeath>(AlienDeath);

        waveColor = defaultColor;
        lineRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        if (planeTransform == null) planeTransform = transform;
    }

    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<OnAlienCalmantUsed>(CalmantApplied);
        EventBus.Unsubscribe<OnCalmantEnded>(CalmantEnded);
        EventBus.Unsubscribe<OnAlienStateChanged>(AlienStateChanged);
        EventBus.Unsubscribe<OnAlienDeath>(AlienDeath);
    }

    #region Red y Audio

    [ServerRpc]
    private void PlaySoundServerRpc(int soundType)
    {
        PlaySoundClientRpc(soundType);
    }

    [ClientRpc]
    private void PlaySoundClientRpc(int soundType)
    {
        if (flatline)
        {
            if (flatlineObject.activeSelf == false) flatlineObject.SetActive(true);
            return;
        }
        FMODUnity.EventReference targetEvent = soundType switch
        {
            1 => fastHeartbeat,
            _ => normalHeartbeat
        };

        AudioManager.instance.PlayOneShotAtPosition(targetEvent, transform.position);
    }
    #endregion

    private void AlienStateChanged(OnAlienStateChanged changed)
    {
        if (flatline) return;

        switch (changed.NewState)
        {
            case AlienStateEnum.Calmado:
                SetWaveColor(calmantColor);
                SetBPM(60f);
                PlaySoundServerRpc(0);
                break;
            case AlienStateEnum.Desangrado:
                SetWaveColor(desangradoColor);
                SetBPM(120f);
                PlaySoundServerRpc(1);
                break;
            default:
                SetWaveColor(defaultColor);
                SetBPM(75f);
                PlaySoundServerRpc(0);
                break;
        }
    }

    private void AlienDeath(OnAlienDeath death)
    {
        flatline = true;
        SetBPM(0f);
        PlaySoundServerRpc(2);
    }

    private void CalmantApplied(OnAlienCalmantUsed used) { if (waveColor != desangradoColor) SetWaveColor(calmantColor); }
    private void CalmantEnded(OnCalmantEnded ended) { if (waveColor != desangradoColor) SetWaveColor(defaultColor); }

    void Update()
    {
        if (lineRenderer == null) return;

        bpmText.text = flatline ? "0 bpm" : bpm.ToString("F0") + " bpm";
        bpmText.color = waveColor;
        lineRenderer.startColor = waveColor;
        lineRenderer.endColor = waveColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        GenerateHeartbeatWave();
    }

    void GenerateHeartbeatWave()
    {
        if (autoCycles) cyclesVisible = Mathf.Max(1, Mathf.RoundToInt((bpm / 60f) * targetCyclesAt60BPM));

        Vector3[] positions = new Vector3[pointsPerLine];
        for (int i = 0; i < pointsPerLine; i++)
        {
            float normalizedX = (float)i / (pointsPerLine - 1);
            float x = Mathf.Lerp(-planeWidth / 2f, planeWidth / 2f, normalizedX);

            float z = flatline ? 0 : CalculateHeartbeatZ(normalizedX);

            positions[i] = planeTransform.TransformPoint(new Vector3(x, 0, z));
        }
        lineRenderer.positionCount = pointsPerLine;
        lineRenderer.SetPositions(positions);
    }

    private float CalculateHeartbeatZ(float normalizedX)
    {
        float phase = Mathf.Repeat((normalizedX * cyclesVisible) + (Time.time * scrollSpeed), 1.0f);
        float p = Mathf.Exp(-Mathf.Pow((phase - 0.25f) * 25f, 2f)) * 0.15f;
        float q = -Mathf.Exp(-Mathf.Pow((phase - 0.45f) * 60f, 2f)) * 0.20f;
        float r = Mathf.Exp(-Mathf.Pow((phase - 0.50f) * 40f, 2f)) * 1.00f;
        float s = -Mathf.Exp(-Mathf.Pow((phase - 0.55f) * 60f, 2f)) * 0.30f;
        float t = Mathf.Exp(-Mathf.Pow((phase - 0.75f) * 15f, 2f)) * 0.25f;
        return (p + q + r + s + t) * amplitude + Random.Range(-baselineNoise, baselineNoise);
    }

    public void SetBPM(float newBPM) => bpm = newBPM;
    public void SetWaveColor(Color newColor) => waveColor = newColor;
}