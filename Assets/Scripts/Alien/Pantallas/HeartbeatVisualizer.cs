using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlaneHeartbeatVisualizer : NetworkBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Referencias")]
    [SerializeField] private Transform planeTransform;
    [SerializeField] private TextMeshProUGUI bpmText;

    [Header("Audio FMOD (Bucles 3D)")]
    [SerializeField] private FMODUnity.EventReference normalHeartbeat;
    [SerializeField] private FMODUnity.EventReference fastHeartbeat;
    [SerializeField] private FMODUnity.EventReference flatlineEvent;

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

    private FMOD.Studio.EventInstance normalInstance;
    private FMOD.Studio.EventInstance fastInstance;
    private FMOD.Studio.EventInstance flatlineInstance;

    private enum AudioLoopState { None, Normal, Fast, Flatline }
    private AudioLoopState currentAudioState = AudioLoopState.None;

    public override void OnNetworkSpawn()
    {
        EventBus.Subscribe<OnAlienCalmantUsed>(CalmantApplied);
        EventBus.Subscribe<OnCalmantEnded>(CalmantEnded);
        EventBus.Subscribe<OnAlienStateChanged>(AlienStateChanged);
        EventBus.Subscribe<OnAlienDeath>(AlienDeath);

        waveColor = defaultColor;
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

        InitializeAudioInstances();
    }

    private void InitializeAudioInstances()
    {
        if (!normalHeartbeat.IsNull) normalInstance = FMODUnity.RuntimeManager.CreateInstance(normalHeartbeat);
        if (!fastHeartbeat.IsNull) fastInstance = FMODUnity.RuntimeManager.CreateInstance(fastHeartbeat);
        if (!flatlineEvent.IsNull) flatlineInstance = FMODUnity.RuntimeManager.CreateInstance(flatlineEvent);
        
        UpdateAudioState();
    }

    private void AlienDeath(OnAlienDeath death)
    {
        flatline = true;
        SetBPM(0f);
        UpdateAudioState();
    }

    private void AlienStateChanged(OnAlienStateChanged changed)
    {
        if (flatline) return;
        
        switch (changed.NewState)
        {
            case AlienStateEnum.Calmado:
                SetWaveColor(calmantColor);
                SetBPM(60f); 
                SetScrollSpeed(1f);
                break;
                
            case AlienStateEnum.Desangrado:
                SetWaveColor(desangradoColor);
                SetBPM(120f);
                SetScrollSpeed(4f);
                break;
                
            default:
                SetWaveColor(defaultColor);
                SetBPM(75f);
                SetScrollSpeed(1f);
                break;
        }

        UpdateAudioState();
    }

    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<OnAlienCalmantUsed>(CalmantApplied);
        EventBus.Unsubscribe<OnCalmantEnded>(CalmantEnded);
        EventBus.Unsubscribe<OnAlienStateChanged>(AlienStateChanged);
        EventBus.Unsubscribe<OnAlienDeath>(AlienDeath);
        
        StopAllAudio();
    }

    private void OnDestroy()
    {
        StopAllAudio();
    }

    private void CalmantApplied(OnAlienCalmantUsed used)
    {
        if (waveColor == desangradoColor) return;
        SetWaveColor(calmantColor);
    }

    private void CalmantEnded(OnCalmantEnded ended)
    {
        if (waveColor == desangradoColor) return;
        SetWaveColor(defaultColor);
    }

    void Update()
    {
        if (lineRenderer == null) return;
        
        if (flatline)
        {
            SetBPM(0f);
        }
        
        lineRenderer.startColor = waveColor;
        lineRenderer.endColor = waveColor;
        bpmText.color = waveColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        bpmText.text = bpm.ToString("F0") + "bpm";
        
        GenerateHeartbeatWave();
        UpdateSpatialAudioPositions();
    }
    private void UpdateAudioState()
    {
        AudioLoopState targetState = AudioLoopState.None;

        if (flatline)
        {
            targetState = AudioLoopState.Flatline;
        }
        else if (bpm >= 100f)
        {
            targetState = AudioLoopState.Fast;
        }
        else if (bpm > 0f)
        {
            targetState = AudioLoopState.Normal;
        }

        if (currentAudioState == targetState) return;

        ManageInstancePlayback(currentAudioState, start: false);

        currentAudioState = targetState;

        ManageInstancePlayback(currentAudioState, start: true);
    }

    private void ManageInstancePlayback(AudioLoopState state, bool start)
    {
        FMOD.Studio.EventInstance instance;

        switch (state)
        {
            case AudioLoopState.Normal: instance = normalInstance; break;
            case AudioLoopState.Fast: instance = fastInstance; break;
            case AudioLoopState.Flatline: instance = flatlineInstance; break;
            default: return;
        }

        if (!instance.isValid()) return;

        if (start)
        {
            instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
            instance.start();
        }
        else
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }
    private void UpdateSpatialAudioPositions()
    {
        var attributes = FMODUnity.RuntimeUtils.To3DAttributes(gameObject);
        if (normalInstance.isValid()) normalInstance.set3DAttributes(attributes);
        if (fastInstance.isValid()) fastInstance.set3DAttributes(attributes);
        if (flatlineInstance.isValid()) flatlineInstance.set3DAttributes(attributes);
    }

    private void StopAllAudio()
    {
        if (normalInstance.isValid()) { normalInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); normalInstance.release(); }
        if (fastInstance.isValid()) { fastInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); fastInstance.release(); }
        if (flatlineInstance.isValid()) { flatlineInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); flatlineInstance.release(); }
        currentAudioState = AudioLoopState.None;
    }

    void GenerateHeartbeatWave()
    {
        if (autoCycles)
        {
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

    public void SetBPM(float newBPM) { bpm = newBPM; UpdateAudioState(); }
    public void SetWaveColor(Color newColor) => waveColor = newColor;
    public void SetAmplitude(float newAmplitude) => amplitude = Mathf.Clamp(newAmplitude, 0.1f, 3f);
    public void SetFlatline(bool enable) { flatline = enable; UpdateAudioState(); }
    public void ToggleFlatline() { flatline = !flatline; UpdateAudioState(); }
    public void SetScrollSpeed(float newSpeed) => scrollSpeed = Mathf.Clamp(newSpeed, 0.1f, 3f);
    public void SetAutoCycles(bool enable) => autoCycles = enable;
    public void SetTargetCyclesAt60BPM(int newTarget) => targetCyclesAt60BPM = Mathf.Max(1, newTarget);
    public void SetCyclesVisible(int newCycles) { autoCycles = false; cyclesVisible = Mathf.Max(1, newCycles); }
}