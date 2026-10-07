# v0.6 — Walkthrough fixes

Open `Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v06.unity` in Unity 2022.3.22f1. Earlier scenes are preserved.

The Bonk room starts with the image hidden. PRESS ME shows the supplied image for the person pressing it; a second press hides it. This is a local reveal, not a synchronized change for everyone.

The visual review covered 44 camera views: four angles in each of seven rooms, four in the square, four approaches, four exterior views, and four with Bonk revealed. Most interior views use a 1.65 m eye height. Before and after renders are in the adjacent `Walkthrough-Before` and `Walkthrough-After` folders, with each sheet ordered top-left, top-right, bottom-left, bottom-right.

Changes:

- Closed unused rear openings in the office, archives and cinema, and completed the rear trim in the courtroom and Bonk room.
- Turned seven return controls toward the rooms and removed mirrored text visible through the backs of signs.
- Connected paths to the courtroom, archives, café, cinema and side-room entrances.
- Mounted flags and display panels against walls, backed archive category labels, and softened cinema and side-room lighting.
- Added two guest chairs in the president's office and corrected the title and inbox placement.
- Raised the Bonk poster above the wall trim and made its button easier to read.

Validation: Unity C# and Udon compilation, a successful lighting bake, 63 seat station wiring checks, five solid rear-wall checks, seven return-control orientation checks, and seven doorway capsule checks (0.5 m wide, 1.6 m tall). All 20 pages across 11 archive entries fit the display. The Bonk interaction method passed show/hide checks in the editor; the scene is saved hidden. Final renders reported no missing materials/shaders or overflowing active text.

These are editor logic checks and rendered camera views, not a VRChat client walkthrough. SDK build validation, headset readability, multiplayer behavior and Quest performance still need testing. Cinema playback remains unfinished.
