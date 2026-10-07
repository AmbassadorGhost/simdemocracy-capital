using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SDCinemaButton : UdonSharpBehaviour
{
    public SDCinema cinema;
    // 0 play/pause, 1 stop, 2 resync, 3 volume down, 4 volume up, 5 host lock.
    public int action;
    public override void Interact()
    {
        if (cinema != null) cinema._Press(action);
    }
}
