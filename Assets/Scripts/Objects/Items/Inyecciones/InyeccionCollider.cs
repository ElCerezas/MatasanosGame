using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class InyeccionCollider : MonoBehaviour
{
    [SerializeField] private InyeccionItem inyeccionItem;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            inyeccionItem.Inject(other.gameObject);
        }
        else if (other.CompareTag("Alien"))
        {
            inyeccionItem.Inject(other.gameObject);
        }
    }
}
