using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Transform camera;

    [Header("Settings")]
    [SerializeField] float sensitivity = 2f;
    [SerializeField] float maxPitch = 80f;

    private float pitchRotation = 0f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            camera.GetComponent<Camera>().enabled = false;
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
        camera.localRotation = Quaternion.Euler(pitchRotation, 0f, 0f);

        //Horitzontal
        gameObject.transform.Rotate(Vector3.up * mouseX);
    }
}
