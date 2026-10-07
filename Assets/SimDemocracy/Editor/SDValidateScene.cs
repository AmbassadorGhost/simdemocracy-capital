using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;

public static class SDValidateScene
{
    [MenuItem("SimDemocracy/Check Scene Wiring")]
    public static void Check()
    {
        var errors = new List<string>();
        var descriptors = Object.FindObjectsOfType<VRCSceneDescriptor>(true);
        if (descriptors.Length != 1) errors.Add("Expected one VRCSceneDescriptor.");
        foreach (var d in descriptors)
            if (d.spawns == null || d.spawns.Length == 0 || d.spawns[0] == null) errors.Add("World spawn is missing.");
        var seats = Object.FindObjectsOfType<SDSeat>(true);
        if (seats.Length == 0) errors.Add("No usable seats found. Generate the v0.2 scene first.");
        foreach (var s in seats)
        {
            if (s.station == null) { errors.Add("Seat missing station: " + s.name); continue; }
            if (s.station.stationEnterPlayerLocation == null || s.station.stationExitPlayerLocation == null)
                errors.Add("Seat missing enter/exit position: " + s.name);
            if (s.station.disableStationExit) errors.Add("Seat prevents normal exit: " + s.name);
            if (s.GetComponent<Collider>() == null) errors.Add("Seat missing interaction collider: " + s.name);
        }
        var sessions = Object.FindObjectsOfType<SDSession>(true);
        if (sessions.Length != 1) errors.Add("Expected one shared debate session.");
        foreach (var s in sessions)
            if (s.board == null || s.clock == null || s.feedback == null || s.motions == null || s.motions.Length == 0)
                errors.Add("Debate session references are incomplete.");
        foreach (var b in Object.FindObjectsOfType<SDSessionButton>(true))
            if (b.session == null || b.action < 0 || b.action > 8) errors.Add("Invalid session button: " + b.name);
        foreach (var a in Object.FindObjectsOfType<SDAction>(true))
        {
            if (a.action == 6 && a.destination == null) errors.Add("Return button missing destination.");
            if (a.action == 5 && a.reveal == null) errors.Add("Doge button missing reveal object.");
            if (a.action <= 4 && a.reader == null) errors.Add("Archive button missing reader.");
        }
        foreach (var r in Object.FindObjectsOfType<SDArchiveReader>(true))
            if (r.display == null || r.documents == null || r.documents.Length == 0) errors.Add("Archive reader is incomplete.");
        if (errors.Count > 0)
        {
            foreach (var error in errors) Debug.LogError("SimDemocracy: " + error);
            if (Application.isBatchMode) throw new System.InvalidOperationException(string.Join("\n", errors));
            EditorUtility.DisplayDialog("Scene wiring needs attention", string.Join("\n", errors), "OK");
        }
        else
        {
            Debug.Log("SimDemocracy scene wiring passed. " + seats.Length + " seat stations found. SDK validation and play testing still required.");
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Scene wiring checked", seats.Length + " seats found. Run SDK validation and Build & Test next.", "OK");
        }
    }
}
