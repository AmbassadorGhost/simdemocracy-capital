using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.UdonNetworkCalling;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SDGavel : UdonSharpBehaviour
{
    public AudioSource sound;
    public TMP_Text caption;
    private float lastHit = -10f;
    private float until;
    public override void Interact()
    {
        if (Time.time - lastHit < 1f) return;
        lastHit = Time.time;
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Bonk));
    }
    [NetworkCallable(maxEventsPerSecond: 1)]
    public void Bonk()
    {
        if (sound != null) sound.Play();
        if (caption != null) caption.text = "ORDER IN THE COURT!";
        until = Time.time + 3f;
    }
    private void Update()
    {
        if (until > 0f && Time.time >= until) { until = 0f; if (caption != null) caption.text = "The court is in session"; }
    }
}

