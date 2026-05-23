using UnityEngine;

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
        else if (other.CompareTag("MixerFlask"))
        {
            MixerFlask f = other.GetComponent<MixerFlask>();
            f.Empty();
            inyeccionItem.Fill(f.liquid.Value, f.color.Value);
        }
    }
}
