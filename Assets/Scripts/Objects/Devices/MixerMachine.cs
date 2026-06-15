using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class MixerMachine : PoweredDevice, IGuiaEntryProvider
{
    [Serializable]
    public class FormulaConfig
    {
        public string displayName;
        public LiquidType liquidType;
        public Color color;
        public bool isRandom;
        public Vector3Int fixedValue;
    }

    [Header("Settings")]
    [SerializeField] int max_Component = 5;
    [SerializeField] MeshRenderer[] tubes;
    NetworkVariable<int> componentA = new NetworkVariable<int>(1);
    NetworkVariable<int> componentB = new NetworkVariable<int>(1);
    NetworkVariable<int> componentC = new NetworkVariable<int>(1);
    [SerializeField] private MixerIndicator componentAIndicator;
    [SerializeField] private MixerIndicator componentBIndicator;
    [SerializeField] private MixerIndicator componentCIndicator;

    [Header("Formulas")]
    [SerializeField] private List<FormulaConfig> formulaConfigs = new();
    [SerializeField] private GameObject formulaEntryPrefab;
    [SerializeField] private Transform formulaEntrySpawnRoot;
    private NetworkList<Vector3Int> resolvedFormulas;
    private List<FormulaEntryUI> spawnedEntryUIs = new();

    [Header("Liquids")]
    [SerializeField] Color sludgeColor;

    [Header("Liquid Flask")]
    [SerializeField] MixerFlask MixerFlask;
    Coroutine[] tubeLerpCoroutines;
    [Header("Audio & Timings")]
    [SerializeField] private FMODUnity.EventReference mixSound;
    [SerializeField] private float mixProcessDuration = 2.0f;
    private NetworkVariable<bool> isMixing = new NetworkVariable<bool>(false);
    [Header("Particles")]
    [SerializeField] ParticleSystem mixerParticles;

    void Awake()
    {
        resolvedFormulas = new NetworkList<Vector3Int>();
        tubeLerpCoroutines = new Coroutine[tubes.Length];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            resolvedFormulas.Clear();
            Vector3Int[] finalValues = new Vector3Int[formulaConfigs.Count];
            HashSet<Vector3Int> existingValues = new HashSet<Vector3Int>();
            for (int i = 0; i < formulaConfigs.Count; i++)
            {
                if (!formulaConfigs[i].isRandom)
                {
                    finalValues[i] = formulaConfigs[i].fixedValue;
                    existingValues.Add(formulaConfigs[i].fixedValue);
                }
            }
            for (int i = 0; i < formulaConfigs.Count; i++)
            {
                if (formulaConfigs[i].isRandom)
                {
                    Vector3Int randValue = Vector3Int.zero;
                    bool isValid = false;
                    int safetyCounter = 0;

                    while (!isValid && safetyCounter < 100)
                    {
                        safetyCounter++;

                        int x = UnityEngine.Random.Range(1, max_Component + 1);
                        int y = UnityEngine.Random.Range(1, max_Component + 1);
                        int z = UnityEngine.Random.Range(1, max_Component + 1);
                        randValue = new Vector3Int(x, y, z);

                        if (x == y && y == z) continue;
                        if (existingValues.Contains(randValue)) continue;

                        isValid = true;
                    }

                    finalValues[i] = randValue;
                    existingValues.Add(randValue);
                }
            }
            foreach (var value in finalValues)
            {
                resolvedFormulas.Add(value);
            }
        }

        resolvedFormulas.OnListChanged += OnResolvedFormulasChanged;

        componentA.OnValueChanged += (_, val) => OnComponentChanged(0, val);
        componentB.OnValueChanged += (_, val) => OnComponentChanged(1, val);
        componentC.OnValueChanged += (_, val) => OnComponentChanged(2, val);

        if (IsServer)
        {
            componentA.Value = 1;
            componentB.Value = 1;
            componentC.Value = 1;
            isMixing.Value = false;
        }

        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        resolvedFormulas.OnListChanged -= OnResolvedFormulasChanged;
        componentA.OnValueChanged -= (_, val) => OnComponentChanged(0, val);
        componentB.OnValueChanged -= (_, val) => OnComponentChanged(1, val);
        componentC.OnValueChanged -= (_, val) => OnComponentChanged(2, val);
        base.OnNetworkDespawn();
    }

    void OnResolvedFormulasChanged(NetworkListEvent<Vector3Int> e)
    {
        if (e.Index < spawnedEntryUIs.Count && spawnedEntryUIs[e.Index] != null)
            spawnedEntryUIs[e.Index].Setup(e.Value);
    }

    public void RegisterEntries(GuiaBehaviour guia)
    {
        for (int i = 0; i < formulaConfigs.Count; i++)
        {
            var config = formulaConfigs[i];
            var go = Instantiate(formulaEntryPrefab, formulaEntrySpawnRoot);
            go.SetActive(false);

            var entryUI = go.GetComponent<FormulaEntryUI>();
            spawnedEntryUIs.Add(entryUI);

            if (i < resolvedFormulas.Count)
                entryUI.Setup(resolvedFormulas[i]);

            guia.AddEntry(config.displayName, go);
        }

        if (resolvedFormulas.Count < formulaConfigs.Count)
            StartCoroutine(WaitForFormulas());
    }

    IEnumerator WaitForFormulas()
    {
        while (resolvedFormulas.Count < formulaConfigs.Count)
            yield return null;

        for (int i = 0; i < spawnedEntryUIs.Count; i++)
            if (spawnedEntryUIs[i] != null)
                spawnedEntryUIs[i].Setup(resolvedFormulas[i]);
    }

    public override void Powered() => UpdateUI();

    public void IncrementComponent(int index)
    {
        if (!hasPower.Value || isMixing.Value) return;

        switch (index)
        {
            case 0: componentA.Value = componentA.Value >= max_Component ? 1 : componentA.Value + 1; break;
            case 1: componentB.Value = componentB.Value >= max_Component ? 1 : componentB.Value + 1; break;
            case 2: componentC.Value = componentC.Value >= max_Component ? 1 : componentC.Value + 1; break;
        }
    }

    public void ConfirmMix()
    {
        if (!hasPower.Value || isMixing.Value) return;
        Vector3Int current = new Vector3Int(componentA.Value, componentB.Value, componentC.Value);
        LiquidType liquidType = LiquidType.Sludge;
        Color liquidColor = sludgeColor;

        for (int i = 0; i < resolvedFormulas.Count; i++)
        {
            if (current == resolvedFormulas[i])
            {
                liquidType = formulaConfigs[i].liquidType;
                liquidColor = formulaConfigs[i].color;
                break;
            }
        }

        StartCoroutine(MixProcessCoroutine(liquidType, liquidColor));
    }

    private IEnumerator MixProcessCoroutine(LiquidType liquidType, Color liquidColor)
    {
        mixerParticles.Play();
        isMixing.Value = true;
        PlayMixSoundRpc();

        float initialFill = 0.5f;

        MixerFlask?.StartFilling(liquidType, liquidColor, mixProcessDuration, initialFill);

        yield return new WaitForSeconds(mixProcessDuration);

        mixerParticles.Stop();
        componentA.Value = 1;
        componentB.Value = 1;
        componentC.Value = 1;
        isMixing.Value = false;
    }
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    private void PlayMixSoundRpc()
    {
        if (!mixSound.IsNull)
        {
            AudioManager.instance.PlayOneShotAtPosition(mixSound, transform.position);
        }
    }

    void OnComponentChanged(int index, int newValue)
    {
        if (tubeLerpCoroutines[index] != null)
        {
            StopCoroutine(tubeLerpCoroutines[index]);
        }

        float targetFill = (float)newValue / max_Component;
        tubeLerpCoroutines[index] = StartCoroutine(LerpTubeFill(index, targetFill, 0.4f));

        UpdateUI();
    }

    IEnumerator LerpTubeFill(int index, float targetFill, float duration)
    {
        Material mat = tubes[index].sharedMaterial;
        float initialFill = mat.GetFloat("_FillAmount");
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentFill = Mathf.Lerp(initialFill, targetFill, elapsed / duration);
            mat.SetFloat("_FillAmount", currentFill);
            yield return null;
        }

        mat.SetFloat("_FillAmount", targetFill);
        tubeLerpCoroutines[index] = null;
    }

    private void UpdateUI()
    {
        componentAIndicator.UpdateIndicator(componentA.Value);
        componentBIndicator.UpdateIndicator(componentB.Value);
        componentCIndicator.UpdateIndicator(componentC.Value);
    }
}