"""
unity_architecture_agent.py — Specialist agent for SugiHandi's Unity C# component architecture.

This agent designs the code architecture for the five core systems identified in the
dev reference's sequence diagrams:
  - WorldManager: spatial awareness, NPC/trigger proximity detection
  - DialogueHandler: branching dialogue loading, display, and choice routing
  - ProgressTracker: silent Butuanon vocabulary + quest objective logging
  - InteractionHandler: quest accept/complete, environmental puzzle sequence management
  - GameInterface: UI layer — dialogue box, Kodeks panel, artifact examine screen, summary screen

Also covers:
  - Local save strategy (PlayerPrefs vs JSON in persistent data path)
  - Scene management: Main Menu → Demo Scene → Summary Screen → Main Menu
  - Touch input architecture (Unity Input System)
  - DOTween integration for UI transitions
  - Kodeks logging pipeline (triggered silently, displayed post-scene)
  - Data flow between all systems

This replaces the monetization agent because SugiHandi is a thesis capstone demo —
there is no monetization, no IAP, no ads, no battle pass.
"""

import asyncio
import json
import os
from pathlib import Path

from dotenv import load_dotenv
from google.antigravity import Agent, LocalAgentConfig
from google.antigravity.types import TemplatedSystemInstructions

from schemas import UnityArchitecture
from utils import extract_json, get_default_retry_config, run_with_timeout_and_retry, save_raw_response
from validators import validate_demo_scope

load_dotenv(Path(__file__).parent / ".env")

GAME_CONTEXT = """
Game Title: SugiHandi
Engine: Unity (C#), Android Build Support, Universal Render Pipeline (URP) 2D
Input: Unity Input System (com.unity.inputsystem) — touch only, single tap
Text/UI: TextMeshPro (handles Butuanon glyph rendering)
Tweening: DOTween (UI panel transitions, dialogue box fade-in/out)
Camera: Cinemachine 2D
Tilemaps: Unity Tilemaps (5 layers)
Audio: Unity Audio Mixer
Version Control: Git / GitHub
Target: Android 8.0+, Dev OS: Windows 11

FIVE CORE SYSTEMS (from the dev reference sequence diagrams):
1. WorldManager — detects player proximity to NPCs and interactable objects
2. DialogueHandler — loads and presents branching Butuanon-embedded dialogue
3. ProgressTracker — silently logs encountered vocabulary and quest objective states
4. InteractionHandler — manages quest lifecycle and environmental puzzle sequences
5. GameInterface — UI controller for all panels: dialogue, Kodeks, artifact examine, summary

SCENE LOOP:
Main Menu → Demo Scene (the one scene) → Summary Screen → Main Menu

LOCAL SAVE (offline only — no backend):
Store: encountered Kodeks entries, quest state, collected artifacts
Method: decide between PlayerPrefs (simple but not file-based) or JSON in Application.persistentDataPath

DESIGN CONSTRAINTS:
- Systems must be decoupled — WorldManager does NOT directly call DialogueHandler, it fires events
- No MonoBehaviour singletons with tight coupling — use UnityEvents or a simple event bus
- ProgressTracker logs must never interrupt gameplay — always background/async-style
- DOTween handles ALL panel show/hide transitions (no coroutine Lerp spaghetti)
"""

IDENTITY = """
You are Bathala, the Unity Architect — a senior Unity C# game developer with 10+ years of
experience shipping clean, maintainable mobile game codebases.

You specialize in decoupled component architecture for narrative/exploration games:
event-driven communication between managers, ScriptableObject-based data, clean separation
between game logic and UI layers.

You know every relevant Unity API: Input System's InputAction, Tilemap Collider 2D,
Cinemachine CinemachineConfiner2D, DOTween's DOFade/DOAnchorPos, TextMeshPro's text assignment,
PlayerPrefs, Application.persistentDataPath, and JsonUtility/Newtonsoft.Json.

You write architecture documents that a junior-to-mid C# developer can implement directly —
clear responsibilities, specific class names, the exact Unity events/callbacks to use.

You MUST output ONLY a single valid JSON object — no markdown, no commentary, no code fences.
"""

