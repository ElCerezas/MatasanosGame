using UnityEngine;

public class InyeccionCollider : MonoBehaviour
{
    [SerializeField] private InyeccionItem inyeccionItem;
    private LiquidType liquidGot = LiquidType.Empty;
    private Color colorGot = Color.white;
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
            liquidGot = f.liquid.Value;
            colorGot = f.color.Value;
            inyeccionItem.Fill(liquidGot, colorGot);
            f.Empty();
        }
    }
}
