using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

// One shared cinema screen. Whoever submits a link records the server time at
// which the video started; every viewer seeks to that clock, so late joiners
// and slow buffers land on the same frame. The instance master can lock controls.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SDCinema : UdonSharpBehaviour
{
    public BaseVRCVideoPlayer player;
    public VRCUrlInputField urlField;
    public Renderer screen;
    public GameObject idleCard;
    public AudioSource speaker;
    public TMP_Text status;

    [UdonSynced] private VRCUrl url = VRCUrl.Empty;
    [UdonSynced] private int version;
    [UdonSynced] private string picker = "";
    [UdonSynced] private double startedAt;
    [UdonSynced] private bool paused;
    [UdonSynced] private float pausedAt;
    [UdonSynced] private bool locked;

    private int loadedVersion;
    private bool loading;
    private bool queued;
    private bool failed;
    private float nextLoad;
    private float nextPress;
    private float nextCheck;
    private float noticeUntil;
    private string notice = "";

    // Synced state can arrive before Start; never blank a screen that is already loading or playing.
    private void Start() { if (!HasUrl()) ShowIdle(true); Draw(); }

    private bool HasUrl() { return url != null && !string.IsNullOrEmpty(url.Get()); }

    private bool CanControl()
    {
        VRCPlayerApi me = Networking.LocalPlayer;
        if (!Utilities.IsValid(me)) return false;
        if (locked && !me.isMaster) { Notice("Host only right now"); return false; }
        return true;
    }

    private void TakeOwnership()
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }

    // Called by the URL field's OnEndEdit event.
    public void OnURLChanged()
    {
        if (urlField == null) return;
        VRCUrl next = urlField.GetUrl();
        urlField.SetUrl(VRCUrl.Empty);
        if (next == null || string.IsNullOrEmpty(next.Get()) || !CanControl()) return;
        TakeOwnership();
        url = next;
        version++;
        picker = Networking.LocalPlayer.displayName;
        startedAt = 0;
        paused = false;
        pausedAt = 0;
        RequestSerialization();
        Load();
    }

    // 0 play/pause, 1 stop, 2 resync, 3 volume down, 4 volume up, 5 host lock.
    public void _Press(int action)
    {
        if (Time.time < nextPress) return;
        nextPress = Time.time + .5f;
        if (action == 2) { Resync(true); Notice("Resynced"); return; }
        if (action == 3 || action == 4)
        {
            if (speaker == null) return;
            speaker.volume = Mathf.Clamp01(speaker.volume + (action == 4 ? .1f : -.1f));
            Notice("Volume " + Mathf.RoundToInt(speaker.volume * 100f) + "%");
            return;
        }
        VRCPlayerApi me = Networking.LocalPlayer;
        if (!Utilities.IsValid(me)) return;
        if (action == 5)
        {
            if (!me.isMaster) { Notice("Only the instance master can lock"); return; }
            TakeOwnership();
            locked = !locked;
            RequestSerialization();
            Draw();
            return;
        }
        if (!CanControl() || !HasUrl()) return;
        TakeOwnership();
        if (action == 0)
        {
            if (startedAt <= 0) return;
            double now = Networking.GetServerTimeInSeconds();
            if (paused) { startedAt = now - pausedAt; paused = false; }
            else { pausedAt = (float)(now - startedAt); paused = true; }
        }
        else if (action == 1)
        {
            url = VRCUrl.Empty;
            version++;
            picker = "";
            startedAt = 0;
            paused = false;
        }
        else return;
        RequestSerialization();
        Apply();
    }

    public override void OnDeserialization() { Apply(); }

    private void Apply()
    {
        if (loadedVersion != version)
        {
            if (HasUrl()) Load();
            else { loadedVersion = version; loading = false; failed = false; player.Stop(); ShowIdle(true); }
        }
        else Resync(false);
        Draw();
    }

    private void Load()
    {
        loadedVersion = version;
        loading = true;
        failed = false;
        ShowIdle(true);
        float wait = nextLoad - Time.time;
        if (wait > 0f)
        {
            // Video players accept one new URL every five seconds.
            if (!queued) { queued = true; SendCustomEventDelayedSeconds(nameof(_LoadQueued), wait + .1f); }
            Draw();
            return;
        }
        nextLoad = Time.time + 5.5f;
        player.PlayURL(url);
        Draw();
    }

    public void _LoadQueued()
    {
        queued = false;
        if (loading && loadedVersion == version && HasUrl()) Load();
    }

    private float Expected()
    {
        if (paused) return pausedAt;
        return (float)(Networking.GetServerTimeInSeconds() - startedAt);
    }

    private void Resync(bool force)
    {
        if (loading || !HasUrl() || startedAt <= 0 || !player.IsReady) return;
        float target = Mathf.Max(0f, Expected());
        float duration = player.GetDuration();
        if (duration > 0f && duration < 1000000f && target >= duration - .5f) return;
        if (paused)
        {
            if (player.IsPlaying) player.Pause();
            if (force || Mathf.Abs(player.GetTime() - target) > .5f) player.SetTime(target);
            return;
        }
        if (!player.IsPlaying) player.Play();
        if (force || Mathf.Abs(player.GetTime() - target) > 1.5f) player.SetTime(target);
    }

    public override void OnVideoStart()
    {
        loading = false;
        failed = false;
        ShowIdle(false);
        if (Networking.IsOwner(gameObject) && startedAt <= 0 && HasUrl())
        {
            startedAt = Networking.GetServerTimeInSeconds() - player.GetTime();
            RequestSerialization();
        }
        // Deferred so a Play() inside Resync never re-enters this event.
        SendCustomEventDelayedSeconds(nameof(_SyncSoon), .25f);
        Draw();
    }

    public void _SyncSoon() { Resync(false); Draw(); }

    public override void OnVideoEnd() { ShowIdle(true); Draw(); }

    public override void OnVideoError(VideoError videoError)
    {
        if (videoError == VideoError.RateLimited)
        {
            nextLoad = Time.time + 5.5f;
            Load();
            return;
        }
        loading = false;
        failed = true;
        ShowIdle(true);
        Draw();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi newOwner) { Draw(); }

    private void Update()
    {
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + 4f;
            if (loadedVersion == version) Resync(false);
        }
        if (noticeUntil > 0f && Time.time > noticeUntil) { noticeUntil = 0f; Draw(); }
    }

    private void ShowIdle(bool idle)
    {
        if (idleCard != null) idleCard.SetActive(idle);
        if (screen != null) screen.enabled = !idle;
    }

    private void Notice(string message)
    {
        notice = message;
        noticeUntil = Time.time + 2.5f;
        Draw();
    }

    private void Draw()
    {
        if (status == null) return;
        string line;
        if (noticeUntil > 0f) line = notice;
        else if (failed) line = "Couldn't play that link";
        else if (loading && HasUrl()) line = "Loading...";
        else if (HasUrl()) line = (paused ? "Paused" : "Now playing") + (string.IsNullOrEmpty(picker) ? "" : "  /  picked by " + picker);
        else line = "Paste a video link above";
        status.text = locked ? line + "\nHOST ONLY" : line;
    }
}
