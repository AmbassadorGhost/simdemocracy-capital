using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SDSeat : UdonSharpBehaviour
{
    public VRC.SDK3.Components.VRCStation station;
    public override void Interact()
    {
        if (station != null && Utilities.IsValid(Networking.LocalPlayer))
            station.UseStation(Networking.LocalPlayer);
    }
}
