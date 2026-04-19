using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public class PowerableItem : NetworkBehaviour
{
    bool powered = false;

    [SerializeField] UnityEvent OnPowered;
    [SerializeField] UnityEvent OnUnplugged;
    [SerializeField] UnityEvent Power;

    public void EnablePower()
    {
        powered = true;
        OnPowered?.Invoke();
    }

    public void DisablePower()
    {
        powered = false;
        OnUnplugged?.Invoke();
    }
    public void Update()
    {
        if (powered) Power?.Invoke();
    }
}
