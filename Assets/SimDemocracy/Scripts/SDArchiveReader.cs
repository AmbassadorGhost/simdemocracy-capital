using TMPro;
using UdonSharp;
using UnityEngine;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SDArchiveReader : UdonSharpBehaviour
{
    public TMP_Text display;
    public string[] titles;
    [TextArea] public string[] documents;
    public int linesPerPage = 12;
    private int entry;
    private int page;
    private void Start() { Show(); }
    public void Select(int index) { entry = index; page = 0; Show(); }
    public void NextEntry() { if (documents == null || documents.Length == 0) return; entry = (entry + 1) % documents.Length; page = 0; Show(); }
    public void PreviousEntry() { if (documents == null || documents.Length == 0) return; entry = (entry + documents.Length - 1) % documents.Length; page = 0; Show(); }
    public void NextPage() { page++; Show(); }
    public void PreviousPage() { page = Mathf.Max(0, page - 1); Show(); }
    private void Show()
    {
        if (display == null) return;
        if (documents == null || documents.Length == 0) { display.text = "No catalog entries. Import a catalog and rebuild."; return; }
        entry = Mathf.Clamp(entry, 0, documents.Length - 1);
        string document = documents[entry];
        if (document == null) document = "No text provided for this catalog entry.";
        string[] lines = document.Split('\n');
        int size = Mathf.Max(1, linesPerPage);
        int count = Mathf.Max(1, (lines.Length + size - 1) / size);
        page = Mathf.Clamp(page, 0, count - 1);
        string heading = titles != null && entry < titles.Length ? titles[entry] : "Archive";
        string result = heading + "\nEntry " + (entry + 1) + "/" + documents.Length + "  |  Page " + (page + 1) + "/" + count + "\n\n";
        for (int i = page * size; i < Mathf.Min(lines.Length, (page + 1) * size); i++) result += lines[i] + "\n";
        display.text = result;
    }
}

