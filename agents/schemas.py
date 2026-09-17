"""
schemas.py — Pydantic output schemas for all SugiHandi specialist agents.

Each schema maps to the structured JSON output produced by one agent.
The GameDesignDocument is the master schema aggregating all five agent outputs.

Agents:
  1. NarrativeDesign      ← narrative_agent.py    (Butuanon NPCs, dialogue, Kodeks, quest)
  2. SystemsDesign        ← systems_agent.py       (six core mechanics spec for Unity demo)
  3. LevelDesign          ← level_design_agent.py  (single demo scene layout)
  4. ArtDirection         ← art_director_agent.py  (pixel art spec, 16-color palette, Aseprite pipeline)
  5. UnityArchitecture    ← unity_architecture_agent.py (C# component design, save, scene loop)
"""

from pydantic import BaseModel, Field
from typing import Optional


# ─────────────────────────────────────────────
# NARRATIVE AGENT SCHEMA
# ─────────────────────────────────────────────

class PlayerOption(BaseModel):
    option_text: str = Field(description="What the player can choose to say or do")
    npc_response: str = Field(description="NPC's response — Butuanon words embedded naturally, no translation shown")
    narrative_consequence: str = Field(description="What changes in the world/quest/Kodeks as a result of this choice")
    kodeks_entry_triggered: Optional[str] = Field(description="Kodeks entry logged silently, or null")

class DialogueBranch(BaseModel):
    trigger: str = Field(description="What the player does to start this dialogue branch")
    npc_opening_line: str = Field(description="NPC's opening line — Butuanon words embedded naturally")
    player_options: list[PlayerOption] = Field(description="2–3 player response options")

class NPC(BaseModel):
    name: str = Field(description="NPC name — grounded in Butuan naming conventions")
    role: str = Field(description="Role in the demo scene, e.g. 'Dock Elder', 'Fisherman'")
    personality: str = Field(description="Personality description (2-3 sentences)")
    butuanon_words_they_use: list[str] = Field(description="Butuanon words this NPC uses, with natural context")
    dialogue_branches: list[DialogueBranch] = Field(description="At least 2 dialogue branches with player options")

class QuestStep(BaseModel):
    step_number: int = Field(description="Step index (1-based)")
    objective: str = Field(description="What the player needs to do")
    completion_trigger: str = Field(description="What action completes this step")
    dialogue_on_completion: str = Field(description="What an NPC says or what text appears on completion")

class QuestChain(BaseModel):
    title: str = Field(description="Quest title")
    quest_giver_npc: str = Field(description="Name of the NPC who gives the quest")
    synopsis: str = Field(description="Brief synopsis — demo scope only (2-3 sentences)")
    steps: list[QuestStep] = Field(description="Sequential quest steps")
    reward: str = Field(description="Narrative/relational reward — no coins or points")
    butuanon_words_encountered_in_quest: list[str] = Field(description="Butuanon vocabulary the player encounters through this quest")

class EnvironmentalText(BaseModel):
    location: str = Field(description="Where in the scene this text appears")
    text_content: str = Field(description="The Butuanon inscription or signage text")
    cultural_note_for_kodeks: str = Field(description="What the Kodeks logs — shown post-scene, not a translation")

class KodeksEntry(BaseModel):
    word_or_phrase: str = Field(description="Butuanon word or phrase")
    scene_context: str = Field(description="Scene context it was encountered in — NOT a direct translation")
    cultural_significance: str = Field(description="Cultural significance of this word/phrase in Butuan context")

class LoreNote(BaseModel):
    title: str = Field(description="Lore note title")
    content: str = Field(description="2-3 sentences of Butuan history/culture relevant to the demo")

class NarrativeDesign(BaseModel):
    game_title: str = Field(description="SugiHandi")
    tagline: str = Field(description="A one-line tagline")
    setting_note: str = Field(description="1-2 sentences grounding the demo scene in Butuan")
    npcs: list[NPC] = Field(description="2–3 NPCs for the demo scene")
    quest_chain: QuestChain = Field(description="The single quest chain for the demo")
    environmental_texts: list[EnvironmentalText] = Field(description="2-3 environmental inscriptions/signage")
    kodeks_seed_entries: list[KodeksEntry] = Field(description="3-5 Kodeks entries seeded into the demo")
    lore_notes: list[LoreNote] = Field(description="Lore notes grounding the scene in Butuan history")


# ─────────────────────────────────────────────
# SYSTEMS AGENT SCHEMA
# ─────────────────────────────────────────────

class TapToMoveSpec(BaseModel):
    description: str = Field(description="How tap-to-move works")
    unity_implementation_notes: str = Field(description="Unity-specific implementation notes")
    pathfinding_approach: str = Field(description="Pathfinding method chosen")
    interact_trigger_radius: str = Field(description="Radius at which tap triggers interaction")

