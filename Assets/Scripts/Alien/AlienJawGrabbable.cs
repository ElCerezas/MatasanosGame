using Unity.Netcode;
using UnityEngine;

public class AlienJawGrabbable : NetworkBehaviour, IGrabbable
{
    [Header("Drag Settings")]
    [SerializeField] private float dragSensitivity = 0.8f;
    [SerializeField] private float maxDragDistance = 1.5f;

    [Header("Feel")]
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float resistanceSpeed = 3f;

    [Header("Animation")]
    // Asigna aquí el Animator del propio Alien/Mandíbula en el Inspector
    [SerializeField] private Animator jawAnimator;

    // Sincroniza el valor objetivo en red automáticamente sin usar RPCs problemáticos
    private NetworkVariable<float> jawOpenAmount = new NetworkVariable<float>(0f);

    // Variable local para el suavizado (Lerp) en cada cliente
    private float currentVisualAmount;

    private NetworkObject grabberNetObj;
    private ulong grabberClientId = ulong.MaxValue;
    private Vector3 grabStartPosition;
    private float grabStartJawAmount;

    public override void OnNetworkSpawn()
    {
        // Evita saltos bruscos de animación al aparecer el objeto
        currentVisualAmount = jawOpenAmount.Value;
    }

    void Update()
    {
        // 1. EL SERVIDOR CALCULA LA FUERZA/DISTANCIA
        if (IsServer)
        {
            float target = 0f;

            if (grabberNetObj != null)
            {
                // Cambiado a Vector3.Distance para medir el "tiro" o alejamiento del jugador en cualquier dirección
                float distance = Vector3.Distance(grabberNetObj.transform.position, grabStartPosition);
                target = grabStartJawAmount + (distance * dragSensitivity / maxDragDistance);
            }
            else
            {
                target = 0f;
            }

            // El servidor actualiza el valor de red de forma constante
            float speed = (grabberNetObj != null ? smoothSpeed : resistanceSpeed);
            jawOpenAmount.Value = Mathf.MoveTowards(jawOpenAmount.Value, Mathf.Clamp01(target), speed * Time.deltaTime);
        }

        // 2. TODOS LOS CLIENTES REALIZAN EL LERP VISUAL
        if (jawAnimator != null)
        {
            float lerpSpeed = (grabberNetObj != null ? smoothSpeed : resistanceSpeed);

            // Aquí ocurre la magia del Lerp frame a frame
            currentVisualAmount = Mathf.Lerp(currentVisualAmount, jawOpenAmount.Value, Time.deltaTime * lerpSpeed);

            // Aplicamos el parámetro al Animator del objeto
            jawAnimator.SetFloat("MouthOpenPercent", currentVisualAmount);
        }
    }

    public ulong GetNetworkObjectID() => NetworkObjectId;

    public void AddGrabber(ulong clientId, NetworkObject playerNetObj)
    {
        if (grabberNetObj != null) return;

        grabberClientId = clientId;
        grabberNetObj = playerNetObj;
        grabStartPosition = playerNetObj.transform.position;
        grabStartJawAmount = jawOpenAmount.Value;
    }

    public void RemoveGrabber(ulong clientId)
    {
        if (clientId != grabberClientId) return;

        grabberNetObj = null;
        grabberClientId = ulong.MaxValue;
    }
}