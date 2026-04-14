using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(PlayerInput))]
public class PlayerCamera : NetworkBehaviour
{
    PlayerInput playerInput;
    [SerializeField] Transform playerCam;

    [Header("Settings")]
    [SerializeField] float sensitivity = 2f;
    [SerializeField] float maxPitch = 80f;

    private float pitchRotation = 0f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { 
            playerCam.GetComponentInChildren<Camera>().enabled = false;
            //playerCam.GetComponent<AudioListener>().enabled = false;
            return;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!IsOwner) return;
        Look();
    }

    private void Look()
    {
        Vector2 lookInput = playerInput.LookInput * sensitivity;
        float mouseX = lookInput.x;
        float mouseY = lookInput.y;

        //Vertical
        pitchRotation -= mouseY;
        pitchRotation = Mathf.Clamp(pitchRotation, -maxPitch, maxPitch);
        playerCam.localRotation = Quaternion.Euler(pitchRotation, 0f, 0f);

        //Horitzontal
        gameObject.transform.Rotate(Vector3.up * mouseX);
    }
}
