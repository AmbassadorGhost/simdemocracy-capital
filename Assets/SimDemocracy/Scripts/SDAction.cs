using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SDAction : UdonSharpBehaviour
{
    // 0 select entry, 1 next entry, 2 previous entry, 3 next page,
    // 4 previous page, 5 local gag reveal, 6 return to square.
    public int action;
    public int entry;
    public SDArchiveReader reader;
    public GameObject reveal;
    public Transform destination;
    public override void Interact()
    {
        if (action == 5) { if (reveal != null) reveal.SetActive(!reveal.activeSelf); return; }
        if (action == 6) { if (Utilities.IsValid(Networking.LocalPlayer) && destination != null) Networking.LocalPlayer.TeleportTo(destination.position, destination.rotation); return; }
        if (reader == null) return;
        if (action == 0) reader.Select(entry);
        else if (action == 1) reader.NextEntry();
        else if (action == 2) reader.PreviousEntry();
        else if (action == 3) reader.NextPage();
        else if (action == 4) reader.PreviousPage();
    }
}
