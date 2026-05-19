using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SplashSettings
{
    public RectTransform container;
    public Sprite[] sprites;
    public Vector2 sizeRange = new Vector2(50f, 150f);
    public Gradient colorGradient;
}

public class PlayerHUDEffects : NetworkBehaviour
{
    [Header("Setup Crítico Multijugador")]
    [SerializeField] private GameObject localCanvasObject; // Arrastra AQUÍ el GameObject del Canvas

    [Header("Sleep")]
    [SerializeField] CanvasGroup sleepOverlay;
    [SerializeField] float sleepFadeDuration = 1f;

    [Header("Flashed")]
    [SerializeField] CanvasGroup flashedOverlay;
    [SerializeField] float flashedFadeOutDuration = 2f;

    [Header("Parasite")]
    [SerializeField] RawImage parasiteNoiseImage;
    [SerializeField] float parasiteGrowthRate = 0.05f;
    [SerializeField] bool parasiteIsPersistent = true;
    [SerializeField] float maxParasiteIntensity = 1f;
    float parasiteIntensity = 0f;
    bool parasiteActive = false;

    [Header("Booger / Moco")]
    [SerializeField] SplashSettings mocoSettings;
    private List<GameObject> activeMocos = new List<GameObject>();

    [Header("Blood / Sangre")]
    [SerializeField] SplashSettings bloodSettings;
    private List<GameObject> activeBloods = new List<GameObject>();

    Coroutine sleepCoroutine;
    Coroutine flashedCoroutine;

    public override void OnNetworkSpawn()
    {
        // SI NO SOMOS EL DUEÑO: Apagamos SU canvas para que no tape nuestra pantalla
        if (!IsOwner)
        {
            if (localCanvasObject != null)
                localCanvasObject.SetActive(false);

            enabled = false;
            return;
        }

        // Si somos el dueño, aseguramos que nuestro Canvas esté encendido
        if (localCanvasObject != null)
            localCanvasObject.SetActive(true);

        EventBus.Subscribe<OnInject>(OnPlayerInjected);
        EventBus.Subscribe<OnPlayerSlipped>(OnPlayerSlipped);
        EventBus.Subscribe<OnInsectExplosion>(OnInsectExplosion);
        EventBus.Subscribe<OnPlayerSneezes>(OnPlayerSneezes);
        EventBus.Subscribe<OnPlayerBlinded>(OnPlayerBlinded);
        EventBus.Subscribe<OnHUDCleaned>(OnHUDCleaned);
        ResetAllEffects();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        EventBus.Unsubscribe<OnInject>(OnPlayerInjected);
        EventBus.Unsubscribe<OnInsectExplosion>(OnInsectExplosion);
        EventBus.Unsubscribe<OnPlayerSlipped>(OnPlayerSlipped);
        EventBus.Unsubscribe<OnPlayerSneezes>(OnPlayerSneezes);
        EventBus.Unsubscribe<OnPlayerBlinded>(OnPlayerBlinded);
        EventBus.Unsubscribe<OnHUDCleaned>(OnHUDCleaned);

        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!parasiteActive) return;

        parasiteIntensity = Mathf.Clamp(parasiteIntensity + parasiteGrowthRate * Time.deltaTime, 0f, maxParasiteIntensity);
        ApplyParasiteVisual(parasiteIntensity);

