# SugiHandi — Unity Scripts Setup Guide

Place the contents of `unity/Assets/Scripts/` into your Unity project's `Assets/Scripts/` folder.

---

## Folder Structure

```
Assets/Scripts/
  Core/
    GameEvents.cs          ← Static event bus — all cross-system communication
    SaveData.cs            ← [Serializable] save data container
    SaveManager.cs         ← JSON save/load at persistentDataPath
    ProgressTracker.cs     ← Silently tracks Kodeks, quest, artifacts, puzzles
    WorldManager.cs        ← Player proximity detection and tap routing
  Data/
    DialogueData.cs        ← ScriptableObject: NPC dialogue tree with {wordKey} tags
    KodeksEntryData.cs     ← ScriptableObject: Butuanon word scene context entry
    QuestData.cs           ← ScriptableObject: quest steps and completion
    ArtifactData.cs        ← ScriptableObject: artifact inscription + historical note
  Gameplay/
    PlayerMovement.cs      ← Tap-to-move (Unity Input System, Rigidbody2D)
    NPCInteractable.cs     ← NPC component — registers with WorldManager
    ArtifactInteractable.cs← Artifact component — opens examine screen on tap
    DialogueHandler.cs     ← Parses {wordKey} tags, drives dialogue tree
    InteractionHandler.cs  ← Quest lifecycle: accept → steps → complete
    EnvironmentalPuzzle.cs ← Tap-marker sequence puzzle (no timer, no fail state)
    PuzzleMarker.cs        ← Individual marker in the puzzle sequence
  UI/
    GameInterface.cs       ← All panels: dialogue, quest, artifact, Kodeks, summary
```

---

## Required Unity Packages

Install via **Window → Package Manager**:

| Package | Why |
|---|---|
| **Input System** (`com.unity.inputsystem`) | Touch input for tap-to-move |
| **Cinemachine** | Camera follow + confiner for scene bounds |
| **TextMeshPro** | Dialogue text (handles Butuanon glyphs) |
| **Universal Render Pipeline (URP)** | 2D lighting for pixel art |
| **DOTween** (Asset Store — free) | UI panel fade transitions |

---

## Unity Project Settings

1. **Project Settings → Player → Other Settings:**
   - Scripting Backend: IL2CPP (for Android)
   - Target Architectures: ARM64
   - Minimum API Level: Android 8.0 (API 26)

2. **Project Settings → Player → Active Input Handling:** `Input System Package (New)`

3. **Import Pixel Art correctly:**
   - Select all sprites → Inspector:
     - Pixels Per Unit: `16`
     - Filter Mode: `Point (no filter)`
     - Compression: `None`
     - Sprite Mode: `Single` (tiles) or `Multiple` (character sheets)

---

## Scene Setup (Demo Scene)

### Hierarchy structure (mandatory from dev reference):

```
DemoScene
  ── Tilemaps
  │    ├── Ground   (Tilemap + TilemapRenderer, Order: 0)
  │    ├── Water    (Tilemap + TilemapRenderer, Order: 1)
  │    ├── Objects  (Tilemap + TilemapRenderer, Order: 2)
  │    ├── Collision (Tilemap + TilemapCollider2D + CompositeCollider2D, no renderer)
  │    └── Overhead (Tilemap + TilemapRenderer, Order: 10)
  ── Objects        (Empty parent for interactive prefabs)
  │    ├── Artifact_GoldBangle (ArtifactInteractable + CircleCollider2D)
  │    └── EnvironmentalPuzzle (EnvironmentalPuzzle + child PuzzleMarker GameObjects)
  ── NPCs           (Empty parent for NPC GameObjects)
  │    ├── NPC_LolaBulan (NPCInteractable + CircleCollider2D + SpriteRenderer)
  │    └── NPC_Datu (NPCInteractable + CircleCollider2D + SpriteRenderer)
  ── Player         (PlayerMovement + Rigidbody2D(Kinematic) + Collider2D)
  ── PlayerSpawnPoint (Empty Transform — mark the start position)
  ── Managers       (Empty parent for manager scripts)
  │    ├── WorldManager
  │    ├── DialogueHandler
  │    ├── InteractionHandler
  │    ├── ProgressTracker
  │    └── SaveManager
  ── UI Canvas      (Canvas, Screen Space - Camera)
       ├── DialoguePanel    (CanvasGroup)
       ├── QuestOfferPanel  (CanvasGroup)
       ├── QuestObjectiveHUD (CanvasGroup)
       ├── ArtifactExamine  (CanvasGroup)
       └── SummaryScreen    (CanvasGroup)
```

---

## ScriptableObject Assets to Create

Right-click in Project window to create each:

### Dialogue Data (`Create → SugiHandi → Dialogue Data`)
- One per NPC: `Dialogue_LolaBulan`, `Dialogue_Datu`, etc.
- Use `{wordKey}` to embed Butuanon words, e.g.:
  ```
  "The {Gabok} left its mark on everything here, Manaog."
  ```

### Kodeks Entries (`Create → SugiHandi → Kodeks Entry`)
- One per Butuanon word: `Kodeks_gabok`, `Kodeks_manaog`, `Kodeks_balay`
- **wordKey** must match the `{wordKey}` in dialogue exactly
- Place all in: `Assets/Resources/Kodeks/` (ProgressTracker loads from here)

### Quest Data (`Create → SugiHandi → Quest Data`)
- One quest for the demo: `Quest_DemoMain`

### Artifact Data (`Create → SugiHandi → Artifact Data`)
- One artifact: `Artifact_GoldBangle`

---

## How the Butuanon Word Pipeline Works

```
NPC Dialogue Text: "The {Gabok} changed everything."
         │
         ▼
  DialogueHandler.ParseAndFireBuruanonTags()
         │
         ├── Fires: GameEvents.RaiseBuruanonWordEncountered("Gabok")
         │             │
         │             ▼
         │       ProgressTracker.HandleWordEncountered("Gabok")
         │             │
         │             ├── Loads: Resources/Kodeks/Gabok.asset
         │             ├── Adds to: _saveData.loggedKodeksWordKeys
         │             └── Fires: GameEvents.RaiseKodeksEntryLogged(entry)
         │                        (silent — no UI shown)
         │
         └── Displays to player: "The Gabok changed everything."
                                    ↑ no translation, word shown naturally
```

---

## Running the Python Agent Pipeline

Before building, run the GDD generator to get your content spec:

```powershell
cd "c:\Users\Jhan Paul\Desktop\SugiHandi\agents"
pip install -r requirements.txt
python main.py
```

This outputs `agents/output/game_design_document.md` — use this as your
NPC dialogue script, scene layout guide, color palette, and architecture reference
while building in Unity.
