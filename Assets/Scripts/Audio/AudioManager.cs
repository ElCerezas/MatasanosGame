using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("Volume")]
    [Range(0, 1)] public float masterVolume = 1;
    [Range(0, 1)] public float musicVolume = 1;
    [Range(0, 1)] public float ambienceVolume = 1;
    [Range(0, 1)] public float SFXVolume = 1;

    [Header("Music Events")]
    public EventReference menuMusic;
    public EventReference gameplayMusic;


    [Header("SFX")]
    public EventReference Enchufe;
 

    [Header("Cinematicas")]


    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;

    private EventInstance currentMusic;
    private EventInstance nextMusic;
    private bool isTransitioning = false;

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

    private void Start()
    {
        RuntimeManager.LoadBank("Master");
        RuntimeManager.LoadBank("Master.strings");
        RuntimeManager.LoadBank("Music");

        StartCoroutine(InitAudioWhenReady());
    }

    private IEnumerator InitAudioWhenReady()
    {
        while (!RuntimeManager.HaveAllBanksLoaded)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        masterBus = RuntimeManager.GetBus("bus:/");

        try { musicBus = RuntimeManager.GetBus("bus:/Music"); }
        catch { musicBus = masterBus; }

        try { sfxBus = RuntimeManager.GetBus("bus:/SFX"); }
        catch { }

        currentMusic = RuntimeManager.CreateInstance(menuMusic);
        currentMusic.start();
        currentMusic.setVolume(musicVolume);
    }

    private void Update()
    {
        if (!masterBus.isValid()) return;
        masterBus.setVolume(masterVolume);
        musicBus.setVolume(musicVolume);
        sfxBus.setVolume(SFXVolume);
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

    #region SFX

    public void PlayOneShot(EventReference sound)
    {
        if (!sound.IsNull)
            RuntimeManager.PlayOneShot(sound);
    }

    #endregion

    #region Cambios de Escena

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;

        if (scene.name == "MainMenu" || scene.name == "MapaBeta")
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
}