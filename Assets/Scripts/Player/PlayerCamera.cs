using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(PlayerInput))]
public class PlayerCamera : NetworkBehaviour
{
    PlayerInput playerInput;
    [SerializeField] Transform playerCam;
    [Header("Settings")]
    [SerializeField] float sensitivity = 2f;
    [SerializeField] float maxPitch = 30f;
    [SerializeField] float minPitch = -80f;
    float pitchRotation = 0f;
    float yawRotation = 0f;
    float currentMinPitch;
    float defaultMaxPitch;

    bool isRagdoll = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        currentMinPitch = minPitch;
        defaultMaxPitch = maxPitch;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { 
            playerCam.GetComponentInChildren<Camera>().enabled = false;
            return;
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
        {
            transform.rotation = Quaternion.Euler(0f, yawRotation, 0f);
        }
        
    }
}