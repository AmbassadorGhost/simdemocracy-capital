# Capital v0.3 — first textured art pass

Open `Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v03.unity` in the existing
SimDemocracy-Capital project. The v0.2 scenes are preserved separately.

## Added

- Five original 512×512 procedural textures: oak boards, woven fabric,
  limestone paving, plaster grain and streaked glass. Shared materials use the
  VRChat Mobile Standard Lite shader. The supplied flag remains unchanged.
- Honey-oak floors, walnut furnishings, mulberry/oatmeal upholstery, navy
  wainscoting, brass rails, plaster walls and limestone paths.
- Timber chair frames and legs, sofa arms/cushions/feet, café plants, ceramic
  cups, bar fluting, shelving, a framed menu and decorative window frames.
- Ceiling beams and pendant fixtures throughout the rooms. Interior and
  sunlight illumination is baked; fixture lights are not realtime.
- Light probes for smaller details, plus a dedicated lightmap UV layout on
  box meshes. The café's unused doorway behind the counter is now a back wall;
  its main entrance and return control remain available.

The changes use original procedural art and Unity primitives, with no asset
store or third-party art downloads. They are a first art pass, not final custom
modeling. Café windows are decorative surfaces, not animated rain or views into
another room.

## Validation

The v0.3 scene passed Unity/Udon compilation and the scene-wiring check with all
61 station seats. Actual Unity café/chamber/overview previews were rendered
and inspected. Geometry measured approximately 25,000 mesh triangles before
the final UV cleanup. Renderer count remains about 1,100 before Unity's static
batching; triangle count alone does not establish performance.

The final bake uses three non-directional lightmaps and interior light probes.
Its log reports no overlapping-UV warnings. Small decorative meshes contribute
baked shadows while receiving light through probes, avoiding tiny lightmap
charts. Baked lighting was reviewed in actual Unity renders, not mockups.

Quest frame time, Android build size, multiplayer, seat poses and full VRChat
SDK Build & Test still need verification. Cinema playback, full archive text,
community artwork and final optimization remain unfinished.

The current portable v0.2 ZIP is an older source snapshot. Use this local Unity
project for the v0.3 scene and its baked lighting/material assets.
