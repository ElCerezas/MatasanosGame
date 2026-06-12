using UnityEngine;
using System.Collections.Generic;

public class SettingsCloseByInput : MonoBehaviour
{
    [SerializeField] private List<GameObject> panelsToClose;
    public void OnDisable()
    {
        foreach (var panel in panelsToClose)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
