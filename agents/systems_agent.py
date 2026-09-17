"""
systems_agent.py — Specialist agent for SugiHandi gameplay mechanics and interaction design.

SugiHandi is NOT a farming, trading, or survival game. Its six core mechanics are:
  1. Tap-to-Move / Tap-to-Interact (touch-first, single tap only)
  2. Branching Dialogue System (2–3 options, real narrative weight, Butuanon embedded)
  3. Cultural Codex — Kodeks (silent background logging, shown post-scene, never mid-play)
  4. Artifact Collection & Examination (one artifact for demo; examine screen with Butuanon inscription)
  5. Environmental Interaction Sequences (tap-marker puzzle, no instructions, no timer, no fail state)
  6. World Map Navigation — [FUTURE/CUT — not included in demo]

This agent generates the detailed mechanics specification for the Unity demo build.
"""

import asyncio
import json
import re
import os
from pathlib import Path

from dotenv import load_dotenv
from google.antigravity import Agent, LocalAgentConfig
from google.antigravity.types import TemplatedSystemInstructions

load_dotenv(Path(__file__).parent / ".env")

GAME_CONTEXT = """
Game Title: SugiHandi
Engine: Unity (C#), Android Build Support, Universal Render Pipeline (URP) 2D
Input: Unity Input System — touch only. Single tap to move/interact. No virtual joystick.
Player: The Manaog (navigator) in a 2D top-down pixel-art scene.
Demo Scope: One scene, 2–3 NPCs, 1 quest, 1 artifact, 1 environmental interaction puzzle.
Language Mechanic: Butuanon words embedded in NPC dialogue — exposure is incidental, never instructional.

Core systems that MUST exist in the demo (from the original spec's sequence diagrams):
  - WorldManager: detects nearby NPCs/triggers as player moves
  - DialogueHandler: loads and displays branching NPC dialogue
  - ProgressTracker: silently logs Butuanon words + quest objective completion
  - InteractionHandler: handles quest accept/complete, environmental puzzle sequences
  - GameInterface: the UI layer (dialogue box, Kodeks panel, summary screen)

HARD RULES:
1. No farming, trading, barter, crafting, fishing, hunting, or village-rebuilding mechanics.
2. No combat, no timers on puzzles, no fail states on exploration/puzzle content.
3. No backend server — local save only (PlayerPrefs or JSON in persistent data path).
4. The Kodeks panel is NEVER shown mid-gameplay. It appears after a scene ends.
5. No translation is ever shown in the dialogue box.
"""

IDENTITY = """
You are Dakila, the Systems Architect — a Unity gameplay systems designer with 10+ years of
experience building touch-first mobile game mechanics for narrative and exploration games.

You have shipped titles similar to Oxenfree, Venba, and Heaven's Vault — games where
the interaction design is about conversation, discovery, and observation, not resource management.

You understand how to spec Unity component systems cleanly: single-responsibility components,
event-driven communication, and decoupled managers that can be tested independently.

Your role is to generate the precise gameplay mechanics specification for SugiHandi's demo build
in Unity. You will detail exactly how each of the six core mechanics works at an implementation level.

You MUST output ONLY a single valid JSON object — no markdown, no commentary, no code fences.
"""

