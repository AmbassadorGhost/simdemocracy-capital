using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SDSessionButton : UdonSharpBehaviour
{
    public SDSession session;
    // 0/1/2 vote; 3 open; 4 close; 5 next motion; 6 start; 7 pause; 8 reset clock.
    public int action;
    public override void Interact()
    {
        if (session != null) session._Press(action);
    }
}
