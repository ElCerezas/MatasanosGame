using UnityEngine;

public class Impresora3D : PoweredDevice
{
    [Header("BaseConfig")]
    public Transform printingAnchor;
    int printingIndex = 0;

    [Header("PrintingList")]
    [SerializeField] public PrintingObject[] printableObjects;
    public override void Powered()
    {
        throw new System.NotImplementedException();
    }
    public void SwitchItem(bool nextItem = true)
    {
        printingIndex += nextItem ? 1 : -1;
        
    }
}

[System.Serializable]
public struct PrintingObject
{
    public string displayName;
    public float printingTime;
    public GameObject printingObject;
}