class BranchingDialogueSpec(BaseModel):
    description: str = Field(description="How the branching dialogue system works")
    options_per_node: str = Field(description="Always 2-3 options per node")
    butuanon_embedding_rule: str = Field(description="How Butuanon words are embedded")
    no_translation_rule: str = Field(description="The hard rule: no translation ever shown")
    unity_implementation_notes: str = Field(description="Unity-specific notes")
    dialogue_data_format: str = Field(description="How dialogue trees are stored")

class KodeksSpec(BaseModel):
    description: str = Field(description="What the Kodeks is and how it works")
    logging_trigger: str = Field(description="What causes a Kodeks entry to be logged")
    when_shown_to_player: str = Field(description="ONLY after scene ends — never mid-gameplay")
    entry_format: str = Field(description="Shows scene context, not a translation")
    unity_implementation_notes: str = Field(description="Unity-specific notes")
    local_save_key: str = Field(description="The save key for Kodeks data")

class ArtifactExaminationSpec(BaseModel):
    description: str = Field(description="How artifact examination works")
    demo_scope: str = Field(description="One artifact for the demo")
    examine_screen_content: str = Field(description="What appears on the examine screen")
    kodeks_log_on_examine: str = Field(description="What the Kodeks logs when artifact is examined")
    unity_implementation_notes: str = Field(description="Unity-specific notes")

class EnvironmentalInteractionSpec(BaseModel):
    description: str = Field(description="How environmental interaction sequences work")
    demo_implementation: str = Field(description="The tap-markers-in-order pattern")
    no_instructions_rule: str = Field(description="No explicit instructions shown to player")
    no_timer_rule: str = Field(description="No timer on the puzzle")
    no_fail_state_rule: str = Field(description="No fail condition")
    completion_trigger: str = Field(description="What triggers sequence completion")
    unity_implementation_notes: str = Field(description="Unity-specific notes")

class QuestSystemSpec(BaseModel):
    description: str = Field(description="How the quest system works")
    demo_quest_flow: str = Field(description="accept → objective → complete → reward flow")
    progress_check_logic: str = Field(description="How ProgressTracker checks objective completion")
    incomplete_state_handling: str = Field(description="Nudge player — no punishment, no fail state")
    unity_implementation_notes: str = Field(description="Unity-specific notes")
    local_save_key: str = Field(description="The save key for quest state")

class LocalSaveSpec(BaseModel):
    description: str = Field(description="Local save strategy")
    save_method: str = Field(description="PlayerPrefs or JSON at persistentDataPath")
    data_saved: list[str] = Field(description="What data is saved")
    save_triggers: str = Field(description="When the game saves")

class SystemCommunicationFlow(BaseModel):
    scenario: str = Field(description="e.g. 'Player taps NPC'")
    flow: str = Field(description="The call chain between systems")

class SystemsDesign(BaseModel):
    game_loop_overview: str = Field(description="Main Menu → Demo Scene → Summary Screen → Main Menu loop")
    tap_to_move_spec: TapToMoveSpec
    branching_dialogue_spec: BranchingDialogueSpec
    kodeks_spec: KodeksSpec
    artifact_examination_spec: ArtifactExaminationSpec
    environmental_interaction_spec: EnvironmentalInteractionSpec
    quest_system_spec: QuestSystemSpec
    local_save_spec: LocalSaveSpec
    system_communication_flow: list[SystemCommunicationFlow] = Field(description="At least 4 data flow scenarios")
    touch_input_notes: str = Field(description="Unity Input System touch handling for single-tap")


# ─────────────────────────────────────────────
# LEVEL DESIGN AGENT SCHEMA
# ─────────────────────────────────────────────

class TilemapLayers(BaseModel):
    ground: str = Field(description="Ground tile description")
    water: str = Field(description="Water tile placement")
    objects: str = Field(description="Object tiles and prefab locations")
    collision: str = Field(description="Collision areas")
    overhead: str = Field(description="Overhead tiles that render above player")

class SpawnPoint(BaseModel):
    description: str = Field(description="Where Manaog starts and why")
    tile_coordinates: str = Field(description="Approximate tile grid position")

class NPCPlacement(BaseModel):
    name: str = Field(description="NPC name")
    position_description: str = Field(description="Where in the scene they stand and why")
    tile_coordinates: str = Field(description="Approximate tile grid position")
    interaction_radius_tiles: float = Field(description="Interaction trigger radius in tiles")
    facing_direction: str = Field(description="Initial facing direction: Down|Up|Left|Right")

