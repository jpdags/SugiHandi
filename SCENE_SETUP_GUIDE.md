# SugiHandi — Scene Setup Guide
# Unity Editor Instructions for Creating All Major Scenes

This guide walks through creating each `.unity` scene file and wiring it to the
SceneConfig system. Follow in order — MainMenu wires the SceneTransitionManager
that all other scenes depend on.

---

## Prerequisites (Do These First)

### 1. Add All Scenes to Build Settings
- File → Build Settings → Add Open Scenes
- Add in this order (index matters):
  0. Menu (already exists)
  1. RiverVillage
  2. TradePort
  3. SacredRuins
  4. ForestedHills
  5. SummaryScreen

### 2. Create Scene Config Assets
- In Project window: right-click `Assets/Resources/Scenes/`
  (create the folder first if it doesn't exist)
- For each scene: Create → SugiHandi → Scene Config
- Name and fill each one per the table below:

| Asset Name                    | sceneName      | displayName            | atmosphere | bgmClip                         | ambienceClip               |
|-------------------------------|----------------|------------------------|------------|---------------------------------|----------------------------|
| SceneConfig_MainMenu          | MainMenu       | Sug-i Handi            | Neutral    | (none)                          | (none)                     |
| SceneConfig_RiverVillage      | RiverVillage   | Suba sa Dakbayan       | Vibrant    | forest_river_spirits.mp3        | water_ambience.mp3         |
| SceneConfig_TradePort         | TradePort      | Hungasan na Pantalan   | Quiet      | waterbender_maritime.ogg        | water_ambience.mp3         |
| SceneConfig_SacredRuins       | SacredRuins    | Mga Bato nga Sagrado   | Sacred     | forest_river_spirits_loop.ogg   | forest_river_spirits_loop.ogg |
| SceneConfig_ForestedHills     | ForestedHills  | Kabukiran              | Forested   | forest_river_spirits.mp3        | forest_river_spirits_loop.ogg |
| SceneConfig_SummaryScreen     | SummaryScreen  | Kodeks                 | Neutral    | (none)                          | (none)                     |

---

## Scene 0: MainMenu (Menu.unity — already exists)

### Additional Setup Needed
1. Find (or create) the persistent manager GameObject.
2. Add `SceneTransitionManager` component to it.
3. Create a full-screen Canvas:
   - Canvas → Screen Space - Overlay, Sort Order = 999
   - Add child Image: stretch to fill, color = black, Alpha = 0
   - Add CanvasGroup to the Image
   - Assign the CanvasGroup to `SceneTransitionManager.fadeOverlayGroup`
4. Add two AudioSource components to the manager GameObject:
   - `bgmSource`: Audio Mixer Group = Music, Loop = true, Play On Awake = false
   - `ambienceSource`: Audio Mixer Group = Ambience, Loop = true, Play On Awake = false
   - Assign both to `SceneTransitionManager`
5. Ensure `MainMenuController.cs` uses `SceneTransitionManager.Instance.LoadScene("RiverVillage")`
   instead of any hardcoded SceneManager.LoadScene call.

---

## Scene 1: RiverVillage

### Narrative Context
- First gameplay scene after the menu.
- Warm, communal river settlement. The Manaog arrives by boat.
- Lola Tising is the anchor NPC — first conversation, first Butuanon words.
- First quest: find a missing trade item for a fisherman NPC.
- Artifact: a clay trade jar sitting near the docks.

### Create the Scene
1. File → New Scene → Save As `Assets/Scenes/RiverVillage.unity`

### Required Scene Hierarchy

```
RiverVillage (Scene Root)
├── _SceneInitializer          [SceneInitializer — assign SceneConfig_RiverVillage]
├── Tilemaps
│   ├── Ground                 [Tilemap + TilemapRenderer, order 0]
│   ├── Water                  [Tilemap + TilemapRenderer, order 1]
│   ├── Objects                [Tilemap + TilemapRenderer, order 2]
│   ├── Collision              [Tilemap + TilemapCollider2D + CompositeCollider2D, hidden]
│   └── Overhead               [Tilemap + TilemapRenderer, order 10]
├── Objects
│   └── ClayJar                [Sprite + ArtifactInteractable — assign ArtifactData_ClayJar]
├── NPCs
│   ├── LolaTising             [Sprite (16x32) + NPCInteractable — assign Dialogue_Elder_Greeting]
│   ├── Fisherman              [Sprite (16x32) + NPCInteractable — assign Dialogue_Fisherman_Quest]
│   └── Merchant               [Sprite (16x32) + NPCInteractable — assign Dialogue_Merchant_Trade]
├── PlayerSpawn                [Empty GameObject — Tag: "PlayerSpawn"]
├── Player                     [Sprite (16x32) + PlayerMovement + Tag: "Player"]
├── Managers
│   ├── WorldManager           [WorldManager — assign Player transform]
│   ├── DialogueHandler        [DialogueHandler]
│   ├── InteractionHandler     [InteractionHandler — assign quest data]
│   └── ProgressTracker        [ProgressTracker — already DontDestroyOnLoad]
├── Main Camera                [Camera + Cinemachine Brain]
│   └── CinemachineVirtualCamera [Follow = Player, Confiner bounds set to map]
└── UI Canvas                  [Canvas — Screen Space Camera]
    ├── DialoguePanel          [CanvasGroup + UI panels per GameInterface]
    ├── QuestOfferPanel        [CanvasGroup]
    ├── QuestObjectiveHUD      [CanvasGroup]
    ├── ArtifactExaminePanel   [CanvasGroup]
    └── SummaryScreen          [CanvasGroup — hidden at start]
```

### Tilemap Painting Notes
- Use the **Ground** layer for: river bank soil, wooden dock planks, packed earth paths
- Use the **Water** layer for: flowing river tiles (animated if using animated sprites)
- Use the **Objects** layer for: huts (rooftops only — top-down!), crates, market stalls, bamboo fences
- Use the **Overhead** layer for: tree canopy tiles that should appear above the player (z-order above player)
- Use the **Collision** layer for: invisible collision tiles on walls, water edges, building borders

### Atmosphere Checklist
- [x] SceneConfig_RiverVillage assigned to `_SceneInitializer`
- [x] Ambient color: warm amber (#F5DFA0) — set in SceneConfig_RiverVillage.ambientColor
- [x] BGM: forest_river_spirits.mp3
- [x] Ambience: water_ambience.mp3

---

## Scene 2: TradePort

### Narrative Context
- Abandoned dock area. Eerie quiet. Trade vessels long gone.
- Evidence of the Gabok's impact — damaged structures, scattered cargo.
- One NPC: a lone keeper/watchman who speaks mostly in Butuanon.
- Artifact: a woven sail fragment with an inscription.
- Environmental puzzle: Tap 5 scattered dock markers in the order of maritime ritual.

### Create the Scene
1. File → New Scene → Save As `Assets/Scenes/TradePort.unity`

### Required Scene Hierarchy

```
TradePort (Scene Root)
├── _SceneInitializer          [SceneInitializer — assign SceneConfig_TradePort]
├── Tilemaps
│   ├── Ground                 [Dock planks, stone pier tiles]
│   ├── Water                  [Open water/sea tiles]
│   ├── Objects                [Broken crates, ship wreckage, hanging nets]
│   ├── Collision              [Water edges, blocked ruins]
│   └── Overhead               [Damaged sail canopies, overhead rigging]
├── Objects
│   ├── WovenSailArtifact      [Sprite + ArtifactInteractable — assign ArtifactData_WovenSail]
│   └── DockMarkerPuzzle       [EnvironmentalPuzzle — 5 PuzzleMarker children]
│       ├── Marker_1           [PuzzleMarker, correct order = 1]
│       ├── Marker_2           [PuzzleMarker, correct order = 2]
│       ├── Marker_3           [PuzzleMarker, correct order = 3]
│       ├── Marker_4           [PuzzleMarker, correct order = 4]
│       └── Marker_5           [PuzzleMarker, correct order = 5]
├── NPCs
│   └── DockKeeper             [Sprite + NPCInteractable]
├── PlayerSpawn                [Tag: "PlayerSpawn" — at dock entrance]
├── Player                     [PlayerMovement + Tag: "Player"]
├── Managers
│   ├── WorldManager
│   ├── DialogueHandler
│   ├── InteractionHandler
│   └── ProgressTracker        [Already persistent]
├── Main Camera
└── UI Canvas                  [Same structure as RiverVillage]
```

### Atmosphere Checklist
- [x] Ambient color: muted dusk blue (#9BAFC4)
- [x] BGM: waterbender_maritime.ogg
- [x] Ambience: water_ambience.mp3

---

## Scene 3: SacredRuins

### Narrative Context
- Ancient stone structures on a forested hillside.
- Anito spirit appears here — introduced through environmental storytelling (inscriptions, glowing stones).
- Puzzle: tap 8 stone markers in culturally correct order (ritual sequence).
- No combat, no fail state. Just discovery.
- Artifact: a gold ornament with an inscription referencing an ancestor's name.

### Create the Scene
1. File → New Scene → Save As `Assets/Scenes/SacredRuins.unity`

### Required Scene Hierarchy

```
SacredRuins (Scene Root)
├── _SceneInitializer          [SceneInitializer — assign SceneConfig_SacredRuins]
├── Tilemaps
│   ├── Ground                 [Stone tiles, mossy earth, carved floor panels]
│   ├── Water                  [(optional) small spring or reflecting pool]
│   ├── Objects                [Crumbling walls, altar stones, vine-covered columns]
│   ├── Collision              [Impassable ruins]
│   └── Overhead               [Tree canopy overhead layer]
├── Objects
│   ├── GoldOrnamentArtifact   [Sprite + ArtifactInteractable — assign ArtifactData_GoldOrnament]
│   └── StoneMarkerPuzzle      [EnvironmentalPuzzle — 8 PuzzleMarker children]
│       ├── StoneMarker_1      [PuzzleMarker]
│       ├── StoneMarker_2      [PuzzleMarker]
│       ├── StoneMarker_3      [PuzzleMarker]
│       ├── StoneMarker_4      [PuzzleMarker]
│       ├── StoneMarker_5      [PuzzleMarker]
│       ├── StoneMarker_6      [PuzzleMarker]
│       ├── StoneMarker_7      [PuzzleMarker]
│       └── StoneMarker_8      [PuzzleMarker]
├── NPCs
│   └── AnitoSpirit            [Sprite (ethereal/glowing) + NPCInteractable]
├── PlayerSpawn                [Tag: "PlayerSpawn"]
├── Player                     [PlayerMovement + Tag: "Player"]
├── Managers ...
└── UI Canvas ...
```

### Atmosphere Checklist
- [x] Ambient color: cool blue-green (#7EB8A0)
- [x] BGM: forest_river_spirits_loop.ogg
- [x] Ambience: forest_river_spirits_loop.ogg (same — layered)

---

## Scene 4: ForestedHills

### Narrative Context
- Transition zone between the settlement and sacred summit.
- Quiet discovery scene — no quest, no puzzle. Pure environmental storytelling.
- Carved markers along the path name landmarks in Butuanon.
- Two travelling NPCs that speak in passing — brief branching dialogue only.

### Create the Scene
1. File → New Scene → Save As `Assets/Scenes/ForestedHills.unity`

### Required Scene Hierarchy

```
ForestedHills (Scene Root)
├── _SceneInitializer          [SceneInitializer — assign SceneConfig_ForestedHills]
├── Tilemaps
│   ├── Ground                 [Dirt path, grass tiles, root-covered earth]
│   ├── Water                  [(optional) shallow stream crossing]
│   ├── Objects                [Tree trunks, carved waymarkers, fallen logs]
│   ├── Collision              [Dense forest blocking off-path movement]
│   └── Overhead               [Heavy forest canopy]
├── Objects
│   ├── WayMarker_1            [Sprite + ArtifactInteractable — Butuanon place name inscriptions]
│   ├── WayMarker_2            [Sprite + ArtifactInteractable]
│   └── WayMarker_3            [Sprite + ArtifactInteractable]
├── NPCs
│   ├── Traveller_A            [Sprite + NPCInteractable — brief passing dialogue]
│   └── Traveller_B            [Sprite + NPCInteractable]
├── PlayerSpawn                [Tag: "PlayerSpawn" — at forest entrance]
├── Player
├── Managers ...
└── UI Canvas ...
```

### Atmosphere Checklist
- [x] Ambient color: rich green (#84BB74)
- [x] BGM: forest_river_spirits.mp3
- [x] Ambience: forest_river_spirits_loop.ogg

---

## Scene 5: SummaryScreen

### Narrative Context
- Post-gameplay. Not a world scene — a UI-only screen.
- Shows the Kodeks (words the player encountered) in a scroll-like layout.
- "Return to Menu" button returns to MainMenu via SceneTransitionManager.

### Create the Scene
1. File → New Scene → Save As `Assets/Scenes/SummaryScreen.unity`

### Required Scene Hierarchy

```
SummaryScreen (Scene Root)
├── _SceneInitializer          [SceneInitializer — assign SceneConfig_SummaryScreen]
├── Main Camera
└── UI Canvas                  [Full-screen, Screen Space Overlay]
    ├── Background             [Dark gradient or parchment texture Image]
    ├── TitleText              [TMP "Kodeks — Mga Pulong nga Imong Nakit-an"]
    ├── KodeksScrollView       [ScrollView containing KodeksEntriesContainer]
    │   └── KodeksEntries      [Vertical Layout Group — rows populated by GameInterface]
    ├── ReturnButton           [Button → wired to GameInterface.summaryReturnMenuButton]
    └── GameInterface          [GameInterface component — attach here in SummaryScreen]
```

### Note
The `GameInterface` in gameplay scenes handles the in-game UI panels. In `SummaryScreen`,
a **separate** `GameInterface` instance handles only the summary display and Kodeks population.
The `ProgressTracker` (DontDestroyOnLoad) provides the Kodeks data automatically.

---

## Scene Transition Wiring

After creating all scenes, verify transitions work:

| From          | Trigger                       | To             |
|---------------|-------------------------------|----------------|
| MainMenu      | "Start" button                | RiverVillage   |
| RiverVillage  | Quest complete → portal/exit  | TradePort      |
| TradePort     | Puzzle complete → exit        | SacredRuins    |
| SacredRuins   | Anito encounter complete      | ForestedHills  |
| ForestedHills | Reach scene exit              | SummaryScreen  |
| SummaryScreen | "Return" button               | MainMenu       |

To trigger a transition from any script:
```csharp
// Option A: Direct call
SceneTransitionManager.Instance.LoadScene("TradePort");

// Option B: Via event bus (preferred for decoupling)
GameEvents.RaiseSceneTransitionRequested("TradePort");
```

---

## Audio Mixer Setup

In the Unity Audio Mixer window (Window → Audio → Audio Mixer):
1. Create a new Audio Mixer named `SugiHandi`
2. Add three groups under Master:
   - `Music` — connect to bgmSource on SceneTransitionManager
   - `Ambience` — connect to ambienceSource on SceneTransitionManager
   - `SFX` — connect to any one-shot AudioSource for UI/interaction sounds
3. Add a **Lowpass** filter on the Music group for future "muffled" effect when dialogue is open
