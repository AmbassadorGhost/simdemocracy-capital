using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;

// One owner processes requests. Voters never take ownership of shared totals.
// Informal, public instance polls only; not identity verification or elections.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SDSession : UdonSharpBehaviour
{
    public TMP_Text board;
    public TMP_Text clock;
    public TMP_Text feedback;
    public string[] motions;
    [UdonSynced] private int motion;
    [UdonSynced] private int round;
    [UdonSynced] private bool votingOpen;
    [UdonSynced] private int[] voters = new int[128];
    [UdonSynced] private int[] choices = new int[128];
    [UdonSynced] private int used;
    [UdonSynced] private bool running;
    [UdonSynced] private double deadline;
    [UdonSynced] private float remaining = 180f;
    private float nextPress;
    private int displayedSecond = -1;

    private void Start() { Refresh(); }
    public override void OnDeserialization() { Refresh(); }
    public override void OnOwnershipTransferred(VRCPlayerApi player) { Refresh(); }
    public override void OnPlayerJoined(VRCPlayerApi player) { Refresh(); }
    public override void OnPlayerLeft(VRCPlayerApi player) { Refresh(); }

    public void _Press(int action)
    {
        if (!Utilities.IsValid(Networking.LocalPlayer) || !Networking.IsNetworkSettled) return;
        if (Time.time < nextPress) return;
        nextPress = Time.time + .4f;
        if (action >= 3 && !Networking.LocalPlayer.isMaster)
        {
            if (feedback != null) feedback.text = "Session controls belong to the instance master.";
            return;
        }
        if (feedback != null) feedback.text = "Request sent. Check the board for confirmation.";
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(Receive), action, round);
    }

    [NetworkCallable(maxEventsPerSecond: 4)]
    public void Receive(int action, int expectedRound)
    {
        if (!Networking.IsOwner(gameObject)) return;
        VRCPlayerApi sender = NetworkCalling.CallingPlayer;
        if (!Utilities.IsValid(sender) || expectedRound != round) return;
        if (action >= 0 && action <= 2)
        {
            if (!votingOpen) return;
            int slot = -1;
            for (int i = 0; i < used; i++) if (voters[i] == sender.playerId) slot = i;
            if (slot < 0)
            {
                if (used >= voters.Length) return;
                slot = used++;
                voters[slot] = sender.playerId;
            }
            choices[slot] = action; // A second click changes the vote, never adds one.
        }
        else
        {
            if (!sender.isMaster) return;
            if (action == 3)
            {
                if (votingOpen) return;
                round++;
                used = 0;
                votingOpen = true;
            }
            else if (action == 4) votingOpen = false;
            else if (action == 5)
            {
                if (votingOpen) return;
                motion = motions != null && motions.Length > 0 ? (motion + 1) % motions.Length : 0;
                round++;
                used = 0;
            }
            else if (action == 6)
            {
                if (running || remaining <= 0f) return;
                deadline = Networking.GetServerTimeInSeconds() + remaining;
                running = true;
            }
            else if (action == 7)
            {
                remaining = SecondsLeft();
                running = false;
            }
            else if (action == 8) { running = false; remaining = 180f; }
            else return;
        }
        RequestSerialization();
        Refresh();
    }

    private float SecondsLeft()
    {
        return running ? Mathf.Max(0f, (float)(deadline - Networking.GetServerTimeInSeconds())) : remaining;
    }
    private void Update()
    {
        int second = Mathf.CeilToInt(SecondsLeft());
        if (running && second == 0 && Networking.IsOwner(gameObject))
        {
            running = false;
            remaining = 0f;
            RequestSerialization();
        }
        if (second != displayedSecond) DrawClock(second);
    }
    private void DrawClock(int second)
    {
        displayedSecond = second;
        if (clock == null) return;
        clock.text = "SPEAKING TIME  " + (second / 60) + ":" + (second % 60).ToString("D2")
            + "\n" + (second == 0 ? "TIME EXPIRED" : running ? "FLOOR OPEN" : "PAUSED / READY");
        clock.color = second <= 30 ? new Color(1f, .72f, .3f) : Color.white;
    }
    private void Refresh()
    {
        int aye = 0, nay = 0, abstain = 0, mine = -1;
        for (int i = 0; i < used; i++)
        {
            if (choices[i] == 0) aye++;
            else if (choices[i] == 1) nay++;
            else abstain++;
            if (Utilities.IsValid(Networking.LocalPlayer) && voters[i] == Networking.LocalPlayer.playerId) mine = choices[i];
        }
        string title = motions != null && motions.Length > 0 ? motions[motion % motions.Length] : "Open discussion";
        VRCPlayerApi host = Networking.GetOwner(gameObject);
        if (board != null) board.text = "THE CHAMBER / INFORMAL POLL\n" + title + "\n\n"
            + "AYE  " + aye + "     NAY  " + nay + "     ABSTAIN  " + abstain
            + "\n" + (votingOpen ? "VOTING OPEN - click again to change your vote" : "VOTING CLOSED")
            + "\nRound " + round + " | " + used + "/128 participants"
            + "\nTally host: " + (Utilities.IsValid(host) ? host.displayName : "Connecting...");
        if (feedback != null) feedback.text = mine < 0 ? "Your vote: not recorded this round."
            : "Your recorded vote: " + (mine == 0 ? "AYE" : mine == 1 ? "NAY" : "ABSTAIN");
        DrawClock(Mathf.CeilToInt(SecondsLeft()));
    }
}

