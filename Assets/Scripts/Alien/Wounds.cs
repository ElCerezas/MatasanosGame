using Unity.Netcode;
using UnityEngine;

public class Wounds : NetworkBehaviour
{
    public void Heal()
    {
        Destroy(gameObject);
    }

    public void Desinfect()
    {

    }
}
