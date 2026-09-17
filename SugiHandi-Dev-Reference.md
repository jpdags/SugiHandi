# SugiHandi — Developer Reference (Demo Build)

> Source: distilled from the capstone "Methodology" chapter. Rewritten as an actionable dev reference, not a research document. Scope has been deliberately cut down from a 5-chapter campaign to a **single vertical-slice demo** that proves out the core mechanics. Any content below marked **[DEMO]** applies to what should actually get built now; content marked **[FUTURE / CUT]** is preserved for context but is explicitly out of scope.

---

## 1. Project Snapshot

**SugiHandi** is a 2D top-down, pixel-art Android game built in Unity. The player is the **Manaog**, a navigator in a post-Gabok, pre-colonial maritime version of Butuan. The game teaches the **Butuanon language** contextually — through NPC dialogue, environmental text, and lore — never through direct quizzes or vocabulary drills.

**Core rule that governs every system:** Butuanon exposure must always be *incidental to play*, never an explicit lesson. No quiz UI, no "translate this" mechanics, no forced vocabulary gates.

---

## 2. Scope Change: Full Game → Demo

The original document specifies a **Prologue + 5 chapters + Ending**, each with its own storyboard, unique NPCs, and a full progression arc.

**For this build, that is cut down to one self-contained demo slice.** The goal isn't to ship narrative content — it's to prove the mechanics work end-to-end.

### Demo scope should include:
- **One scene** (a single settlement or dock area), fully art-dressed per the pixel-art spec below.
- **2–3 NPCs** with branching dialogue (enough to prove the branching + Kodeks logging pipeline, not a full cast).
- **1 short quest chain** (accept → objective → complete → reward), enough to prove `InteractionHandler` ↔ `ProgressTracker` ↔ `GameInterface` communication.
- **1 environmental interaction puzzle** (the "tap markers in order" pattern from the old Chapter 3 spec) — this is the most mechanically distinct system and is worth proving early, independent of story chapter.
- **A functioning Kodeks (codex)** panel, even if it only has a handful of entries.
- **Main menu → gameplay → summary/return loop**, fully working, since this is the connective tissue the whole demo sits inside.

### Explicitly [FUTURE / CUT] for now:
- Multi-chapter narrative structure, Prologue/Ending framing.
- Anito spirit encounters (unless you want to fold this into the one environmental puzzle above).
- Artifact collection *as a full gallery system* — a single artifact examine screen is enough to prove the interaction; don't build the full growing-gallery UI yet.
- World map with sequential chapter-node unlocking — irrelevant with one scene.
- Backend server / save sync (see §9 and the flags at the end).
- The PENS/SUS research instruments, pre/post-test surveys, and formal evaluation methodology — these are thesis-evaluation concerns, not dev tasks. Don't build UI or data plumbing for them unless a stakeholder specifically asks.

---

## 3. Core Design Pillars

1. **Language exposure is incidental, never instructional.** No dialogue box ever shows a translation. Meaning comes from gesture cues, environmental markers, and quest context.
2. **Choices matter narratively**, not just cosmetically — even in the demo, branching dialogue should lead to genuinely different NPC responses/Kodeks entries.
3. **No failure states in exploration/puzzle content.** The environmental interaction sequences (stone-marker puzzle, etc.) have no timer and no fail condition.
4. **Touch-first UI.** Every interaction is tap-to-move / tap-to-interact — designed for a wide range of ages and phone experience levels.

---

## 4. Tech Stack & Tooling

| Category | Choice |
|---|---|
| Engine | Unity (Android Build Support module) |
| Language | C# (Unity gameplay logic) |
| Pixel Art | Aseprite 1.3.15 |
| UI/Wireframe | Figma |
| Rendering | Universal Render Pipeline (URP) — 2D lighting |
| Camera | Cinemachine |
| Maps | Unity Tilemaps |
| Text/UI | TextMeshPro (handles Butuanon glyph rendering) |
| Input | Unity Input System (touch) |
| Audio | Unity Audio Mixer |
| Tweening/UI transitions | DOTween |
| Version control | Git / GitHub |
| Target OS | Android 8.0+ |
| Dev OS | Windows 11 |

**[DEMO]** All of the above tools apply to the demo scope as-is — nothing here needs cutting, it's all core to proving out the mechanics.

---

## 5. Game Mechanics (Detailed)

These six mechanics are the actual product of this project — the demo exists to prove they work together correctly. Keep this section as your implementation spec.

### 5.1 Tap-to-Move / Tap-to-Interact
- Player taps a destination tile → Manaog pathfinds/walks there.
- Tapping an NPC, object, or environmental marker triggers its interaction.
- No virtual joystick, no drag-to-move — single-tap only, chosen for touch accessibility across age groups.

