"""
level_design_agent.py — Specialist agent for SugiHandi's single demo scene layout.

The demo is one self-contained scene: a settlement or dock area in maritime Butuan.
There is NO multi-island world map — that is explicitly [FUTURE/CUT].

The scene must conform to the scene hierarchy standard from the dev reference:
  - 5 Tilemap layers: Ground, Water, Objects, Collision, Overhead
  - An 'Objects' parent for interactive sprites/prefabs
  - An 'NPCs' parent for character GameObjects
  - A defined Player Spawn Point
  - A UI Canvas containing the dialogue system + Kodeks panel

Pixel art specs for layout:
  - All tiles: 16×16 px
  - Characters: 16×32 px
  - 16 Pixels Per Unit in Unity
  - Camera: strictly 2D top-down (buildings = rooftops only)
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
Setting: Post-Gabok, pre-colonial maritime Butuan — the scene is a dock/settlement area.
Engine: Unity with Tilemaps and URP 2D.
Demo Scope: ONE SCENE ONLY. No world map, no boat travel, no multi-island structure.
Camera: Strictly 2D top-down. Buildings shown as rooftops only — no side-on perspective.
Pixel Art: 16×16 tiles, 16×32 character sprites, 16 PPU, Point (no filter).

Scene hierarchy (Unity standard — this must be respected):
  - Ground (Tilemap) — walkable terrain: dirt paths, docks, sand, grass
  - Water (Tilemap) — river/sea tiles at scene edges
  - Objects (Tilemap + prefabs) — interactive objects: artifacts, stone markers, crates, signage
  - Collision (Tilemap) — invisible collision layer for walls, water edges, buildings
  - Overhead (Tilemap) — elements that render above the player: tree canopy, roof overhangs
  - 'Objects' parent GameObject — holds all interactive prefabs (artifacts, env puzzle markers)
  - 'NPCs' parent GameObject — holds all NPC GameObjects (2–3 NPCs for demo)
  - Player Spawn Point — defined start position for the Manaog
  - UI Canvas — dialogue system + Kodeks panel (always present, panels shown/hidden as needed)

NPC count for demo: 2–3
Interactive objects: 1 artifact, 1 environmental interaction puzzle (tap-markers-in-order), optional signage
"""

IDENTITY = """
You are Ligaya, the Level Architect — a Unity level designer specializing in 2D top-down
pixel-art games. You understand Unity Tilemaps deeply: layer ordering, collision tilemap
setup, Rule Tiles, and sprite sorting layers.

You understand pre-colonial Butuan's physical environment: balangay docks, communal longhouses
(balay), trading areas, sacred boulders, carved markers. You design spaces that feel like
real places, not generic video game levels.

Your designs communicate cultural meaning through space — a worn path leading to an elder's
house, stone markers arranged in a sacred pattern, dock crates that hint at trade routes.

You MUST output ONLY a single valid JSON object — no markdown, no commentary, no code fences.
"""

SCHEMA_HINT = """
Output a JSON object with EXACTLY these keys:
{
  "scene_name": "string (name of the demo scene — something grounded in Butuan place names)",
  "scene_description": "string (2-3 sentences: what this place is, its atmosphere, its role in Butuan)",
  "scene_dimensions": "string (approximate tile dimensions, e.g. '40×28 tiles')",
  "tilemap_layers": {
    "ground": "string (what ground tiles make up this scene — paths, dock planks, sand, grass)",
    "water": "string (where water tiles appear — river edge, sea inlet, etc.)",
    "objects": "string (what object tiles appear — crates, signage, carved stones, dock posts)",
    "collision": "string (what areas are blocked — buildings, water edges, dense vegetation)",
    "overhead": "string (what overhead tiles exist — roof sections, tree canopy patches)"
  },
  "player_spawn_point": {
    "description": "string (where the Manaog starts and why this makes narrative sense)",
    "tile_coordinates": "string (approximate grid position, e.g. 'x:5, y:14')"
  },
  "npcs": [
    {
      "name": "string",
      "position_description": "string (where in the scene they stand and why)",
      "tile_coordinates": "string",
      "interaction_radius_tiles": "number (how many tiles away the player can trigger interaction)",
      "facing_direction": "string (Down|Up|Left|Right — initial sprite direction)"
    }
  ],
  "interactive_objects": [
    {
      "type": "string (Artifact|EnvPuzzleMarker|Signage|Door)",
      "name": "string",
      "position_description": "string (where it is and what it looks like visually)",
      "tile_coordinates": "string",
      "interaction_notes": "string (what happens when tapped)",
      "layer": "string (which Tilemap layer or parent it belongs to)"
    }
  ],
  "environmental_puzzle": {
    "description": "string (the tap-markers-in-order sequence for this demo scene)",
    "marker_positions": ["string (position description for each marker)"],
    "correct_order_rationale": "string (the cultural/narrative reason for this specific order — not shown to player)",
    "completion_effect": "string (what happens in the scene when the sequence is completed correctly)"
  },
  "environmental_storytelling_notes": "string (how the scene layout communicates Butuan history without dialogue)",
  "visual_reference_notes": "string (specific visual notes for the pixel artist — what each area should evoke)",
  "unity_scene_setup_notes": "string (practical Unity notes: sorting layers, camera bounds, Cinemachine confiner, etc.)"
}
"""

LEVEL_DESIGN_PROMPT = f"""
Given the following game context, generate the complete level design specification for
SugiHandi's single demo scene.

GAME CONTEXT:
{GAME_CONTEXT}

Requirements:
- Design ONE scene only — a dock/settlement area in maritime Butuan. No world map, no multiple areas.
- The scene must feel like a real, specific place in pre-colonial Butuan — not a generic tropical village.
- Include exactly 2–3 NPC positions with placement rationale (why does this person stand here?).
- Include the Player Spawn Point with narrative justification (why does the Manaog start here?).
- Include 1 artifact object placement and 1 environmental interaction puzzle (tap-markers-in-order).
  The puzzle markers must have a culturally meaningful correct order — but that order is never explained to the player.
- Specify all 5 Tilemap layers: Ground, Water, Objects, Collision, Overhead.
- Environmental storytelling notes must describe at least 3 details the player discovers through exploration alone.
- Unity setup notes must mention: sorting layers, Cinemachine confiner for camera bounds, and the Collision layer setup.

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


async def run_level_design_agent() -> dict:
    """Run the level design specialist agent and return structured output."""
    config = LocalAgentConfig(
        system_instructions=TemplatedSystemInstructions(identity=IDENTITY),
    )

    async with Agent(config) as agent:
        print("[Level Design Agent] Generating demo scene layout for maritime Butuan...")
        response = await agent.chat(LEVEL_DESIGN_PROMPT)
        text = await response.text()
        data = extract_json(text)
        print("[Level Design Agent] Complete")
        return data


if __name__ == "__main__":
    result = asyncio.run(run_level_design_agent())
    print(json.dumps(result, indent=2, ensure_ascii=False))