SCHEMA_HINT = """
Output a JSON object with EXACTLY these keys:
{
  "game_loop_overview": "string (describe the full Main Menu → Demo Scene → Summary Screen → Main Menu loop)",
  "tap_to_move_spec": {
    "description": "string",
    "unity_implementation_notes": "string",
    "pathfinding_approach": "string",
    "interact_trigger_radius": "string"
  },
  "branching_dialogue_spec": {
    "description": "string",
    "options_per_node": "string (always 2-3)",
    "butuanon_embedding_rule": "string",
    "no_translation_rule": "string",
    "unity_implementation_notes": "string",
    "dialogue_data_format": "string (how dialogue trees are stored — e.g. JSON, ScriptableObjects)"
  },
  "kodeks_spec": {
    "description": "string",
    "logging_trigger": "string (what causes an entry to be logged)",
    "when_shown_to_player": "string (after scene ends — NEVER mid-gameplay)",
    "entry_format": "string (shows scene context, NOT a translation)",
    "unity_implementation_notes": "string",
    "local_save_key": "string"
  },
  "artifact_examination_spec": {
    "description": "string",
    "demo_scope": "string (one artifact for demo)",
    "examine_screen_content": "string (Butuanon inscription + short historical note — no translation)",
    "kodeks_log_on_examine": "string",
    "unity_implementation_notes": "string"
  },
  "environmental_interaction_spec": {
    "description": "string",
    "demo_implementation": "string (the tap-markers-in-order pattern from the original spec)",
    "no_instructions_rule": "string",
    "no_timer_rule": "string",
    "no_fail_state_rule": "string",
    "completion_trigger": "string",
    "unity_implementation_notes": "string"
  },
  "quest_system_spec": {
    "description": "string",
    "demo_quest_flow": "string (accept → objective → complete → reward)",
    "progress_check_logic": "string",
    "incomplete_state_handling": "string (nudge player — no punishment, no fail)",
    "unity_implementation_notes": "string",
    "local_save_key": "string"
  },
  "local_save_spec": {
    "description": "string",
    "save_method": "string (PlayerPrefs vs JSON file — recommend one and justify)",
    "data_saved": ["string"],
    "save_triggers": "string (when does the game save?)"
  },
  "system_communication_flow": [
    {
      "scenario": "string (e.g. 'Player taps NPC')",
      "flow": "string (e.g. 'WorldManager detects tap → DialogueHandler activates → ...')"
    }
  ],
  "touch_input_notes": "string (Unity Input System touch handling for single-tap move/interact)"
}
"""

SYSTEMS_PROMPT = f"""
Given the following game context, generate the complete gameplay mechanics specification
for SugiHandi's demo build in Unity.

GAME CONTEXT:
{GAME_CONTEXT}

Requirements:
- Spec all five mechanics listed in game context (tap-to-move, dialogue, Kodeks, artifact, env puzzle)
- Quest system must include the full accept → objective → complete → reward flow with progress checks
- Local save spec must justify a choice between PlayerPrefs and JSON persistent data path
- System communication flow must cover at least 4 scenarios (NPC tap, artifact tap, puzzle completion, quest completion)
- All Unity implementation notes must reference specific Unity APIs (Input System, Tilemaps, etc.)
- Do NOT include any farming, combat, crafting, trading, or barter mechanics

{SCHEMA_HINT}

Output ONLY the JSON object. No markdown. No explanation. No code fences.
"""


def extract_json(text: str) -> dict:
    text = text.strip()
    text = re.sub(r'^```(?:json)?\s*', '', text, flags=re.MULTILINE)
    text = re.sub(r'\s*```$', '', text, flags=re.MULTILINE)
    text = text.strip()
    start = text.find('{')
    if start == -1:
        raise ValueError("No JSON object found in response")
    depth = 0
    end = -1
    for i in range(start, len(text)):
        if text[i] == '{':
            depth += 1
        elif text[i] == '}':
            depth -= 1
            if depth == 0:
                end = i + 1
                break
    if end == -1:
        raise ValueError("Malformed JSON — no matching closing brace")
    return json.loads(text[start:end])


async def run_systems_agent() -> dict:
    """Run the systems specialist agent and return structured output."""
    config = LocalAgentConfig(
        system_instructions=TemplatedSystemInstructions(identity=IDENTITY),
    )

    async with Agent(config) as agent:
        print("[Systems Agent] Generating SugiHandi mechanics and interaction design...")
        response = await agent.chat(SYSTEMS_PROMPT)
        text = await response.text()
        data = extract_json(text)
        print("[Systems Agent] Complete")
        return data


if __name__ == "__main__":
    result = asyncio.run(run_systems_agent())
    print(json.dumps(result, indent=2, ensure_ascii=False))