### 5.2 Branching Dialogue System
- Every NPC interaction presents **2–3 player response options**.
- Choices carry real narrative weight: different alliance paths, different Kodeks entries, different follow-up scenes — not just flavor text.
- NPCs speak in natural English with Butuanon words embedded inline.
- **Hard rule: no translation is ever shown in the dialogue box.**

### 5.3 World Map Navigation — **[FUTURE / CUT for demo]**
- Full-game spec: illustrated pixel-art map, chapter nodes unlock sequentially, locked nodes stay visible but grayed out.
- Not needed with a single demo scene — skip entirely, or replace with a simple "Enter Demo" button on the main menu.

### 5.4 Cultural Codex — *Kodeks*
- Silently logs every Butuanon word, artifact, and cultural practice encountered.
- Entries appear **after a scene ends**, not as mid-gameplay pop-ups — don't interrupt play to add an entry.
- Words are shown with the **scene context** they were encountered in, never a direct translation.
- **[DEMO]** Build the panel and the logging pipeline; a handful of entries is enough to prove it works.

### 5.5 Artifact Collection & Examination
- Artifacts (clay jars, carved figurines, sail fragments, gold ornaments) are glowing interactive world objects.
- Tapping a discovered artifact opens an examine screen: Butuanon inscription + a short historical note.
- Full spec: artifacts also populate a growing gallery inside the Kodeks.
- **[DEMO]** Build one artifact + its examine screen to prove the interaction and the "log to Kodeks" flow. Skip the gallery UI until you're past the demo.

### 5.6 Environmental Interaction Sequences
- Specific scenes replace tap-to-talk with a puzzle-like sequence — e.g. the original spec's "tap eight stone markers in a culturally significant order."
- **No explicit instructions, no timer, no failure state.**
- **[DEMO]** This is worth building even at demo scope — it's the most mechanically distinct system and validates a non-dialogue interaction path. Don't tie it to "Chapter 3" specifically; treat it as a standalone interactable in the demo scene.

---

## 6. Game Loop

- **Micro loop** (per session): explore → NPC/environment interaction → Kodeks logs quietly in the background → objective progresses.
- **Macro loop** (full game): chapter-to-chapter narrative progression. **[FUTURE / CUT]** — irrelevant at demo scope since there's only one scene. The demo's "macro loop" is just: Main Menu → Demo Scene → Summary Screen → Main Menu.

---

## 7. Art & Asset Pipeline

