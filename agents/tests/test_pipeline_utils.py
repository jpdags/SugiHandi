"""
test_pipeline_utils.py — Unit tests for SugiHandi agent pipeline hardening.

Verifies:
  1. Robust JSON extraction (strings with braces, escaped quotes, markdown code fences).
  2. Pydantic schema validation & error raising on invalid data.
  3. Demo scope validator (NPC counts, banned mechanics keywords).
  4. Defensive GDD markdown generation with empty / partial agent results.
"""

import unittest
from pathlib import Path
import sys

# Add agents directory to sys.path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from utils import extract_json
from schemas import NarrativeDesign, SystemsDesign
from validators import validate_demo_scope
from main import build_gdd_markdown


class TestPipelineUtils(unittest.TestCase):

    def test_extract_json_clean(self):
        text = '{"name": "Datu Kalingayan", "role": "Dock Elder"}'
        data = extract_json(text)
        self.assertEqual(data["name"], "Datu Kalingayan")

    def test_extract_json_with_code_fences(self):
        text = """```json
{
  "setting_note": "A quiet dock in pre-colonial Butuan.",
  "npcs": []
}
```"""
        data = extract_json(text)
        self.assertEqual(data["setting_note"], "A quiet dock in pre-colonial Butuan.")

    def test_extract_json_with_braces_in_strings(self):
        # Dialogue containing literal braces and escaped quotes
        text = r"""
Here is the design output:
{
  "dialogue": "Look at the balangay {boat} carved with \"gold\" ornaments.",
  "count": 3
}
Hope this helps!
"""
        data = extract_json(text)
        self.assertEqual(data["count"], 3)
        self.assertIn("{boat}", data["dialogue"])
        self.assertIn('"gold"', data["dialogue"])

    def test_extract_json_invalid_raises_error(self):
        with self.assertRaises(ValueError):
            extract_json("This is purely conversational text with no JSON object.")

    def test_schema_validation_success_and_failure(self):
        # Valid minimal narrative data
        valid_data = {
            "game_title": "SugiHandi",
            "tagline": "A journey through pre-colonial Butuan",
            "setting_note": "Maritime dock area",
            "npcs": [
                {
                    "name": "Kalingayan",
                    "role": "Dock Elder",
                    "personality": "Observant and wise.",
                    "butuanon_words_they_use": ["balangay (boat)"],
                    "dialogue_branches": [
                        {
                            "trigger": "Player approaches dock",
                            "npc_opening_line": "The tide is turning.",
                            "player_options": [
                                {
                                    "option_text": "Ask about the waters",
                                    "npc_response": "The sea remembers all voyages.",
                                    "narrative_consequence": "Logged in Kodeks",
                                    "kodeks_entry_triggered": "balangay"
                                }
                            ]
                        }
                    ]
                }
            ],
            "quest_chain": {
                "title": "The Golden Seal",
                "quest_giver_npc": "Kalingayan",
                "synopsis": "Retrieve the carved seal.",
                "steps": [
                    {
                        "step_number": 1,
                        "objective": "Examine the marker",
                        "completion_trigger": "Tap stone marker",
                        "dialogue_on_completion": "The pattern is revealed."
                    }
                ],
                "reward": "Knowledge of the tidal paths",
                "butuanon_words_encountered_in_quest": ["balangay"]
            },
            "environmental_texts": [
                {
                    "location": "North Dock",
                    "text_content": "Balangay docking post",
                    "cultural_note_for_kodeks": "Pre-colonial maritime mooring"
                }
            ],
            "kodeks_seed_entries": [
                {
                    "word_or_phrase": "balangay",
                    "scene_context": "Found by the waterways",
                    "cultural_significance": "Historical plank boat"
                }
            ],
            "lore_notes": [
                {
                    "title": "Ancient Balangays of Butuan",
                    "content": "Carbon-dated to 320 AD, representing early maritime technology."
                }
            ]
        }

        # Should validate successfully
        model = NarrativeDesign.model_validate(valid_data)
        self.assertEqual(model.game_title, "SugiHandi")

        # Invalid data missing required fields should fail loudly
        with self.assertRaises(Exception):
            NarrativeDesign.model_validate({"game_title": "SugiHandi"})

    def test_demo_scope_validator(self):
        # Banned keyword check
        bad_data = {
            "systems": "We implemented a combat system with turn-based attacks and crafting stations."
        }
        issues = validate_demo_scope("systems", bad_data, strict=False)
        self.assertTrue(any("combat system" in i for i in issues))
        self.assertTrue(any("crafting station" in i for i in issues))

    def test_build_gdd_markdown_defensive_empty_dicts(self):
        # When agents fail and return {}, build_gdd_markdown should NOT raise KeyError!
        markdown = build_gdd_markdown(
            narrative={},
            systems={},
            level_design={},
            art_direction={},
            unity_architecture={},
            generated_at="2026-09-18 00:00:00",
        )
        self.assertIn("SugiHandi — Game Design Document", markdown)
        self.assertIn("Narrative Design data unavailable", markdown)
        self.assertIn("Systems Design data unavailable", markdown)
        self.assertIn("Level Design data unavailable", markdown)
        self.assertIn("Art Direction data unavailable", markdown)
        self.assertIn("Unity Architecture data unavailable", markdown)


if __name__ == "__main__":
    unittest.main()