class InteractiveObject(BaseModel):
    type: str = Field(description="Artifact|EnvPuzzleMarker|Signage|Door")
    name: str = Field(description="Object name")
    position_description: str = Field(description="Where it is and its visual appearance")
    tile_coordinates: str = Field(description="Approximate tile grid position")
    interaction_notes: str = Field(description="What happens when tapped")
    layer: str = Field(description="Which Tilemap layer or parent it belongs to")

class EnvironmentalPuzzle(BaseModel):
    description: str = Field(description="The tap-markers-in-order puzzle description")
    marker_positions: list[str] = Field(description="Position description for each marker")
    correct_order_rationale: str = Field(description="Cultural/narrative reason for this order — not shown to player")
    completion_effect: str = Field(description="What happens in scene when sequence completes")

class LevelDesign(BaseModel):
    scene_name: str = Field(description="Demo scene name — grounded in Butuan place names")
    scene_description: str = Field(description="What this place is, its atmosphere, its role in Butuan (2-3 sentences)")
    scene_dimensions: str = Field(description="Approximate tile dimensions")
    tilemap_layers: TilemapLayers
    player_spawn_point: SpawnPoint
    npcs: list[NPCPlacement] = Field(description="2–3 NPC placements")
    interactive_objects: list[InteractiveObject] = Field(description="Artifact, puzzle markers, signage")
    environmental_puzzle: EnvironmentalPuzzle
    environmental_storytelling_notes: str = Field(description="How layout communicates Butuan history without dialogue")
    visual_reference_notes: str = Field(description="Notes for the pixel artist on what each area should evoke")
    unity_scene_setup_notes: str = Field(description="Sorting layers, Cinemachine confiner, Collision layer setup")


# ─────────────────────────────────────────────
# ART DIRECTOR AGENT SCHEMA
# ─────────────────────────────────────────────

class PixelArtSpecs(BaseModel):
    tool: str = Field(description="Aseprite 1.3.15")
    tile_size_px: str = Field(description="16x16")
    character_size_px: str = Field(description="16x32")
    palette_size: int = Field(description="16")
    export_format: str = Field(description="PNG, transparent background")
    unity_ppu: int = Field(description="16")
    unity_filter_mode: str = Field(description="Point (no filter)")
    camera_perspective: str = Field(description="Strictly 2D top-down")

class PaletteColor(BaseModel):
    index: int = Field(description="Color index 0-15")
    hex: str = Field(description="Hex color code")
    name: str = Field(description="Descriptive color name")
    usage: str = Field(description="What this color is used for in the scene")

class PaletteGroups(BaseModel):
    jungle_greens: list[str] = Field(description="Hex codes for jungle green tones")
    river_blues: list[str] = Field(description="Hex codes for river/water blue tones")
    warm_ambers: list[str] = Field(description="Hex codes for warm amber/earth tones")
    skin_tones: list[str] = Field(description="Hex codes for skin tones")
    neutrals_and_outlines: list[str] = Field(description="Hex codes for neutrals and outline black")

class AnimationFrames(BaseModel):
    idle: int = Field(description="Frames for idle animation")
    walk: int = Field(description="Frames for walk animation")
    interact: int = Field(description="Frames for interact animation")

class CharacterSpriteGuide(BaseModel):
    manaog_player: str = Field(description="Visual description of the Manaog player character")
    npc_design_principles: str = Field(description="How NPCs look distinct while fitting Butuan culture")
    animation_frames_per_action: AnimationFrames
    cultural_clothing_notes: str = Field(description="Accurate pre-colonial Butuan clothing details")

class TilemapVisualGuide(BaseModel):
    ground_tiles: str = Field(description="Ground tile visual description and palette colors used")
    water_tiles: str = Field(description="Water tile visual description and animation approach")
    object_tiles: str = Field(description="Object tile visual description")
    overhead_tiles: str = Field(description="Overhead tile visual description and layering")
    collision_tiles: str = Field(description="Invisible collision tiles note")

class ArtDirection(BaseModel):
    art_style_statement: str = Field(description="Clear 1-sentence statement: pixel art in Aseprite, not painterly")
    pixel_art_specs: PixelArtSpecs
    color_palette_16: list[PaletteColor] = Field(description="All 16 colors with hex codes, names, and usage")
    palette_groups: PaletteGroups
    character_sprite_guide: CharacterSpriteGuide
    tilemap_visual_guide: TilemapVisualGuide
    ui_aesthetic: str = Field(description="Pixel art UI frame style — carved wood/woven cloth feel")
    unity_lighting_notes: str = Field(description="URP 2D lighting settings and approach")
    cultural_accuracy_notes: str = Field(description="Butuan-specific accuracy notes and what NOT to do")
    visual_references: list[str] = Field(description="Specific pixel art games and Butuan material culture references")
    aseprite_workflow_notes: str = Field(description="Indexed color mode, palette locking, export settings")