**[DEMO — build to this spec exactly, it doesn't change with scope]**

- All assets hand-drawn in **Aseprite 1.3.15**.
- Environment tiles: **16×16 px**.
- Character sprites: **16×32 px** (16 wide, 32 tall — taller than one tile so characters read clearly against the top-down camera).
- Fixed **16-color palette**: jungle greens, river blues, warm ambers, skin tones.
- Export as **PNG with transparent background**.
- Import into Unity at **16 Pixels Per Unit**, **Point (no filter)** filter mode — this preserves crisp pixel edges. Do not let this default to Bilinear; it will blur the art.
- Camera: strictly **2D top-down**. Buildings are represented by rooftops.

### Scene Hierarchy Standard
Each scene should contain:
- 5 Tilemap layers: `Ground`, `Water`, `Objects`, `Collision`, `Overhead`
- An `Objects` parent for interactive sprites/prefabs
- An `NPCs` parent for character GameObjects
- A defined `Player Spawn Point`
- A `UI Canvas` containing the dialogue system + Kodeks panel

---

## 8. System Architecture — Demo Scope

The original spec calls for a **client-server model**: Unity APK (fully offline-playable) + a **PHP REST API + MySQL backend** for optional cloud save/progress sync, vocabulary records, quest state, and artifact logs.

**[DEMO] Build offline-only. Skip the backend entirely for now.**

Reasoning is in the flags section below — short version: the demo doesn't need cross-device sync, and building a PHP/MySQL API in parallel with the Unity client roughly doubles the surface area of what you're maintaining for something the demo doesn't require. Use Unity's local save (`PlayerPrefs` or a simple JSON file in persistent data path) to store:
- Encountered vocabulary/Kodeks entries
- Quest state
- Collected artifacts

If cloud sync becomes a real requirement later, it's a clean, isolated add-on — nothing in the demo's mechanics depends on it existing.

---

## 9. Core Interaction Flow (Sequence Logic)

This is the actual system wiring implied by the source document's sequence diagrams — written as logic, not UML:

**Gameplay exploration loop:**
`Player movement input → WorldManager detects nearby NPC/trigger → (if in range) DialogueHandler activates → ProgressTracker silently logs any Butuanon vocabulary encountered → next narrative/interaction segment unlocks`

**NPC dialogue flow:**
`Tap NPC → DialogueHandler loads contextual Butuanon-embedded dialogue → player picks a response → ProgressTracker logs the word/phrase encountered → (optional) a new quest activates`

**Quest flow:**
`Player views quest list via NPC → GameInterface pulls list → player accepts → InteractionHandler formally accepts, shows quest details → player performs actions (explore/recover/talk) → InteractionHandler continuously checks progress against ProgressTracker → on attempted completion: ProgressTracker verifies objectives →`
- if complete: mark finished, GameInterface unlocks rewards + shows success
- if incomplete: GameInterface shows an "incomplete" state and nudges the player to keep exploring (no punishment, no fail state)

Keep these four systems (`WorldManager`, `DialogueHandler`, `ProgressTracker`, `InteractionHandler`, `GameInterface`) as distinct, decoupled components — the source material's diagrams imply this separation and it's the right call for testability.

---

## 10. Kahanas sa Sinulti (Progress Tracking)

Full-game spec: a radar chart tracking contextual language acquisition across **five cultural-linguistic domains**.

**[DEMO]** You don't need five domains' worth of content to prove this system. Build the radar-chart UI and back it with real data from whatever the demo's vocabulary/Kodeks entries actually cover — even if that's only 1–2 domains represented, it proves the visualization and data pipeline work. Don't fabricate placeholder domains just to fill out five axes.

---

## 11. Functional Requirements (Demo-Scoped)

The system shall:
- Allow the player to start, pause, and exit the demo scene.
- Allow exploration of the single demo environment.
- Allow interaction with NPCs speaking naturally embedded Butuanon dialogue.
- Trigger contextual language exposure through at least one environmental element (signage, inscription, or chant).
- Allow the player to complete at least one quest.
- Silently log encountered vocabulary and artifacts to the Kodeks.
- Store/retrieve save data locally on-device (no backend required).

## 12. Non-Functional Requirements

- **Usability:** intuitive for a broad age range on first touch, no tutorial text walls.
- **Performance:** stable frame rate on mid-range Android (8.0+).
- **Reliability:** no crashes across a full demo playthrough loop.
- **Offline playability:** demo must be 100% playable with no network connection.
- **Cultural integrity:** Butuanon content should be reviewed for accuracy before any playtest, even at demo scale.

## 13. Testing Checklist

- [ ] Functional: chapter/scene entry works, dialogue triggers fire correctly, quest tracking updates, Kodeks logs correctly, artifact examine screen opens/closes cleanly.
- [ ] Performance: load time, frame rate, and touch responsiveness on at least one mid-range test device.
- [ ] Narrative/language: a Butuanon speaker or cultural resource person reviews dialogue accuracy — do this even at demo scale, before it compounds into later content.
- [ ] Playtest: run the full Main Menu → Demo → Summary → Menu loop start to finish with at least one outside player.

---

## 14. Flags & Recommendations

A few things in the source material are worth addressing before a build starts, rather than after:

1. **Backend server is scope creep for a demo.** The original spec pairs a fully-offline Unity client with a PHP + MySQL backend for "optional" sync features. For a demo whose entire purpose is proving mechanics, this doubles your tech stack (a second language, a second deployment target, a database schema) for a feature the demo doesn't need. **Recommendation:** build local-save-only now; treat the backend as a separate, later milestone if cloud sync is ever actually required.

2. **The document mixes research-methodology content with dev specs.** Sections on PENS/SUS surveys, pre/post-tests, and descriptive-range scoring tables are thesis-evaluation instruments, not application features. If you hand the original document to a dev-focused AI agent as-is, there's a real risk it tries to build survey UI or scoring logic that has no place in the game itself. This rewrite strips that out — keep it that way in whatever you feed to Antigravity.

3. **No explicit mobile display spec.** The document lists "Unity Input System" for touch but never states a target resolution, aspect ratio, or safe-area handling for notches/gesture bars. Worth nailing down before UI layout work starts, since Figma wireframes built without that in mind tend to need rework.

4. **Hardware note (minor, but flagging since it's cheap to fix):** the listed dev machine has 8GB RAM. Running Unity + URP + Aseprite simultaneously on 8GB is workable but can get tight, especially once the project grows past demo scope. Not a blocker, just worth knowing if the build starts feeling sluggish later.

Everything else in the source material — the six mechanics, the art pipeline, the scene hierarchy standard, the interaction-flow logic — is solid and internally consistent. No changes needed there beyond scoping it down to one scene.
