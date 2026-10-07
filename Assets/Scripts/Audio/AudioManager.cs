using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.SceneManagement;
using System;

public class AudioManager : MonoBehaviour
{
    [Header("Volume")]
    [Range(0, 1)] public float masterVolume = 1;
    [Range(0, 1)] public float musicVolume = 1;
    [Range(0, 1)] public float ambienceVolume = 1;
    [Range(0, 1)] public float SFXVolume = 1;

    [Header("Bus Paths (deben coincidir con FMOD Studio)")]
    [SerializeField] private string musicBusPath = "bus:/Music";
    [SerializeField] private string sfxBusPath = "bus:/SFX";
    [SerializeField] private string ambienceBusPath = "bus:/Ambience";

    [Header("Music Events")]
    public EventReference menuMusic;
    public EventReference gameplayMusic;
    public EventReference waitingRoomMusic;


    [Header("Ambiences")]
    public EventReference defaultAmbience;

    [Header("SFX")]
    public EventReference Enchufe;


    [Header("Cinematicas")]


    private Bus masterBus;
    private Bus musicBus;
    private Bus ambienceBus;
    private Bus sfxBus;

    //MUSIC
    private EventInstance currentMusic;
    private EventInstance nextMusic;
    private bool isTransitioning = false;

    //AMBIENCE
    private EventInstance currentAmbience;
    private EventInstance nextAmbience;
    private bool isAmbienceTransitioning = false;

    private Dictionary<string, EventInstance> cinematicInstances = new Dictionary<string, EventInstance>();
    private Coroutine musicParameterCoroutine;
    private float currentModesValue = 0f;

