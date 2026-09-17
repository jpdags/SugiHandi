"""
art_director_agent.py — Specialist agent for SugiHandi pixel art direction and asset pipeline.

The art style is Aseprite pixel art — NOT hand-painted, NOT painterly.
This distinction matters: the reference doc is explicit that all assets are hand-drawn in
Aseprite 1.3.15, imported at 16 PPU with Point (no filter) — bilinear filtering is forbidden.

Pixel art spec (from dev reference — do not deviate):
  - Tool: Aseprite 1.3.15
  - Environment tiles: 16×16 px
  - Character sprites: 16×32 px (16 wide, 32 tall)
  - Fixed 16-color palette: jungle greens, river blues, warm ambers, skin tones
  - Export: PNG with transparent background
  - Unity import: 16 Pixels Per Unit, Point (no filter) — NEVER Bilinear
  - Camera: strictly 2D top-down. Buildings shown as rooftops only.

Cultural anchor: Butuanon / pre-colonial Butuan visual culture specifically —
not generic Visayan, Malay, or pan-SEA aesthetics.
"""

import asyncio
import json
import os
from pathlib import Path

from dotenv import load_dotenv
from google.antigravity import Agent, LocalAgentConfig
from google.antigravity.types import TemplatedSystemInstructions

from schemas import ArtDirection
from utils import extract_json, get_default_retry_config, run_with_timeout_and_retry, save_raw_response
from validators import validate_demo_scope

load_dotenv(Path(__file__).parent / ".env")

GAME_CONTEXT = """
Game Title: SugiHandi
Art Tool: Aseprite 1.3.15 (pixel art — NOT hand-painted, NOT painterly, NOT vector)
Engine: Unity with Universal Render Pipeline (URP) 2D lighting

MANDATORY PIXEL ART SPECS (non-negotiable — from dev reference):
  - Environment tiles: 16×16 px
  - Character sprites: 16×32 px (taller than one tile so characters read clearly against top-down camera)
  - Color palette: FIXED 16-color palette — jungle greens, river blues, warm ambers, skin tones
  - Export format: PNG with transparent background
  - Unity import settings: 16 Pixels Per Unit (PPU), Point (no filter) — NEVER Bilinear or Trilinear
  - Camera view: strictly 2D top-down. Buildings are rooftops only — no side-on perspective.

Cultural anchor: Pre-colonial Butuan Kingdom specifically.
  - The Butuan Kingdom was known for its gold craftsmanship (gold ornaments, Kinnari figure),
    balangay seafaring culture, and trade with China and neighboring kingdoms.
  - Visual references should come from actual Butuan material culture: the Butuan Ivory Seal,
    balangay boat aesthetics, Manobo/Butuanon textile geometry, and gold ornament patterns.
  - Do NOT default to Visayan baybayin, Maranao okir, or generic pan-SEA aesthetics — those are
    the wrong cultural reference for Butuan.

Demo scene: One dock/settlement area. Lush riverside/coastal environment alongside worn wood
and stone structures. The color palette should feel warm and lived-in, not storm-devastated.
"""

IDENTITY = """
You are Marikit, the Pixel Art Director — a game art director with 12+ years of experience
leading pixel art pipelines for 2D top-down mobile games in Unity.

You are an expert in Aseprite workflows, restricted-palette pixel art (16-color, NES-era discipline),
and Unity's 2D URP lighting pipeline. You know exactly how sprite import settings affect the
final look: PPU, filter mode, atlas packing, sprite sorting layers.

You have deep respect for Butuanon/Butuan visual culture. You reference the actual material
culture of the Butuan Kingdom (gold Kinnari, balangay boats, Butuan Ivory Seal, Manobo-Bukidnon
textiles) rather than borrowing from neighboring groups.

You MUST output ONLY a single valid JSON object — no markdown, no commentary, no code fences.
"""