# ─────────────────────────────────────────────
# UNITY ARCHITECTURE AGENT SCHEMA
# ─────────────────────────────────────────────

class UnitySystem(BaseModel):
    system_name: str = Field(description="Human-readable system name")
    class_name: str = Field(description="C# class name")
    monobehaviour: bool = Field(description="Whether this is a MonoBehaviour")
    responsibility: str = Field(description="Single-responsibility description")
    key_fields: list[str] = Field(description="Field name + type + purpose")
    key_methods: list[str] = Field(description="Method signature + what it does")
    events_fired: list[str] = Field(description="Events/UnityEvents this system fires")
    events_listened_to: list[str] = Field(description="Events this system listens to and from which system")
    unity_components_required: list[str] = Field(description="Required Unity components")
    notes: str = Field(description="Important implementation notes")

class SceneManagement(BaseModel):
    scenes_list: list[str] = Field(description="All scene names in the build")
    main_menu_to_demo: str = Field(description="Transition from main menu to demo scene")
    demo_to_summary: str = Field(description="Transition from demo to summary screen")
    summary_to_main_menu: str = Field(description="Return from summary to main menu")

class SaveDataSchema(BaseModel):
    kodeks_entries: str = Field(description="How Kodeks entries are stored")
    quest_state: str = Field(description="How quest progress is stored")
    collected_artifacts: str = Field(description="How artifact collection is stored")

class LocalSaveDesign(BaseModel):
    chosen_method: str = Field(description="PlayerPrefs or JSON at persistentDataPath")
    justification: str = Field(description="Why this method fits the demo scope")
    save_data_schema: SaveDataSchema
    save_class_name: str = Field(description="The C# class responsible for save/load")
    save_trigger: str = Field(description="When the game saves")

class TouchInputArchitecture(BaseModel):
    unity_package: str = Field(description="com.unity.inputsystem")
    tap_detection_approach: str = Field(description="How a single tap is detected")
    raycast_approach: str = Field(description="How tap position maps to world objects")
    move_trigger: str = Field(description="How tap on ground triggers movement")
    interact_trigger: str = Field(description="How tap on NPC/object triggers interaction")

class DOTweenPattern(BaseModel):
    panel: str = Field(description="UI panel name")
    show_tween: str = Field(description="DOTween call to show the panel")
    hide_tween: str = Field(description="DOTween call to hide the panel")
    notes: str = Field(description="Implementation notes")

class DialogueDataFormat(BaseModel):
    storage: str = Field(description="ScriptableObjects or JSON files")
    structure_description: str = Field(description="How a dialogue tree node is structured")
    butuanon_embedding_approach: str = Field(description="How Butuanon words are embedded without showing translation")

class KodeksPipeline(BaseModel):
    log_trigger: str = Field(description="What calls ProgressTracker to log")
    storage_during_session: str = Field(description="How logs accumulate in memory")
    display_timing: str = Field(description="When Kodeks panel is shown — post-scene ONLY")
    panel_architecture: str = Field(description="How GameInterface shows the Kodeks panel")

class DataFlowScenario(BaseModel):
    scenario: str = Field(description="Scenario name e.g. 'Player taps NPC'")
    step_by_step: list[str] = Field(description="Each step in the call chain between systems")

class FolderStructure(BaseModel):
    scripts: list[str] = Field(description="Script folder paths and contents")
    data: list[str] = Field(description="ScriptableObjects and data files")
    scenes: list[str] = Field(description="Scene file locations")
    prefabs: list[str] = Field(description="Prefab categories")

class UnityArchitecture(BaseModel):
    architecture_overview: str = Field(description="How the five systems relate and communicate (1 paragraph)")
    systems: list[UnitySystem] = Field(description="All 5 systems: WorldManager, DialogueHandler, ProgressTracker, InteractionHandler, GameInterface")
    scene_management: SceneManagement
    local_save_design: LocalSaveDesign
    touch_input_architecture: TouchInputArchitecture
    dotween_ui_patterns: list[DOTweenPattern] = Field(description="DOTween patterns for each UI panel")
    dialogue_data_format: DialogueDataFormat
    kodeks_pipeline: KodeksPipeline
    data_flow_scenarios: list[DataFlowScenario] = Field(description="At least 4 data flow scenarios")
    folder_structure: FolderStructure


# ─────────────────────────────────────────────
# MASTER GAME DESIGN DOCUMENT
# ─────────────────────────────────────────────

class GameDesignDocument(BaseModel):
    generated_at: str = Field(description="ISO timestamp of generation")
    narrative: NarrativeDesign
    systems: SystemsDesign
    level_design: LevelDesign
    art_direction: ArtDirection
    unity_architecture: UnityArchitecture
