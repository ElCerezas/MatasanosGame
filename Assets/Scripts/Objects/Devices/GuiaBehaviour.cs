using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GuiaBehaviour : PoweredDevice
{
    [Serializable]
    public class GuiaEntry
    {
        public string nombre;
        public GameObject target;
    }

    [Header("Guia Settings")]
    [SerializeField] private List<GuiaEntry> entries = new();
    [SerializeField] private float autoCloseTime = 10f;
    [SerializeField] private float scrollSpeed = 0.1f;
    [SerializeField] private float entryHeight = 0.1f;

    [Header("UI References")]
    [SerializeField] private GameObject guiaCanvas;
    [SerializeField] private GameObject entryItemPrefab;
    [SerializeField] private Transform listContainer;
    [SerializeField] private GameObject listPanel;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;

    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);
    private NetworkVariable<int> highlightedIndex = new NetworkVariable<int>(0);
    private NetworkVariable<int> activeTargetIndex = new NetworkVariable<int>(-1);

    private List<TextMeshProUGUI> entryTexts = new();
    private List<RectTransform> entryRects = new();
    private Coroutine scrollCoroutine;
    private Coroutine autoCloseCoroutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        isOpen.OnValueChanged += (_, __) => RefreshUI();
        highlightedIndex.OnValueChanged += (_, __) => { RefreshHighlight(); ScrollToEntry(highlightedIndex.Value); };
        activeTargetIndex.OnValueChanged += (_, __) => RefreshUI();

        BuildList();
        RefreshUI();
    }

    public override void Powered()
    {
        if (!hasPower.Value)
            CloseGuia();
    }

    public void OpenGuia()
    {
        if (!IsServer) return;
        if (!hasPower.Value) return;
        if (isOpen.Value) return;

        highlightedIndex.Value = 0;
        activeTargetIndex.Value = -1;
        isOpen.Value = true;

        PublishGuiaEventClientRpc(GuiaAction.Open, 0);

        if (autoCloseTime > 0f)
        {
            if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
        }
    }

    public void NextEntry()
    {
        if (!IsServer) return;
        if (!hasPower.Value) return;

        if (!isOpen.Value && activeTargetIndex.Value == -1) return;

        highlightedIndex.Value = (highlightedIndex.Value + 1) % entries.Count;
        ResetAutoCloseTimer();
    }

    public void PreviousEntry()
    {
        if (!IsServer) return;
        if (!hasPower.Value) return;

        if (!isOpen.Value && activeTargetIndex.Value == -1) return;

        highlightedIndex.Value = (highlightedIndex.Value - 1 + entries.Count) % entries.Count;
        ResetAutoCloseTimer();
    }

    public void SelectEntry()
    {
        if (!IsServer) return;
        if (!hasPower.Value) return;

        activeTargetIndex.Value = highlightedIndex.Value;
        isOpen.Value = false;

        PublishGuiaEventClientRpc(GuiaAction.EntrySelected, highlightedIndex.Value);
        ResetAutoCloseTimer();
    }

    public void CloseActiveEntry()
    {
        if (!IsServer) return;

        activeTargetIndex.Value = -1;
        isOpen.Value = true;
        highlightedIndex.Value = 0;

        PublishGuiaEventClientRpc(GuiaAction.ActiveEntryClosed, 0);

        ResetAutoCloseTimer();
    }

    public void CloseGuia()
    {
        if (!IsServer) return;

        isOpen.Value = false;
        activeTargetIndex.Value = -1;
        PublishGuiaEventClientRpc(GuiaAction.Close, 0);

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
    }

    private void ResetAutoCloseTimer()
    {
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        if (autoCloseTime > 0f)
        {
            autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
        }
    }

    [ClientRpc]
    private void PublishGuiaEventClientRpc(GuiaAction action, int selectedIndex)
    {
        var guiaEvent = new GuiaEvent
        {
            Action = action,
            SelectedIndex = selectedIndex
        };
        EventBus.Publish(guiaEvent);
    }

    private void BuildList()
    {
        if (listContainer == null) return;
        if (entryItemPrefab == null) return;

        foreach (Transform child in listContainer)
            Destroy(child.gameObject);
        entryTexts.Clear();
        entryRects.Clear();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var item = Instantiate(entryItemPrefab, listContainer);
            var rectTransform = item.GetComponent<RectTransform>();
            var tmp = item.GetComponentInChildren<TextMeshProUGUI>();

            if (tmp == null) continue;

            tmp.text = entry.nombre;
            tmp.color = normalColor;
            entryTexts.Add(tmp);
            entryRects.Add(rectTransform);

            rectTransform.anchoredPosition = new Vector2(0, -i * entryHeight);
        }

        var listRectTransform = listContainer.GetComponent<RectTransform>();
        if (listRectTransform != null)
        {
            listRectTransform.anchoredPosition = Vector2.zero;
        }
    }

    private void RefreshUI()
    {
        bool hasActiveTarget = activeTargetIndex.Value != -1;
        bool showList = isOpen.Value || hasActiveTarget;

        guiaCanvas.SetActive(showList);
        listPanel.SetActive(showList);

        foreach (var entry in entries)
            if (entry.target != null)
                entry.target.SetActive(false);

        if (hasActiveTarget && activeTargetIndex.Value < entries.Count)
            if (entries[activeTargetIndex.Value].target != null)
                entries[activeTargetIndex.Value].target.SetActive(true);

        if (isOpen.Value) RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        for (int i = 0; i < entryTexts.Count; i++)
            entryTexts[i].color = i == highlightedIndex.Value ? highlightColor : normalColor;
    }

    private void HandleHighlightChanged()
    {
        RefreshHighlight();
        ScrollToEntry(highlightedIndex.Value);
    }

    private void ScrollToEntry(int index)
    {
        if (index < 0 || index >= entryRects.Count) return;

        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);

        scrollCoroutine = StartCoroutine(ScrollCoroutine(index));
    }

    private IEnumerator ScrollCoroutine(int targetIndex)
    {
        RectTransform containerRect = listContainer.GetComponent<RectTransform>();
        if (containerRect == null) yield break;

        float targetScrollY = targetIndex * entryHeight;

        while (true)
        {
            Vector2 currentPos = containerRect.anchoredPosition;
            Vector2 newPos = new Vector2(currentPos.x, Mathf.Lerp(currentPos.y, targetScrollY, scrollSpeed));
            containerRect.anchoredPosition = newPos;

            if (Mathf.Abs(newPos.y - targetScrollY) < 0.001f)
            {
                containerRect.anchoredPosition = new Vector2(currentPos.x, targetScrollY);
                break;
            }

            yield return null;
        }
    }

    private void HandleTargetChanged(int index)
    {
        foreach (var entry in entries)
            if (entry.target != null)
                entry.target.SetActive(false);

        if (index >= 0 && index < entries.Count)
            if (entries[index].target != null)
                entries[index].target.SetActive(true);
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseTime);
        CloseGuia();
    }
}