SCHEMA_HINT = """
Output a JSON object with EXACTLY these keys:
{
  "art_style_statement": "string (1 clear sentence: the art is pixel art made in Aseprite, not painterly)",
  "pixel_art_specs": {
    "tool": "Aseprite 1.3.15",
    "tile_size_px": "16x16",
    "character_size_px": "16x32",
    "palette_size": 16,
    "export_format": "PNG, transparent background",
    "unity_ppu": 16,
    "unity_filter_mode": "Point (no filter)",
    "camera_perspective": "Strictly 2D top-down"
  },
  "color_palette_16": [
    {
      "index": 0,
      "hex": "#hexcode",
      "name": "string (e.g. 'Deep River Blue')",
      "usage": "string (e.g. 'deep water tiles, shadows')"
    }
  ],
  "palette_groups": {
    "jungle_greens": ["#hexcode"],
    "river_blues": ["#hexcode"],
    "warm_ambers": ["#hexcode"],
    "skin_tones": ["#hexcode"],
    "neutrals_and_outlines": ["#hexcode"]
  },
  "character_sprite_guide": {
    "manaog_player": "string (visual description: clothing, colors used from palette, distinguishing features)",
    "npc_design_principles": "string (how NPCs should look distinct from player while fitting Butuan culture)",
    "animation_frames_per_action": {
      "idle": "number",
      "walk": "number",
      "interact": "number"
    },
    "cultural_clothing_notes": "string (accurate pre-colonial Butuan clothing: materials, colors, patterns)"
  },
  "tilemap_visual_guide": {
    "ground_tiles": "string (what ground tiles look like: dock planks, dirt, sand, grass — palette colors used)",
    "water_tiles": "string (river/sea tiles — palette colors, animation approach for water shimmer)",
    "object_tiles": "string (crates, signage, carved stones — pixel art treatment)",
    "overhead_tiles": "string (roof sections, tree canopy — how they layer over characters)",
    "collision_tiles": "string (invisible — just note they exist for wall/water boundaries)"
  },
  "ui_aesthetic": "string (UI should feel like carved wood and woven cloth — describe pixel art UI frame style, font approach with TextMeshPro)",
  "unity_lighting_notes": "string (URP 2D lighting: Global Light 2D settings, Point lights for torches/glows, no bloom overuse)",
  "cultural_accuracy_notes": "string (specific to Butuan — what NOT to do; which visual tropes to avoid)",
  "visual_references": ["string (specific pixel art games or Butuan material culture references — be specific)"],
  "aseprite_workflow_notes": "string (palette locking in Aseprite, indexed color mode, export settings)"
}
"""

ART_DIRECTION_PROMPT = f"""
Given the following game context, generate the complete pixel art direction document for SugiHandi.

GAME CONTEXT:
{GAME_CONTEXT}

Requirements:
- The 16-color palette must cover exactly 16 colors across the four groups: jungle greens, river blues,
  warm ambers, and skin tones, plus neutrals/outlines. Provide exact hex codes for all 16.
- Character animation frame counts must be practical for a student solo/small-team pixel art project.
- Cultural accuracy notes must specifically address Butuan Kingdom visual culture — not generic SEA.
- Unity lighting notes must address URP 2D specifically (Global Light 2D, Freeform Light 2D, sprite sorting).
- Visual references must include at least one real pixel art game AND one actual Butuan material culture artifact.
- Aseprite workflow notes must mention indexed color mode and palette locking — this enforces the 16-color limit.
- Do NOT suggest hand-painted, painterly, or vector styles — pixel art (Aseprite) only.

{SCHEMA_HINT}

Output ONLY the JSON object. No markdown. No explanation. No code fences.
"""


async def _execute_art_director_agent() -> dict:
    """Core art director agent execution with native schema & fallback extraction."""
    config = LocalAgentConfig(
        system_instructions=TemplatedSystemInstructions(identity=IDENTITY),
        response_schema=ArtDirection,
        retry_config=get_default_retry_config(),
    )

    async with Agent(config) as agent:
        print("[Art Director Agent] Generating Butuan pixel art direction and asset pipeline...")
        response = await agent.chat(ART_DIRECTION_PROMPT)

        structured = await response.structured_output()
        if structured:
            if isinstance(structured, dict):
                data = structured
            else:
                data = structured.model_dump() if hasattr(structured, "model_dump") else dict(structured)
            save_raw_response("art_direction", json.dumps(data, indent=2, ensure_ascii=False))
        else:
            text = await response.text()
            save_raw_response("art_direction", text)
            data = extract_json(text)

        validated = ArtDirection.model_validate(data)
        validated_dict = validated.model_dump()
        validate_demo_scope("art_direction", validated_dict)
        print("[Art Director Agent] Complete")
        return validated_dict


async def run_art_director_agent(timeout_seconds: float = 120.0, max_attempts: int = 2) -> dict:
    """Run the art director specialist agent with timeout, retry backoff, and schema validation."""
    return await run_with_timeout_and_retry(
        agent_name="Art Director Agent",
        action=_execute_art_director_agent,
        schema_cls=ArtDirection,
        timeout_seconds=timeout_seconds,
        max_attempts=max_attempts,
    )


if __name__ == "__main__":
    result = asyncio.run(run_art_director_agent())
    print(json.dumps(result, indent=2, ensure_ascii=False))