        if (!parasiteIsPersistent && parasiteIntensity >= maxParasiteIntensity)
            CleanParasite();
    }

    #region EventReceivers
    void OnPlayerInjected(OnInject data) { if (data.VictimID == NetworkObjectId && data.Type == InyeccionType.Calmante) TriggerSleep(5f); }
    void OnPlayerSlipped(OnPlayerSlipped data) { if (data.VictimID == NetworkObjectId) TriggerBlood(); }
    void OnInsectExplosion(OnInsectExplosion data) { if (data.VictimID == NetworkObjectId) TriggerParasite(); }
    void OnPlayerSneezes(OnPlayerSneezes data) { if (data.VictimID == NetworkObjectId) TriggerMoco(); }
    void OnPlayerBlinded(OnPlayerBlinded data) { if (data.VictimID == NetworkObjectId) TriggerFlash(data.Duration); }
    void OnHUDCleaned(OnHUDCleaned data)
    {
        if (data.VictimID != NetworkObjectId) return;
        if (data.CleanMoco) CleanMoco();
        if (data.CleanSangre) CleanBlood();
        if (data.CleanParasito) CleanParasite();
    }
    #endregion

    #region Effects Logic

    public void TriggerSleep(float duration)
    {
        if (sleepCoroutine != null) StopCoroutine(sleepCoroutine);
        sleepCoroutine = StartCoroutine(SleepRoutine(duration));
    }
    IEnumerator SleepRoutine(float duration)
    {
        yield return FadeCanvasGroup(sleepOverlay, 0f, 0.85f, sleepFadeDuration * 0.3f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float blink = Mathf.PingPong(elapsed * 0.8f, 1f);
            sleepOverlay.alpha = Mathf.Lerp(0.5f, 0.95f, blink);
            elapsed += Time.deltaTime;
            yield return null;
        }
        yield return FadeCanvasGroup(sleepOverlay, sleepOverlay.alpha, 0f, sleepFadeDuration);
        sleepCoroutine = null;
    }

    public void TriggerFlash(float duration)
    {
        if (flashedCoroutine != null) StopCoroutine(flashedCoroutine);
        flashedCoroutine = StartCoroutine(FlashRoutine(duration));
    }
    IEnumerator FlashRoutine(float duration)
    {
        float targetAlpha = Mathf.Clamp01(duration / 10f);
        yield return FadeCanvasGroup(flashedOverlay, 0f, targetAlpha, 0.1f);
        yield return new WaitForSeconds(duration * 0.5f);
        yield return FadeCanvasGroup(flashedOverlay, targetAlpha, 0f, flashedFadeOutDuration + duration * 0.5f);
        flashedCoroutine = null;
    }

    public void TriggerParasite()
    {
        if (parasiteNoiseImage == null) return;
        parasiteActive = true;
        parasiteNoiseImage.enabled = true;
    }

    void ApplyParasiteVisual(float intensity)
    {
        if (parasiteNoiseImage == null) return;

        float noise = Mathf.PerlinNoise(Time.time * 3f, 0f);
        float scaleWobble = Mathf.Lerp(0.95f, 1.05f, noise);

        parasiteNoiseImage.rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.1f, Vector3.one * 1.5f, intensity) * scaleWobble;

        Color c = parasiteNoiseImage.color;
        c.a = Mathf.Lerp(0f, 1f, intensity);
        parasiteNoiseImage.color = c;

        float offset = Time.time * 0.05f;
        parasiteNoiseImage.uvRect = new Rect(offset, offset, 1f, 1f);
    }

    public void CleanParasite()
    {
        parasiteActive = false;
        parasiteIntensity = 0f;
        if (parasiteNoiseImage != null)
            parasiteNoiseImage.enabled = false;
    }

    void CreateSplash(SplashSettings settings, List<GameObject> activeList)
    {
        if (settings.container == null || settings.sprites == null || settings.sprites.Length == 0) return;

        GameObject splashObj = new GameObject("Splash_Instance");
        splashObj.transform.SetParent(settings.container, false);

        Image img = splashObj.AddComponent<Image>();
        img.sprite = settings.sprites[Random.Range(0, settings.sprites.Length)];
        img.color = settings.colorGradient.Evaluate(Random.value);
        img.raycastTarget = false;

        RectTransform rect = img.rectTransform;
        float size = Random.Range(settings.sizeRange.x, settings.sizeRange.y);
        rect.sizeDelta = new Vector2(size, size);
        rect.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        float x = Random.Range(settings.container.rect.xMin, settings.container.rect.xMax);
        float y = Random.Range(settings.container.rect.yMin, settings.container.rect.yMax);
        rect.anchoredPosition = new Vector2(x, y);

        activeList.Add(splashObj);
    }

    public void TriggerMoco() => CreateSplash(mocoSettings, activeMocos);
    public void CleanMoco() { foreach (var m in activeMocos) if (m != null) Destroy(m); activeMocos.Clear(); }

    public void TriggerBlood() => CreateSplash(bloodSettings, activeBloods);
    public void CleanBlood() { foreach (var b in activeBloods) if (b != null) Destroy(b); activeBloods.Clear(); }

    #endregion

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    public void ResetAllEffects()
    {
        if (sleepOverlay != null) sleepOverlay.alpha = 0f;
        if (flashedOverlay != null) flashedOverlay.alpha = 0f;
        CleanMoco();
        CleanBlood();
        CleanParasite();
    }
}

// --- INSPECTOR PERSONALIZADO PARA TESTEO ---
#if UNITY_EDITOR
[CustomEditor(typeof(PlayerHUDEffects))]
public class PlayerHUDEffectsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Dibuja las variables del script de forma normal

        PlayerHUDEffects script = (PlayerHUDEffects)target;

        GUILayout.Space(15);
        GUILayout.Label("BOTONES DE TESTEO (Solo en Play Mode)", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Entra en Play Mode para usar los botones de testeo.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("🩸 Añadir Sangre", GUILayout.Height(30))) script.TriggerBlood();
        if (GUILayout.Button("🤢 Añadir Moco", GUILayout.Height(30))) script.TriggerMoco();
        if (GUILayout.Button("👾 Activar Parásito", GUILayout.Height(30))) script.TriggerParasite();

        GUILayout.Space(10);

        if (GUILayout.Button("Limpiar Sangre")) script.CleanBlood();
        if (GUILayout.Button("Limpiar Moco")) script.CleanMoco();
        if (GUILayout.Button("Limpiar Parásito")) script.CleanParasite();

        GUILayout.Space(10);

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("❌ RESETEAR TODO", GUILayout.Height(35))) script.ResetAllEffects();
    }
}
#endif