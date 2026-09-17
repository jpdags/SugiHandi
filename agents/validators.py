"""
validators.py — Post-generation demo scope and hard-rule validator.

Enforces SugiHandi demo constraints at the code level:
  - Demo scope limits (NPC count 2-3, quest steps <= 4, single scene)
  - Forbidden mechanics detection (combat, farming, trading/barter, world map)
  - Incidental language rule (no direct translations or quiz prompts in dialogue)
"""

from typing import Any


FORBIDDEN_SYSTEM_TERMS = [
    "combat system",
    "turn-based combat",
    "real-time combat",
    "attack power",
    "hit points",
    "hp bar",
    "mana points",
    "farming mechanic",
    "crop planting",
    "harvesting crops",
    "trade market",
    "barter system",
    "currency exchange",
    "coin shop",
    "crafting station",
    "fishing minigame",
    "world map navigation",
    "multi-island voyage",
    "campaign chapter",
]

QUIZ_OR_TRANSLATION_PATTERNS = [
    "what does this word mean",
    "translate this",
    "translation:",
    "correct translation",
    "vocabulary test",
    "language quiz",
]


def _scan_strings_for_keywords(data: Any, keywords: list[str]) -> list[str]:
    """Recursively search for keywords in any string values within nested dict/list."""
    findings = []

    if isinstance(data, str):
        lower_val = data.lower()
        for kw in keywords:
            if kw in lower_val:
                findings.append(f"Found forbidden term '{kw}' in: \"{data[:80]}...\"")
    elif isinstance(data, dict):
        for k, v in data.items():
            findings.extend(_scan_strings_for_keywords(v, keywords))
    elif isinstance(data, list):
        for item in data:
            findings.extend(_scan_strings_for_keywords(item, keywords))

    return findings


def validate_narrative_scope(data: dict) -> list[str]:
    """Validate narrative content against demo constraints."""
    issues = []

    npcs = data.get("npcs", [])
    if not (2 <= len(npcs) <= 4):
        issues.append(f"NPC count {len(npcs)} violates demo scope (expected 2-3, max 4).")

    quest_chain = data.get("quest_chain", {})
    steps = quest_chain.get("steps", [])
    if len(steps) > 4:
        issues.append(f"Quest chain has {len(steps)} steps — demo scope should be <= 4 steps.")

    for pattern in QUIZ_OR_TRANSLATION_PATTERNS:
        for npc in npcs:
            for branch in npc.get("dialogue_branches", []):
                for opt in branch.get("player_options", []):
                    text = opt.get("option_text", "").lower()
                    if pattern in text:
                        issues.append(f"Incidental rule violated: player option contains quiz/translation pattern '{pattern}'.")

    return issues


def validate_systems_scope(data: dict) -> list[str]:
    """Validate gameplay mechanics against demo constraints."""
    issues = []
    forbidden_hits = _scan_strings_for_keywords(data, FORBIDDEN_SYSTEM_TERMS)
    issues.extend(forbidden_hits)
    return issues


def validate_level_design_scope(data: dict) -> list[str]:
    """Validate level design against single-scene demo constraints."""
    issues = []
    npcs = data.get("npcs", [])
    if not (2 <= len(npcs) <= 4):
        issues.append(f"Level design NPC placements ({len(npcs)}) outside demo scope (2-3 expected).")
    return issues


def validate_art_direction_scope(data: dict) -> list[str]:
    """Validate art direction specs against fixed project constraints."""
    issues = []
    palette = data.get("color_palette_16", [])
    if len(palette) != 16:
        issues.append(f"Art palette has {len(palette)} colors (strictly 16 required).")

    specs = data.get("pixel_art_specs", {})
    if specs.get("unity_ppu") not in (16, "16"):
        issues.append(f"Unity PPU is {specs.get('unity_ppu')} (must be 16).")

    return issues


def validate_demo_scope(agent_name: str, data: dict, strict: bool = False) -> list[str]:
    """
    Unified validation runner. If strict=True and issues are found, raises ValueError.
    Otherwise returns list of issue strings for warning/logging.
    """
    issues: list[str] = []

    name_lower = agent_name.lower()
    if "narrative" in name_lower:
        issues.extend(validate_narrative_scope(data))
    elif "systems" in name_lower:
        issues.extend(validate_systems_scope(data))
    elif "level" in name_lower:
        issues.extend(validate_level_design_scope(data))
    elif "art" in name_lower:
        issues.extend(validate_art_direction_scope(data))

    if issues:
        warning_msg = f"⚠️  [{agent_name}] Demo scope check found {len(issues)} issue(s):\n" + "\n".join(f"   • {issue}" for issue in issues)
        print(warning_msg)
        if strict:
            raise ValueError(f"[{agent_name}] Demo scope validation failed:\n" + "\n".join(issues))

    return issues
