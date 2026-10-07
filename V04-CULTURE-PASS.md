# SimDemocracy v0.4 cultural pass

The working project is `C:\Users\gkgho\VR Chat SimDem\SimDemocracy-Capital`.
Open `Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v04.unity`. The saved v0.3 scene is retained.

This version starts from the user's saved v0.3 scene, rather than the older copy in the Codex outputs folder.

## Changes

- Close the unused rear opening behind the courtroom bench, with matching wall, wainscot and collision.
- Add a separate clerk's desk and original duck figurine, with a historical Ducks Constitution label.
- Add a Senate Reading Room nook with an oversized fictional Biscuit Bill. Existing seats and voting controls remain in place.
- Add a newspaper rack with newly typeset exhibit covers for The SimDemocracy Post and FBN, dated to their historical sources.
- Add Risistas Corner as an original interpretation panel, without reproducing chat messages or unverified emoji art.
- Add decorative Tau prices to the café menu. There is no wallet or purchase integration.
- Add an optional grass garden with a plaque explaining the 2026 plant-contact ruling, separately from self-exclusion law.
- Add an original abstract Danyowave arrival panel in the square.
- Add an interior archives name plaque and eight sourced cultural entries to the existing archive reader. Select the gold Cultural Field Guide volume, then use the reader's existing entry/page controls.

The source of record for those eight additions is `Assets/SimDemocracy/Data/culture-v04.json`. It stores report IDs, eras, evidence labels, sources, display copy, longer context and artwork status. The editor pass uses that record for labels and archive content. Changing the JSON later does not automatically update an already-built scene.

## Scope

This is a focused first pass. It does not yet add the painted-wolf sculpture, hidden Librarian's Office, playable chess, video playback, an honors cabinet or a live community feed. The existing Doge room remains in place.

Historical dates and claims are drawn from the user-supplied research report. This implementation does not claim to independently verify its Discord observations or all cited legal sources. Props and exhibit covers are original interpretations.

## Verification

Unity C# compilation and non-editor UdonSharp compilation passed. The custom scene wiring checker found all 61 seat stations. A physics raycast confirms that the new rear courtroom wall blocks the former opening. Reloading v0.4 confirmed eleven archive entries: the original three plus eight cultural records.

The lighting bake uses three lightmaps. An initial UV-overlap warning on the new wall was corrected with separated UV islands. Actual Unity camera renders were reviewed for the courtroom, duck clerk, reading nook, café, Risistas Corner, archives and square; text placement and entrance routes were refined from those renders.

These checks are not a VRChat SDK Build & Test, a full SDK validation, a multiplayer session, a headset readability test or a Quest performance test. Those remain unperformed. No world has been uploaded.
