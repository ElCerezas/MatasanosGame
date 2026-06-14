using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(PlayerInput))]
public class PlayerCamera : NetworkBehaviour
{
    PlayerInput playerInput;
    PauseHandler pauseHandler;
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
    float pitchRotation = 0f;
    float yawRotation = 0f;
    float currentMinPitch;

    bool isRagdoll = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        pauseHandler = GetComponent<PauseHandler>();
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
    public void Ragdoll(bool active)
    {
        isRagdoll = active;
    }
    public void SetHoldingTool(bool holding)
    {
        pitchRotation = Mathf.Clamp(pitchRotation, currentMinPitch, maxPitch);
    }

    void Update()
    {
        if (!IsOwner) return;
        if (pauseHandler.IsPaused) return;
        Vector2 lookInput = playerInput.LookInput * sensitivity;
        yawRotation += lookInput.x;
        pitchRotation -= lookInput.y;
        pitchRotation = Mathf.Clamp(pitchRotation, currentMinPitch, maxPitch);
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

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