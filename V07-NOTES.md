# v0.7: Movie night, dusk and the Constitution

Open `Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v07.unity`. v0.6 and earlier are untouched.
The build script is `Assets/SimDemocracy/Editor/SDCapitalV07.cs`. **SimDemocracy → Create v0.7 Scene**
builds from v0.6 and bakes; it refuses to overwrite an existing v0.7.

## What changed

**Cinema works.** Booth behind the right-hand seats: paste a link into the bar and press Enter.
Everyone in the instance sees the same video at the same time, and late joiners jump to the
current position. Buttons: PLAY / PAUSE, STOP, RESYNC (local), VOL - / VOL + (local),
HOST LOCK (instance master only; while locked, only the master can change or pause).
The status line shows who picked the video. Built on the SDK's Unity video player
(`Scripts/SDCinema.cs`, `Scripts/SDCinemaButton.cs`), 1080p max.

**Dusk.** Painted sunset sky (`Art-v07/Dusk-sky.png`, sun placed on the light's bearing),
low warm sun, haze, and baked light at the four lanterns.
The 42 blank cream wall panels are gone: 21 became dusk windows (rainy ones in the café),
the cinema and side room got dark felt acoustic panels, and 5 stay behind existing displays.
47 low-poly trees around the edge of the map.

**Square.** Fountain in the north half of the square with 8 seats on the rim, and a duck on top.
A TIMELINE noticeboard beside it (founding, Painted Wolf Day, the Post, the Danyowave, FBN,
the Ducks Constitution repeal, touching grass, "You are here").

**President's office.** PRESIDENTS OF SIMDEMOCRACY honours board on the west wall: all 81
numbered presidencies from your saved wiki list (1-56 and 133-157; 57-132 marked "to be added").
Three desk drawers on the president's side: MORE AMENDMENTS, BACKUP CLERK, THE LAST BISCUIT.

**Courtroom.** Witness stand with a seat, right of the bench.

**Café.** The four table cups are now mugs you can pick up (synced). Optional mirror by the
entrance, off by default, with a MIRROR switch; avatars-only reflection to keep it cheap on Quest.

**Archives.** The Constitution book now opens the full text: Preamble plus Parts 1-9, from the
live archive (revision 6870, 1 Oct 2026; your local May 2025 copy was older). Footnotes are
stripped. NEXT ENTRY walks through the parts. Source files: `Data/constitution-r6870.wiki`
and `.json`.

## Checks run (Unity editor, batch mode)

C# and Udon compilation; scene wiring (72 seat stations); cinema wiring (URL field event,
6 buttons, behaviour on the video player object); 4 pickups; mirror present and off;
131 archive pages fit the reader; no overflowing text; 7 doorways and the paths around the
fountain clear for a player-sized capsule; rear walls and return buttons unchanged.

To fit this machine's free memory, the bake uses 512 px lightmap pages and a coarser
lightmap on the open ground plane. If a bake loops on "out of memory", close a few apps
and bake again. `SDCapitalV07.Verify` re-runs all checks on the saved scene.

Review renders (44 room views plus close-ups of each new feature) are in `../v07-Previews`.
The mirror shows white in editor renders; VRChat draws the reflection.

## Not tested yet

Video playback can't be tested in batch mode, so the cinema needs a real check: Build & Test
with two clients, paste a link, pause, late-join. YouTube links resolve on PC; on Quest, test
the links you actually plan to use. Also still pending from v0.6: SDK validation, headset
readability, multiplayer, Quest performance. Nothing has been uploaded.

The Senate debate MP4 on your desktop can't be played from disk in VRChat. Upload it somewhere
(e.g. YouTube, unlisted is fine) and paste that link at the booth.
