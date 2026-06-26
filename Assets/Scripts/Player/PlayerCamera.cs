using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(PlayerInput))]
public class PlayerCamera : NetworkBehaviour
{
    PlayerInput playerInput;
    PauseHandler pauseHandler;
    PlayerController playerController;

    [SerializeField] Transform playerCam;
    [SerializeField] Transform holdPoint;
    [SerializeField] GameObject BodyVisual;
    [SerializeField] GameObject HatVisual;
    [SerializeField] float holdPointDistance = 1.5f;
    [SerializeField] float holdPointHeight = 1.2f;
    [SerializeField] float maxHoldPointAngleFromCamera = 10f;

    [Header("Settings")]
    [SerializeField] float sensitivity = 2f;
    [SerializeField] float maxPitch = 30f;
    [SerializeField] float minPitch = -80f;

    [Header("Cinematic Pan")]
    [SerializeField] float panDuration = 1.5f;
    [SerializeField] float panEaseSpeed = 5f;
    [SerializeField] private float _cameraTransitionDuration;
    [SerializeField] private float _cameraTransitionSpeed;
    [SerializeField] private Vector3 endPos;
    [SerializeField] private Vector3 endRot;

    float pitchRotation = 0f;
    float yawRotation = 0f;
    float currentMinPitch;
    bool isRagdoll = false;
    bool isCinematic = false;
    Coroutine panCoroutine;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        pauseHandler = GetComponent<PauseHandler>();
        playerController = GetComponent<PlayerController>();
        currentMinPitch = minPitch;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            playerCam.GetComponentInChildren<Camera>().enabled = false;
            return;
        }

        sensitivity = SettingsManager.Instance.GetSetting("Mouse Sensitivity");
        SettingsManager.Instance.OnSettingChanged += OnSettingChanged;
        EventBus.Subscribe<VictoryEvent>(OnVictoryEvent);

        if (IsLocalPlayer)
        {
            int hiddenLayer = LayerMask.NameToLayer("LocalPlayerLayer");
            BodyVisual.layer = hiddenLayer;
            HatVisual.layer = hiddenLayer;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yawRotation = transform.eulerAngles.y;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        EventBus.Unsubscribe<VictoryEvent>(OnVictoryEvent);
    }

    private void OnVictoryEvent(VictoryEvent evt)
    {
        if (isCinematic) return;

        AlienStateManager target = FindFirstObjectByType<AlienStateManager>();
        if (target == null) return;

        if (panCoroutine != null)
            StopCoroutine(panCoroutine);

        panCoroutine = StartCoroutine(PanToTarget(target.transform));
    }

    private IEnumerator PanToTarget(Transform target)
    {
        isCinematic = true;
        playerController?.SetCinematic(true);

        float elapsed = 0f;
        float startYaw = yawRotation;
        float startPitch = pitchRotation;

        while (elapsed < panDuration)
        {
            elapsed += Time.deltaTime;

            Vector3 directionToTarget = target.position - playerCam.position;

            /*
            float targetYaw = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Asin(directionToTarget.normalized.y) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, currentMinPitch, maxPitch);

            float yawDelta = Mathf.DeltaAngle(yawRotation, targetYaw);

            float t = Mathf.SmoothStep(0f, 1f, elapsed / panDuration);

            yawRotation = Mathf.LerpAngle(startYaw, startYaw + yawDelta, elapsed);
            pitchRotation = Mathf.Lerp(startPitch, targetPitch, elapsed);
            */

            playerCam.transform.position = Vector3.Lerp(playerCam.transform.position, endPos, _cameraTransitionSpeed * Time.deltaTime);
            playerCam.transform.rotation = Quaternion.Lerp(playerCam.transform.rotation, Quaternion.Euler(endRot), _cameraTransitionSpeed * Time.deltaTime);
            
            yield return null;
        }

        Vector3 finalDir = target.position - playerCam.position;
        yawRotation = Mathf.Atan2(finalDir.x, finalDir.z) * Mathf.Rad2Deg;
        pitchRotation = Mathf.Clamp(-Mathf.Asin(finalDir.normalized.y) * Mathf.Rad2Deg, currentMinPitch, maxPitch);
    }

    public void EndCinematic()
    {
        if (panCoroutine != null)
        {
            StopCoroutine(panCoroutine);
            panCoroutine = null;
        }
        //isCinematic = false;
        //playerController?.SetCinematic(false);
    }

    public void Ragdoll(bool active) => isRagdoll = active;

    public void SetHoldingTool(bool holding)
    {
        pitchRotation = Mathf.Clamp(pitchRotation, currentMinPitch, maxPitch);
    }

    void Update()
    {
        if (!IsOwner) return;
        if (isCinematic) return;
        if (pauseHandler.IsPaused) return;

        Vector2 lookInput = playerInput.LookInput * sensitivity;
        yawRotation += lookInput.x;
        pitchRotation -= lookInput.y;
        pitchRotation = Mathf.Clamp(pitchRotation, currentMinPitch, maxPitch);
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        if (isCinematic) return;

        playerCam.localRotation = Quaternion.Euler(pitchRotation, 0f, 0f);

        if (!isRagdoll)
            transform.rotation = Quaternion.Euler(0f, yawRotation, 0f);

        float clampedPitch = Mathf.Min(pitchRotation, maxHoldPointAngleFromCamera);
        Vector3 holdDirection = Quaternion.Euler(clampedPitch, yawRotation, 0f) * Vector3.forward;
        holdPoint.position = transform.position + Vector3.up * holdPointHeight + holdDirection * holdPointDistance;
        holdPoint.rotation = Quaternion.Euler(clampedPitch, yawRotation, 0f);
    }

    private void OnSettingChanged(string key, float value)
    {
        if (key == "Mouse Sensitivity")
            sensitivity = value;
    }

}