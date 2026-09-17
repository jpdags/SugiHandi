# SugiHandi — Audio Credits

All audio sourced from OpenGameArt.org. Licenses are compatible with the demo build.

---

## Music

### forest_river_spirits.mp3
- Source: https://opengameart.org/content/forest-river-spirits
- License: CC-BY 3.0 (Attribution required)
- Suggested use: Background music for sacred site / Anito spirit encounters, forested hill scenes
- Attribution: Credit the original author from the OGA page in your game credits

### waterbender_maritime.ogg
- Source: https://opengameart.org/content/waterbenderglory-to-the-machine-tsorthan-grove-tricksntraps
- License: CC-BY 3.0 (Attribution required)
- Suggested use: Maritime/river travel scenes, trade port atmosphere, dusk/dawn cinematic sequences
- Attribution: Credit the original author from the OGA page in your game credits

---

## Ambience

### water_ambience.mp3
- Source: https://opengameart.org/content/water-ambience
- License: CC0 / CC-BY 3.0 (dual-licensed — no attribution required under CC0)
- Suggested use: River village background loop, docking areas, open water scenes
- Attribution: Optional — "Credit isn't required, but appreciated" (SpringySpringo)

### forest_river_spirits_loop.ogg
- Source: https://opengameart.org/content/forest-river-spirits (OGG variant)
- License: CC-BY 3.0
- Suggested use: Loopable background for forested areas and sacred ruins
- Attribution: Same as forest_river_spirits.mp3

---

## SFX

### preview_water.mp3
- Source: https://opengameart.org/content/waterbenderglory-to-the-machine-tsorthan-grove-tricksntraps
- License: CC-BY 3.0
- Suggested use: Water interaction sounds, environmental puzzle triggers near water, ritual ambience

---

## Unity Import Notes

- Import all files at Compressed (Vorbis) for mobile builds
- Set Load Type to Streaming for long ambience/music tracks
- Set Load Type to Decompress On Load for short SFX
- Check Loop on all Ambience tracks in the AudioSource component
- Assign through GameInterface.cs audio mixer channels

## Recommended Unity Audio Mixer Setup

  AudioMixer (SugiHandi)
  Music      -- BGM channel: fade in/out on scene transitions
  Ambience   -- Looped ambient: always playing at low volume
  SFX        -- One-shot events: artifact pickup, UI clicks, NPC interaction sounds