    public static AudioManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingChanged -= OnSettingChanged;
    }

    private void Start()
    {
        RuntimeManager.LoadBank("Master");
        RuntimeManager.LoadBank("Master.strings");
        RuntimeManager.LoadBank("Music");

        masterVolume = SettingsManager.Instance.GetSetting("Master Volume");
        SFXVolume = SettingsManager.Instance.GetSetting("SFX Volume");
        musicVolume = SettingsManager.Instance.GetSetting("Music Volume");
        ambienceVolume = SettingsManager.Instance.GetSetting("Ambience Volume");
        SettingsManager.Instance.OnSettingChanged += OnSettingChanged;

        StartCoroutine(InitAudioWhenReady());
    }

    private IEnumerator InitAudioWhenReady()
    {
        while (!RuntimeManager.HaveAllBanksLoaded)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // El bus master ("bus:/") siempre existe
        masterBus = RuntimeManager.GetBus("bus:/");

        // Los demás solo se piden si existen de verdad en los banks cargados.
        // Si no existen, se quedan inválidos y Update() los ignora.
        TryGetBus(musicBusPath, out musicBus);
        TryGetBus(sfxBusPath, out sfxBus);
        TryGetBus(ambienceBusPath, out ambienceBus);

        currentMusic = RuntimeManager.CreateInstance(menuMusic);
        currentMusic.start();
        currentMusic.setVolume(musicVolume);
    }

    // Comprueba si el bus existe ANTES de pedirlo, así FMOD no escribe el error en consola
    private bool TryGetBus(string path, out Bus bus)
    {
        bus = default;

        if (string.IsNullOrEmpty(path)) return false;

        if (!BusExists(path))
        {
            Debug.LogWarning($"[AudioManager] El bus '{path}' no existe en los banks cargados. " +
                             "Revisa el nombre en FMOD Studio (Mixer) o el campo del Inspector.");
            return false;
        }

        return RuntimeManager.StudioSystem.getBus(path, out bus) == FMOD.RESULT.OK;
    }

    private bool BusExists(string path)
    {
        var system = RuntimeManager.StudioSystem;

        if (system.getBankList(out Bank[] banks) != FMOD.RESULT.OK || banks == null)
            return false;

        foreach (var bank in banks)
        {
            if (bank.getBusList(out Bus[] buses) != FMOD.RESULT.OK || buses == null)
                continue;

            foreach (var b in buses)
            {
                if (b.getPath(out string busPath) == FMOD.RESULT.OK &&
                    string.Equals(busPath, path, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void Update()
    {
        if (!masterBus.isValid()) return;

        masterBus.setVolume(masterVolume);

        if (musicBus.isValid()) musicBus.setVolume(musicVolume);
        if (sfxBus.isValid()) sfxBus.setVolume(SFXVolume);
        if (ambienceBus.isValid()) ambienceBus.setVolume(ambienceVolume);
    }

    private void OnSettingChanged(string key, float value)
    {
        switch (key)
        {
            case "Master Volume":
                masterVolume = value;
                break;
            case "Music Volume":
                musicVolume = value;
                break;
            case "Ambience Volume":
                ambienceVolume = value;
                break;
            case "SFX Volume":
                SFXVolume = value;
                break;
        }
    }

    #region Musica

    public void PlayMusic(EventReference musicEvent)
    {
        if (!musicEvent.IsNull)
        {
            if (currentMusic.isValid())
            {
                currentMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                currentMusic.release();
            }

            currentMusic = RuntimeManager.CreateInstance(musicEvent);
            currentMusic.start();
            currentMusic.setVolume(musicVolume);
        }
        else
        {
            Debug.LogWarning("Intento de reproducir música con EventReference nula");
        }
    }

    public void PlayMusicImmediate(EventReference musicEvent)
    {
        if (!musicEvent.IsNull)
        {
            if (currentMusic.isValid())
            {
                currentMusic.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                currentMusic.release();
            }

            currentMusic = RuntimeManager.CreateInstance(musicEvent);
            currentMusic.start();
            currentMusic.setVolume(musicVolume);
        }
    }

    public void ReturnToGameplayImmediate()
    {
        PlayMusicImmediate(gameplayMusic);
    }

    public void ReturnToMenuImmediate()
    {
        PlayMusicImmediate(menuMusic);
    }

    public void ChangeMusicWithFade(EventReference newMusic, float fadeTime = 2f)
    {
        if (newMusic.IsNull || isTransitioning) return;
        StartCoroutine(SmoothCrossfadeMusic(newMusic, fadeTime));
    }

    private IEnumerator SmoothCrossfadeMusic(EventReference newMusic, float fadeTime)
    {
        if (newMusic.IsNull) yield break;

        if (IsPlayingMusic(newMusic))
        {
            if (currentMusic.isValid())
            {
                currentMusic.setVolume(musicVolume);
            }
            yield break;
        }

        isTransitioning = true;

        float startVolume = musicVolume;

        nextMusic = RuntimeManager.CreateInstance(newMusic);
        nextMusic.start();
        nextMusic.setVolume(0f);

        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeTime);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float exponentialT = 1f - Mathf.Pow(1f - t, 3f);
            float finalT = (smoothT + exponentialT) * 0.5f;

            if (currentMusic.isValid())
            {
                float oldVolume = Mathf.Lerp(startVolume, 0f, finalT);
                currentMusic.setVolume(oldVolume);
            }

            float newVolume = Mathf.Lerp(0f, startVolume, finalT);
            nextMusic.setVolume(newVolume);

            yield return null;
        }

        if (currentMusic.isValid())
        {
            currentMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentMusic.release();
        }

        currentMusic = nextMusic;
        nextMusic.clearHandle();
        isTransitioning = false;
    }

    public void SetMusicParameterSmooth(string parameterName, float targetValue, float duration = 1.5f)
    {
        if (!currentMusic.isValid()) return;

        if (musicParameterCoroutine != null)
            StopCoroutine(musicParameterCoroutine);

        musicParameterCoroutine = StartCoroutine(
            FadeMusicParameter(parameterName, targetValue, duration)
        );
    }

    private IEnumerator FadeMusicParameter(string parameterName, float targetValue, float duration)
    {
        if (!currentMusic.isValid()) yield break;

        float startValue = currentModesValue;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            float smoothT = t * t * (3f - 2f * t);
            float elasticT = Mathf.Sin(t * Mathf.PI * 0.5f);
            float value = Mathf.Lerp(startValue, targetValue, (smoothT + elasticT) * 0.5f);

            currentMusic.setParameterByName(parameterName, value);

            currentModesValue = value;
            yield return null;
        }

        currentModesValue = targetValue;
        currentMusic.setParameterByName(parameterName, targetValue);
    }

    #endregion



    public void PlayOneShot(EventReference sound)
    {
        if (!sound.IsNull)
            RuntimeManager.PlayOneShot(sound);
    }
    public void PlayOneShotAtPosition(EventReference sound, Vector3 position)
    {
        if (!sound.IsNull)
            RuntimeManager.PlayOneShot(sound, position);
    }



    #region Cambios de Escena

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;

        if (scene.name == "MainMenu" || scene.name == "WaitingRoom" || scene.name == "MapaGold")
        {
            ApplyShopLowcut(true);
        }
        else
        {
            ApplyShopLowcut(false);
        }

        if ((scene.name == "MainMenu") && IsPlayingMusic(GetMusicForScene(scene.name)))
        {
            return;
        }

        EventReference newMusic = GetMusicForScene(scene.name);

        if (!newMusic.IsNull && !IsPlayingMusic(newMusic) && !isTransitioning)
        {
            StartCoroutine(ChangeMusicWhenReady(newMusic));
        }
    }

    private IEnumerator ChangeMusicWhenReady(EventReference newMusic)
    {
        while (!RuntimeManager.HaveAllBanksLoaded)
        {
            yield return null;
        }
        while (!masterBus.isValid())
        {
            yield return null;
        }
        StartCoroutine(SmoothCrossfadeMusic(newMusic, 2f));
    }

    private EventReference GetMusicForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "MainMenu":
                return menuMusic;

            case "MapaGold":
                return gameplayMusic;

            case "WaitingRoom":
                return waitingRoomMusic;

            default:
                return default;
        }
    }

    private void ApplyShopLowcut(bool enable)
    {
        if (!currentMusic.isValid()) return;

        float target = enable ? 1f : 0f;
        float duration = enable ? 1.2f : 2.5f;

        SetMusicParameterSmooth("Modes", target, duration);
    }

    private bool IsPlayingMusic(EventReference musicEvent)
    {
        if (!currentMusic.isValid() || musicEvent.IsNull) return false;

        if (isTransitioning) return false;

        try
        {
            EventDescription currentDesc;
            currentMusic.getDescription(out currentDesc);

            string currentPath;
            currentDesc.getPath(out currentPath);

            EventInstance tempInstance = RuntimeManager.CreateInstance(musicEvent);
            EventDescription targetDesc;
            tempInstance.getDescription(out targetDesc);

            string targetPath;
            targetDesc.getPath(out targetPath);

            tempInstance.release();

            return !string.IsNullOrEmpty(currentPath) &&
                   !string.IsNullOrEmpty(targetPath) &&
                   currentPath == targetPath;
        }
        catch
        {
            PLAYBACK_STATE playbackState;
            currentMusic.getPlaybackState(out playbackState);
            return playbackState == PLAYBACK_STATE.PLAYING && !isTransitioning;
        }
    }

    #endregion

    #region Ambientes
    public void PlayAmbience(EventReference ambienceEvent, bool fadeIn = true, float fadeTime = 2f)
    {
        if (ambienceEvent.IsNull) return;

        if (IsPlayingAmbience(ambienceEvent) && !isAmbienceTransitioning)
            return;

        if (fadeIn && currentAmbience.isValid())
        {
            CrossfadeAmbience(ambienceEvent, fadeTime);
        }
        else
        {
            if (currentAmbience.isValid())
            {
                currentAmbience.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                currentAmbience.release();
            }
            currentAmbience = RuntimeManager.CreateInstance(ambienceEvent);
            currentAmbience.start();
            currentAmbience.setVolume(ambienceVolume);
        }
    }

    public void StopAmbience(bool fadeOut = true, float fadeTime = 2f)
    {
        if (!currentAmbience.isValid()) return;

        if (fadeOut)
        {
            StartCoroutine(FadeOutAmbience(fadeTime));
        }
        else
        {
            currentAmbience.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentAmbience.release();
        }
    }


    public void CrossfadeAmbience(EventReference newAmbience, float fadeTime = 2f)
    {
        if (newAmbience.IsNull || isAmbienceTransitioning) return;
        StartCoroutine(SmoothCrossfadeAmbience(newAmbience, fadeTime));
    }


    public void SetAmbienceParameter(string parameterName, float value)
    {
        if (currentAmbience.isValid())
            currentAmbience.setParameterByName(parameterName, value);
    }


    private IEnumerator SmoothCrossfadeAmbience(EventReference newAmbience, float fadeTime)
    {
        isAmbienceTransitioning = true;

        float startVolume = ambienceVolume;

        nextAmbience = RuntimeManager.CreateInstance(newAmbience);
        nextAmbience.start();
        nextAmbience.setVolume(0f);

        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeTime);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (currentAmbience.isValid())
                currentAmbience.setVolume(Mathf.Lerp(startVolume, 0f, smoothT));

            nextAmbience.setVolume(Mathf.Lerp(0f, startVolume, smoothT));
            yield return null;
        }

        if (currentAmbience.isValid())
        {
            currentAmbience.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentAmbience.release();
        }

        currentAmbience = nextAmbience;
        nextAmbience.clearHandle();
        isAmbienceTransitioning = false;
    }

    private IEnumerator FadeOutAmbience(float fadeTime)
    {
        if (!currentAmbience.isValid()) yield break;

        float startVolume = ambienceVolume;
        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeTime);
            currentAmbience.setVolume(Mathf.Lerp(startVolume, 0f, t));
            yield return null;
        }
        currentAmbience.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        currentAmbience.release();
    }

    private bool IsPlayingAmbience(EventReference ambienceEvent)
    {
        if (!currentAmbience.isValid() || ambienceEvent.IsNull) return false;
        if (isAmbienceTransitioning) return false;

        try
        {
            currentAmbience.getDescription(out EventDescription currentDesc);
            currentDesc.getPath(out string currentPath);

            EventInstance temp = RuntimeManager.CreateInstance(ambienceEvent);
            temp.getDescription(out EventDescription targetDesc);
            targetDesc.getPath(out string targetPath);
            temp.release();

            return currentPath == targetPath;
        }
        catch
        {
            currentAmbience.getPlaybackState(out PLAYBACK_STATE state);
            return state == PLAYBACK_STATE.PLAYING && !isAmbienceTransitioning;
        }
    }

    #endregion
}