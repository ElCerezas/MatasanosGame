using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class Fuse : PoweredDevice
{
    [SerializeField] GeneratorSystem gen;
    [SerializeField] float undoTime;
    [SerializeField] Fuse otherFuse;
   
    public NetworkVariable<bool> isSwitched = new NetworkVariable<bool>(true);
    [SerializeField]NetworkAnimator animator;
    float undoTimer = 0f;



    private void Update()
    {
        if (!IsServer) return;
        if (!hasPower.Value && isSwitched.Value)
        {
            undoTimer += Time.deltaTime;
            if (undoTimer > undoTime)
            {
                isSwitched.Value = false;
                undoTimer = 0f;
                animator.Animator.SetBool("IsEnergized", isSwitched.Value);
            }
        }
    }
    public override void Powered()
    {
        if (!IsServer) return;
        isSwitched.Value = hasPower.Value;
        undoTimer = 0f;
        animator.Animator.SetBool("IsEnergized", isSwitched.Value);
    }
    public void Switched()
    {
        if (isSwitched.Value || hasPower.Value) return;
        isSwitched.Value = true;
        undoTimer = 0f;
        animator.Animator.SetBool("IsEnergized", isSwitched.Value);

        if (otherFuse.isSwitched.Value) gen.TurnOnGenerator();
    }

}
