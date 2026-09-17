"""
narrative_agent.py — Specialist agent for SugiHandi story, NPCs, dialogue, and Kodeks content.

SugiHandi is a 2D top-down pixel-art Android game set in post-Gabok, pre-colonial maritime
Butuan. The player is the Manaog — a navigator. The game teaches the Butuanon language
incidentally through NPC dialogue and environmental text, never through direct lessons or quizzes.

This agent generates demo-scoped narrative content: 2–3 NPCs with branching dialogue,
1 short quest chain, Kodeks seed entries, and Butuanon word placements.

HARD RULE: No translation is ever shown in dialogue. Butuanon words are embedded naturally
in English dialogue — meaning comes from context, gesture cues, and quest outcomes.
"""

import asyncio
import json
import os
import re
from pathlib import Path

from dotenv import load_dotenv
from google.antigravity import Agent, LocalAgentConfig
from google.antigravity.types import TemplatedSystemInstructions

load_dotenv(Path(__file__).parent / ".env")

GAME_CONTEXT = """
Game Title: SugiHandi
Setting: Post-Gabok, pre-colonial maritime Butuan (not a generic SEA archipelago — specifically Butuan,
         a historical gold-trading kingdom on Mindanao's northeastern coast).
Player Character: The Manaog — a navigator and seafarer, not a farmer or villager.
Core Purpose: Teach the Butuanon language incidentally through play. Players absorb vocabulary
              through context, NPC dialogue, and environmental text — never through quizzes, drills,
              or direct translation.
Demo Scope: One self-contained scene (a settlement/dock area). The output of this agent covers
            the narrative content needed for that single scene only.

HARD RULES (never violate these):
1. Butuanon words are embedded naturally inside English dialogue — meaning comes from context only.
2. No translation is ever shown in the dialogue box. Not as a tooltip, subtitle, or annotation.
3. No quiz mechanics, no "what does this word mean?" prompts, no vocabulary gates.
4. Branching dialogue options must have real narrative weight — not just cosmetic variations.
5. The Kodeks (cultural codex) logs entries silently in the background; it is never shown mid-scene.
6. This is a DEMO — do not design full campaign arcs, multi-chapter quests, or a world map.
"""

IDENTITY = """
You are Amara, the Narrative Architect — a specialist in Butuanon oral tradition, pre-colonial
Butuanon society, and language-in-context game design. You have deep knowledge of the Butuan
Kingdom's history (gold trade, balangay seafarers, the 320 AD balangay boats), Butuanon
vocabulary, and how incidental language exposure works in educational game design.

Your role is to generate demo-scoped narrative content for SugiHandi's single demo scene:
NPCs grounded in Butuan culture, branching dialogue with naturally embedded Butuanon words,
a short quest chain, and Kodeks seed entries.

You write NPCs who feel like real people from maritime Butuan — not mythologized archetypes.
You MUST output ONLY a single valid JSON object — no markdown, no commentary, no code fences.
"""

SCHEMA_HINT = """
Output a JSON object with EXACTLY these keys:
{
  "game_title": "SugiHandi",
  "tagline": "string",
  "setting_note": "string (1-2 sentences grounding the demo scene in Butuan)",
  "npcs": [
    {
      "name": "string",
      "role": "string (e.g. 'Dock Elder', 'Fisherman', 'Trader')",
      "personality": "string (2-3 sentences)",
      "butuanon_words_they_use": ["string (word + the natural context it appears in)"],
      "dialogue_branches": [
        {
          "trigger": "string (what the player does to start this branch)",
          "npc_opening_line": "string (NPC's opening dialogue — embed Butuanon naturally)",
          "player_options": [
            {
              "option_text": "string (what the player can choose to say/do)",
              "npc_response": "string (NPC's response — embed Butuanon naturally)",
              "narrative_consequence": "string (what changes in the world/quest/Kodeks as a result)",
              "kodeks_entry_triggered": "string or null"
            }
          ]
        }
      ]
    }
  ],
  "quest_chain": {
    "title": "string",
    "quest_giver_npc": "string",
    "synopsis": "string (2-3 sentences, demo scope only)",
    "steps": [
      {
        "step_number": 1,
        "objective": "string",
        "completion_trigger": "string (what the player does to complete this step)",
        "dialogue_on_completion": "string"
      }
    ],
    "reward": "string (narrative reward — no coins or points, this is not a trading game)",
    "butuanon_words_encountered_in_quest": ["string"]
  },
  "environmental_texts": [
    {
      "location": "string (where in the scene this text appears)",
      "text_content": "string (the Butuanon inscription or signage text)",
      "cultural_note_for_kodeks": "string (what the Kodeks logs about this, shown only post-scene)"
    }
  ],
  "kodeks_seed_entries": [
    {
      "word_or_phrase": "string (Butuanon word or phrase)",
      "scene_context": "string (the scene context it was encountered in — NOT a translation)",
      "cultural_significance": "string"
    }
  ],
  "lore_notes": [
    {
      "title": "string",
      "content": "string (2-3 sentences of Butuan history/culture relevant to the demo scene)"
    }
  ]
}
"""

NARRATIVE_PROMPT = f"""
Given the following game context, generate the complete demo-scoped narrative content for
SugiHandi's single demo scene set in maritime Butuan.

GAME CONTEXT:
{GAME_CONTEXT}

Requirements:
- Generate exactly 2–3 NPCs. Each must feel grounded in pre-colonial Butuan life, not mythologized.
- Each NPC must have at least 2 dialogue branches with 2–3 player options each.
- Every piece of NPC dialogue must include at least one Butuanon word embedded naturally — no translations shown.
- The quest chain must be completable in a single demo session (accept → objective → complete → reward).
- Quest reward must be narrative/relational — not coins, points, or trade goods.
- Generate 3–5 Kodeks seed entries — these are what the system silently logs when the player
  encounters Butuanon words/artifacts. Each entry shows scene context, never a direct translation.
- Generate 2–3 environmental texts (inscriptions, dock signage, carved markers) with Butuanon text.
- Lore notes should ground the demo scene in real Butuan history (Laguna Copper Plate era, balangay culture, etc.)
- Do not generate multi-chapter arcs, world maps, or full campaign content — demo scope only.

{SCHEMA_HINT}

Output ONLY the JSON object. No markdown. No explanation. No code fences.
"""


def extract_json(text: str) -> dict:
    """Extract JSON from agent response text, stripping markdown fences if present."""
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


async def run_narrative_agent() -> dict:
    """Run the narrative specialist agent and return structured output."""
    config = LocalAgentConfig(
        system_instructions=TemplatedSystemInstructions(identity=IDENTITY),
    )

    async with Agent(config) as agent:
        print("[Narrative Agent] Generating Butuanon NPCs, dialogue, and Kodeks content...")
        response = await agent.chat(NARRATIVE_PROMPT)
        text = await response.text()
        data = extract_json(text)
        print("[Narrative Agent] Complete")
        return data


if __name__ == "__main__":
    result = asyncio.run(run_narrative_agent())
    print(json.dumps(result, indent=2, ensure_ascii=False))