SCHEMA_HINT = """
Output a JSON object with EXACTLY these keys:
{
  "architecture_overview": "string (1 paragraph: how the five systems relate and communicate)",
  "systems": [
    {
      "system_name": "string (e.g. 'WorldManager')",
      "class_name": "string (C# class name)",
      "monobehaviour": true,
      "responsibility": "string (what this system owns and is solely responsible for)",
      "key_fields": ["string (field name + type + purpose)"],
      "key_methods": ["string (method signature + what it does)"],
      "events_fired": ["string (UnityEvent or C# event name + when it fires)"],
      "events_listened_to": ["string (event name + from which system)"],
      "unity_components_required": ["string (what Unity components must be on the same or child GameObject)"],
      "notes": "string (any important implementation notes)"
    }
  ],
  "scene_management": {
    "scenes_list": ["string (scene names in the build)"],
    "main_menu_to_demo": "string (how scene transition works — SceneManager.LoadScene + DOTween fade)",
    "demo_to_summary": "string (what triggers this, how the summary screen receives data)",
    "summary_to_main_menu": "string (simple return flow)"
  },
  "local_save_design": {
    "chosen_method": "string ('PlayerPrefs' or 'JSON at persistentDataPath' — choose one)",
    "justification": "string (why this method fits the demo scope)",
    "save_data_schema": {
      "kodeks_entries": "string (how Kodeks entries are stored)",
      "quest_state": "string (how quest progress is stored)",
      "collected_artifacts": "string (how artifact collection is stored)"
    },
    "save_class_name": "string (the C# class responsible for save/load)",
    "save_trigger": "string (when does the game save?)"
  },
  "touch_input_architecture": {
    "unity_package": "com.unity.inputsystem",
    "tap_detection_approach": "string (how single tap is detected — InputAction or Pointer.press)",
    "raycast_approach": "string (how tap → world position → NPC/object detection works)",
    "move_trigger": "string (how tap on walkable ground triggers pathfinding/movement)",
    "interact_trigger": "string (how tap on NPC/object triggers interaction)"
  },
  "dotween_ui_patterns": [
    {
      "panel": "string (e.g. 'Dialogue Box')",
      "show_tween": "string (exact DOTween call, e.g. 'panel.DOFade(1f, 0.25f)')",
      "hide_tween": "string (exact DOTween call)",
      "notes": "string"
    }
  ],
  "dialogue_data_format": {
    "storage": "string (ScriptableObjects or JSON files — choose one and justify)",
    "structure_description": "string (how a dialogue tree node is structured)",
    "butuanon_embedding_approach": "string (how Butuanon words are embedded in dialogue strings without showing translation)"
  },
  "kodeks_pipeline": {
    "log_trigger": "string (what calls ProgressTracker to log a word/artifact)",
    "storage_during_session": "string (how logs accumulate in memory during play)",
    "display_timing": "string (when Kodeks panel is shown — post-scene on summary screen ONLY)",
    "panel_architecture": "string (how GameInterface shows the Kodeks panel)"
  },
  "data_flow_scenarios": [
    {
      "scenario": "string (e.g. 'Player taps NPC')",
      "step_by_step": ["string (each step in the call chain)"]
    }
  ],
  "folder_structure": {
    "scripts": ["string (folder path + what scripts go here)"],
    "data": ["string (ScriptableObjects, JSON data files)"],
    "scenes": ["string (scene files)"],
    "prefabs": ["string (prefab categories)"]
  }
}
"""

ARCHITECTURE_PROMPT = f"""
Given the following game context, generate the complete Unity C# architecture document
for SugiHandi's demo build.

GAME CONTEXT:
{GAME_CONTEXT}

Requirements:
- Define all 5 systems: WorldManager, DialogueHandler, ProgressTracker, InteractionHandler, GameInterface
- Each system must list specific C# class names, key fields with types, and key method signatures
- Events between systems must be named and typed — no vague "it communicates" descriptions
- Local save design must pick ONE method (PlayerPrefs OR JSON) and justify it for demo scope
- DOTween patterns must include actual DOTween API calls (DOFade, DOAnchorPos, etc.)
- Dialogue data format must choose ScriptableObjects or JSON files and justify the choice for a small dev team
- Data flow scenarios must cover at minimum: player taps NPC, player taps artifact, env puzzle completion, quest completion
- Folder structure must be practical for a Unity project on a small/student team
- Do NOT include any monetization, IAP, ads, analytics, or cloud sync concerns — this is a demo only

{SCHEMA_HINT}

Output ONLY the JSON object. No markdown. No explanation. No code fences.
"""


async def _execute_unity_architecture_agent() -> dict:
    """Core Unity architecture agent execution with native schema & fallback extraction."""
    config = LocalAgentConfig(
        system_instructions=TemplatedSystemInstructions(identity=IDENTITY),
        response_schema=UnityArchitecture,
        retry_config=get_default_retry_config(),
    )

    async with Agent(config) as agent:
        print("[Unity Architecture Agent] Generating C# component architecture and save design...")
        response = await agent.chat(ARCHITECTURE_PROMPT)

        structured = await response.structured_output()
        if structured:
            if isinstance(structured, dict):
                data = structured
            else:
                data = structured.model_dump() if hasattr(structured, "model_dump") else dict(structured)
            save_raw_response("unity_architecture", json.dumps(data, indent=2, ensure_ascii=False))
        else:
            text = await response.text()
            save_raw_response("unity_architecture", text)
            data = extract_json(text)

        validated = UnityArchitecture.model_validate(data)
        validated_dict = validated.model_dump()
        validate_demo_scope("unity_architecture", validated_dict)
        print("[Unity Architecture Agent] Complete")
        return validated_dict


async def run_unity_architecture_agent(timeout_seconds: float = 120.0, max_attempts: int = 2) -> dict:
    """Run the Unity architecture specialist agent with timeout, retry backoff, and schema validation."""
    return await run_with_timeout_and_retry(
        agent_name="Unity Architecture Agent",
        action=_execute_unity_architecture_agent,
        schema_cls=UnityArchitecture,
        timeout_seconds=timeout_seconds,
        max_attempts=max_attempts,
    )


if __name__ == "__main__":
    result = asyncio.run(run_unity_architecture_agent())
    print(json.dumps(result, indent=2, ensure_ascii=False))
