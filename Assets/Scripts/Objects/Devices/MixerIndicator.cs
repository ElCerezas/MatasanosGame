using Unity.Netcode;
using UnityEngine;

public class MixerIndicator : MonoBehaviour
{
    [SerializeField] private GameObject Empty;
    [SerializeField] private GameObject Level1;
    [SerializeField] private GameObject Level2;
    [SerializeField] private GameObject Level3;
    [SerializeField] private GameObject Level4;

    public void UpdateIndicator(int level)
    {
        Empty.SetActive(level == 0);
        Level1.SetActive(level == 1);
        Level2.SetActive(level == 2);
        Level3.SetActive(level == 3);
        Level4.SetActive(level == 4);
    }

}
