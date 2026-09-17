"""
main.py — Orchestrator for the SugiHandi Game Design Agent Pipeline.

NOTE: stdout is forced to UTF-8 on Windows to support emoji/unicode output.

This script coordinates all five specialist agents, collects their structured outputs,
and compiles everything into a Game Design Document (GDD) for the SugiHandi demo build.

Agents:
  1. narrative_agent     — Butuanon NPCs, dialogue, Kodeks entries, quest chain (Butuan setting)
  2. systems_agent       — Six core mechanics spec (tap-to-move, dialogue, Kodeks, artifact, puzzle, quest)
  3. level_design_agent  — Single demo scene layout (dock/settlement in maritime Butuan)
  4. art_director_agent  — Pixel art spec (Aseprite, 16×16 tiles, 16-color palette, 16 PPU)
  5. unity_architecture_agent — C# component architecture, local save, scene loop, DOTween patterns

Usage:
    python main.py

Output:
    agents/output/narrative.json
    agents/output/systems.json
    agents/output/level_design.json
    agents/output/art_direction.json
    agents/output/unity_architecture.json
    agents/output/game_design_document.md   ← the full GDD
"""

import asyncio
import json
import os
import sys
from datetime import datetime
from pathlib import Path

import sys
import io

# Force UTF-8 output on Windows (fixes cp1252 UnicodeEncodeError)
if sys.platform == "win32":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")

from dotenv import load_dotenv

# Load environment variables from .env file
env_path = Path(__file__).parent / ".env"
if not env_path.exists():
    print("❌ ERROR: .env file not found!")
    print(f"   Create a .env file at: {env_path}")
    print("   Copy .env.example and add your GEMINI_API_KEY.")
    sys.exit(1)

load_dotenv(env_path)

if not os.getenv("GEMINI_API_KEY"):
    print("❌ ERROR: GEMINI_API_KEY is not set in your .env file.")
    print("   Get your key at: https://aistudio.google.com/app/api-keys")
    sys.exit(1)

# Add agents directory to path for imports
sys.path.insert(0, str(Path(__file__).parent))

from narrative_agent import run_narrative_agent
from systems_agent import run_systems_agent
from level_design_agent import run_level_design_agent
from art_director_agent import run_art_director_agent
from unity_architecture_agent import run_unity_architecture_agent

OUTPUT_DIR = Path(__file__).parent / "output"
OUTPUT_DIR.mkdir(exist_ok=True)


def save_json(data: dict, filename: str) -> None:
    """Save a dict as pretty-printed JSON."""
    path = OUTPUT_DIR / filename
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    print(f"   💾 Saved: {path}")


def build_gdd_markdown(
    narrative: dict,
    systems: dict,
    level_design: dict,
    art_direction: dict,
    unity_architecture: dict,
    generated_at: str,
) -> str:
    """Compile all agent outputs into a formatted Game Design Document for SugiHandi."""

    def h1(text): return f"# {text}\n"
    def h2(text): return f"\n## {text}\n"
    def h3(text): return f"\n### {text}\n"
    def h4(text): return f"\n#### {text}\n"
    def hr(): return "\n---\n"
    def bold(text): return f"**{text}**"
    def bullet(text): return f"- {text}"
    def numbered(i, text): return f"{i}. {text}"
    def blockquote(text): return f"> {text}"
    def code(text): return f"`{text}`"

    lines = []

    # ── Header ──────────────────────────────────────────────────────────────
    lines.append(h1(f"🌊 {narrative.get('game_title', 'SugiHandi')} — Game Design Document (Demo Build)"))
    lines.append(f"*{narrative.get('tagline', '')}*\n")
    lines.append(f"> Generated: {generated_at}\n")
    lines.append(f"> Scope: **Single vertical-slice demo** — one scene, maritime Butuan setting.\n")
    lines.append(blockquote("Core rule: Butuanon exposure is always *incidental to play*, never an explicit lesson. No quizzes, no translation boxes, no vocabulary gates."))
    lines.append("")
    lines.append(hr())

    # ── Narrative ────────────────────────────────────────────────────────────
    lines.append(h2("📖 Narrative Design"))
    lines.append(f"{blockquote(narrative.get('setting_note', ''))}\n")

    lines.append(h3("NPCs (Demo Scene)"))
    for npc in narrative.get("npcs", []):
        lines.append(h4(f"{npc['name']} — *{npc['role']}*"))
        lines.append(npc.get("personality", "") + "\n")
        lines.append(bold("Butuanon words they use:"))
        for word_context in npc.get("butuanon_words_they_use", []):
            lines.append(bullet(word_context))
        lines.append("")

        lines.append(bold("Dialogue Branches:"))
        for branch in npc.get("dialogue_branches", []):
            lines.append(f"\n- **Trigger:** {branch.get('trigger', '')}")
            lines.append(f"  *NPC:* \"{branch.get('npc_opening_line', '')}\"")
            lines.append(f"  **Player options:**")
            for opt in branch.get("player_options", []):
                lines.append(f"    - **→ {opt['option_text']}**")
                lines.append(f"      *NPC:* \"{opt['npc_response']}\"")
                lines.append(f"      *Consequence:* {opt['narrative_consequence']}")
                if opt.get("kodeks_entry_triggered"):
                    lines.append(f"      *Kodeks logs:* {opt['kodeks_entry_triggered']}")
        lines.append("")

    lines.append(h3("Quest Chain"))
    qc = narrative.get("quest_chain", {})
    lines.append(f"{bold(qc.get('title', ''))} — given by {bold(qc.get('quest_giver_npc', ''))}\n")
    lines.append(qc.get("synopsis", "") + "\n")
    lines.append(bold("Steps:"))
    for step in qc.get("steps", []):
        lines.append(f"\n**Step {step['step_number']}:** {step['objective']}")
        lines.append(f"- *Completion trigger:* {step['completion_trigger']}")
        lines.append(f"- *On complete:* \"{step['dialogue_on_completion']}\"")
    lines.append(f"\n{bold('Reward:')} {qc.get('reward', '')}")
    lines.append(f"\n{bold('Butuanon vocabulary encountered:')} {', '.join(qc.get('butuanon_words_encountered_in_quest', []))}\n")

    lines.append(h3("Environmental Texts (Inscriptions / Signage)"))
    for env_text in narrative.get("environmental_texts", []):
        lines.append(f"\n**{env_text['location']}**")
        lines.append(f"> *{env_text['text_content']}*")
        lines.append(f"Kodeks note (shown post-scene): {env_text['cultural_note_for_kodeks']}\n")

    lines.append(h3("Kodeks Seed Entries"))
    for entry in narrative.get("kodeks_seed_entries", []):
        lines.append(f"\n**{entry['word_or_phrase']}**")
        lines.append(f"- *Scene context:* {entry['scene_context']}")
        lines.append(f"- *Cultural significance:* {entry['cultural_significance']}")
    lines.append("")

    lines.append(h3("Lore Notes (Butuan History)"))
    for note in narrative.get("lore_notes", []):
        lines.append(f"\n**{note['title']}**")
        lines.append(note.get("content", "") + "\n")

    lines.append(hr())

    # ── Systems ──────────────────────────────────────────────────────────────
    lines.append(h2("⚙️ Gameplay Mechanics Design"))

    lines.append(h3("Game Loop"))
    lines.append(systems.get("game_loop_overview", "") + "\n")

    lines.append(h3("5.1 Tap-to-Move / Tap-to-Interact"))
    ttm = systems.get("tap_to_move_spec", {})
    lines.append(ttm.get("description", "") + "\n")
    lines.append(f"- {bold('Unity implementation:')} {ttm.get('unity_implementation_notes', '')}")
    lines.append(f"- {bold('Pathfinding:')} {ttm.get('pathfinding_approach', '')}")
    lines.append(f"- {bold('Interact radius:')} {ttm.get('interact_trigger_radius', '')}\n")

    lines.append(h3("5.2 Branching Dialogue System"))
    bd = systems.get("branching_dialogue_spec", {})
    lines.append(bd.get("description", "") + "\n")
    lines.append(f"- {bold('Options per node:')} {bd.get('options_per_node', '')}")
    lines.append(f"- {bold('Butuanon rule:')} {bd.get('butuanon_embedding_rule', '')}")
    lines.append(f"- {bold('No-translation rule:')} {bd.get('no_translation_rule', '')}")
    lines.append(f"- {bold('Data format:')} {bd.get('dialogue_data_format', '')}")
    lines.append(f"- {bold('Unity notes:')} {bd.get('unity_implementation_notes', '')}\n")

    lines.append(h3("5.3 Cultural Codex — Kodeks"))
    kd = systems.get("kodeks_spec", {})
    lines.append(kd.get("description", "") + "\n")
    lines.append(f"- {bold('Logging trigger:')} {kd.get('logging_trigger', '')}")
    lines.append(f"- {bold('When shown:')} {kd.get('when_shown_to_player', '')}")
    lines.append(f"- {bold('Entry format:')} {kd.get('entry_format', '')}")
    lines.append(f"- {bold('Save key:')} {code(kd.get('local_save_key', ''))}")
    lines.append(f"- {bold('Unity notes:')} {kd.get('unity_implementation_notes', '')}\n")

    lines.append(h3("5.4 Artifact Collection & Examination"))
    af = systems.get("artifact_examination_spec", {})
    lines.append(af.get("description", "") + "\n")
    lines.append(f"- {bold('Demo scope:')} {af.get('demo_scope', '')}")
    lines.append(f"- {bold('Examine screen:')} {af.get('examine_screen_content', '')}")
    lines.append(f"- {bold('Kodeks log:')} {af.get('kodeks_log_on_examine', '')}")
    lines.append(f"- {bold('Unity notes:')} {af.get('unity_implementation_notes', '')}\n")

    lines.append(h3("5.5 Environmental Interaction Sequences"))
    ei = systems.get("environmental_interaction_spec", {})
    lines.append(ei.get("description", "") + "\n")
    lines.append(f"- {bold('Demo implementation:')} {ei.get('demo_implementation', '')}")
    lines.append(f"- {bold('No instructions:')} {ei.get('no_instructions_rule', '')}")
    lines.append(f"- {bold('No timer:')} {ei.get('no_timer_rule', '')}")
    lines.append(f"- {bold('No fail state:')} {ei.get('no_fail_state_rule', '')}")
    lines.append(f"- {bold('Completion trigger:')} {ei.get('completion_trigger', '')}")
    lines.append(f"- {bold('Unity notes:')} {ei.get('unity_implementation_notes', '')}\n")

    lines.append(h3("5.6 Quest System"))
    qs = systems.get("quest_system_spec", {})
    lines.append(qs.get("description", "") + "\n")
    lines.append(f"- {bold('Demo flow:')} {qs.get('demo_quest_flow', '')}")
    lines.append(f"- {bold('Progress check:')} {qs.get('progress_check_logic', '')}")
    lines.append(f"- {bold('Incomplete handling:')} {qs.get('incomplete_state_handling', '')}")
    lines.append(f"- {bold('Save key:')} {code(qs.get('local_save_key', ''))}")
    lines.append(f"- {bold('Unity notes:')} {qs.get('unity_implementation_notes', '')}\n")

    lines.append(h3("Local Save Strategy"))
    ls = systems.get("local_save_spec", {})
    lines.append(f"{bold('Method:')} {ls.get('save_method', '')}\n")
    lines.append(ls.get("description", "") + "\n")
    lines.append(bold("Data saved:"))
    for item in ls.get("data_saved", []):
        lines.append(bullet(item))
    lines.append(f"\n{bold('Save triggers:')} {ls.get('save_triggers', '')}\n")

    lines.append(h3("System Communication Flows"))
    for flow in systems.get("system_communication_flow", []):
        lines.append(f"\n**{flow['scenario']}**")
        lines.append(f"`{flow['flow']}`\n")

    lines.append(hr())

    # ── Level Design ─────────────────────────────────────────────────────────
    lines.append(h2("🗺️ Level Design — Demo Scene"))

    lines.append(h3(f"Scene: {level_design.get('scene_name', 'Demo Scene')}"))
    lines.append(level_design.get("scene_description", "") + "\n")
    lines.append(f"{bold('Dimensions:')} {level_design.get('scene_dimensions', '')}\n")

    lines.append(h3("Tilemap Layers"))
    layers = level_design.get("tilemap_layers", {})
    for layer_name, description in layers.items():
        lines.append(f"- {bold(layer_name.capitalize()+'  ')}: {description}")
    lines.append("")

    lines.append(h3("Player Spawn Point"))
    spawn = level_design.get("player_spawn_point", {})
    lines.append(f"*{spawn.get('description', '')}*")
    lines.append(f"Coordinates: {code(spawn.get('tile_coordinates', ''))}\n")

    lines.append(h3("NPC Placements"))
    for npc in level_design.get("npcs", []):
        lines.append(f"\n**{npc['name']}** — {npc['position_description']}")
        lines.append(f"- Coordinates: {code(npc['tile_coordinates'])}, facing {npc['facing_direction']}")
        lines.append(f"- Interaction radius: {npc['interaction_radius_tiles']} tiles")
    lines.append("")

    lines.append(h3("Interactive Objects"))
    for obj in level_design.get("interactive_objects", []):
        lines.append(f"\n**{obj['name']}** ({obj['type']})")
        lines.append(f"- {obj['position_description']}")
        lines.append(f"- Coordinates: {code(obj['tile_coordinates'])}, Layer: {obj['layer']}")
        lines.append(f"- On tap: {obj['interaction_notes']}")
    lines.append("")

    lines.append(h3("Environmental Interaction Puzzle"))
    ep = level_design.get("environmental_puzzle", {})
    lines.append(ep.get("description", "") + "\n")
    lines.append(bold("Marker positions:"))
    for pos in ep.get("marker_positions", []):
        lines.append(bullet(pos))
    lines.append(f"\n{bold('Correct order rationale (dev-only, not shown to player):')}")
    lines.append(blockquote(ep.get("correct_order_rationale", "")))
    lines.append(f"\n{bold('Completion effect:')} {ep.get('completion_effect', '')}\n")

    lines.append(h3("Environmental Storytelling"))
    lines.append(level_design.get("environmental_storytelling_notes", "") + "\n")

    lines.append(h3("Visual Reference Notes (for pixel artist)"))
    lines.append(level_design.get("visual_reference_notes", "") + "\n")

    lines.append(h3("Unity Scene Setup Notes"))
    lines.append(level_design.get("unity_scene_setup_notes", "") + "\n")

    lines.append(hr())

    # ── Art Direction ────────────────────────────────────────────────────────
    lines.append(h2("🎨 Art Direction — Pixel Art"))

    lines.append(h3("Art Style"))
    lines.append(art_direction.get("art_style_statement", "") + "\n")

    lines.append(h3("Pixel Art Specifications"))
    specs = art_direction.get("pixel_art_specs", {})
    lines.append("| Parameter | Value |")
    lines.append("|-----------|-------|")
    lines.append(f"| Tool | {specs.get('tool', '')} |")
    lines.append(f"| Tile size | {specs.get('tile_size_px', '')} px |")
    lines.append(f"| Character size | {specs.get('character_size_px', '')} px |")
    lines.append(f"| Palette | {specs.get('palette_size', '')} colors |")
    lines.append(f"| Export | {specs.get('export_format', '')} |")
    lines.append(f"| Unity PPU | {specs.get('unity_ppu', '')} |")
    lines.append(f"| Filter mode | {specs.get('unity_filter_mode', '')} |")
    lines.append(f"| Camera | {specs.get('camera_perspective', '')} |")
    lines.append("")

    lines.append(h3("16-Color Palette"))
    lines.append("| # | Hex | Name | Usage |")
    lines.append("|---|-----|------|-------|")
    for color in art_direction.get("color_palette_16", []):
        lines.append(f"| {color['index']} | `{color['hex']}` | {color['name']} | {color['usage']} |")
    lines.append("")

    lines.append(h3("Palette Groups"))
    groups = art_direction.get("palette_groups", {})
    for group_name, colors in groups.items():
        color_str = "  ".join([f"`{c}`" for c in colors])
        lines.append(f"- {bold(group_name.replace('_', ' ').title())}: {color_str}")
    lines.append("")

    lines.append(h3("Character Sprite Guide"))
    csg = art_direction.get("character_sprite_guide", {})
    lines.append(f"{bold('Manaog (Player):')} {csg.get('manaog_player', '')}\n")
    lines.append(f"{bold('NPC design principles:')} {csg.get('npc_design_principles', '')}\n")
    anim = csg.get("animation_frames_per_action", {})
    lines.append(f"{bold('Animation frames:')} Idle: {anim.get('idle', '')} | Walk: {anim.get('walk', '')} | Interact: {anim.get('interact', '')}\n")
    lines.append(f"{bold('Cultural clothing notes:')} {csg.get('cultural_clothing_notes', '')}\n")

    lines.append(h3("Tilemap Visual Guide"))
    tvg = art_direction.get("tilemap_visual_guide", {})
    for layer, desc in tvg.items():
        lines.append(f"- {bold(layer.replace('_', ' ').title())}: {desc}")
    lines.append("")

    lines.append(h3("UI Aesthetic"))
    lines.append(art_direction.get("ui_aesthetic", "") + "\n")

    lines.append(h3("URP 2D Lighting Notes"))
    lines.append(art_direction.get("unity_lighting_notes", "") + "\n")

    lines.append(h3("⚠️ Cultural Accuracy Notes (Butuan-specific)"))
    lines.append(f"> {art_direction.get('cultural_accuracy_notes', '')}\n")

    lines.append(h3("Visual References"))
    for ref in art_direction.get("visual_references", []):
        lines.append(bullet(ref))
    lines.append("")

    lines.append(h3("Aseprite Workflow Notes"))
    lines.append(art_direction.get("aseprite_workflow_notes", "") + "\n")

    lines.append(hr())

    # ── Unity Architecture ────────────────────────────────────────────────────
    lines.append(h2("🏗️ Unity C# Architecture"))

    lines.append(h3("Overview"))
    lines.append(unity_architecture.get("architecture_overview", "") + "\n")

    lines.append(h3("Core Systems"))
    for system in unity_architecture.get("systems", []):
        lines.append(h4(f"`{system['class_name']}` — {system['system_name']}"))
        lines.append(f"*{system['responsibility']}*\n")
        lines.append(bold("Key fields:"))
        for field in system.get("key_fields", []):
            lines.append(bullet(field))
        lines.append("")
        lines.append(bold("Key methods:"))
        for method in system.get("key_methods", []):
            lines.append(bullet(f"`{method}`"))
        lines.append("")
        lines.append(bold("Events fired:"))
        for event in system.get("events_fired", []):
            lines.append(bullet(event))
        lines.append("")
        lines.append(bold("Listens to:"))
        for event in system.get("events_listened_to", []):
            lines.append(bullet(event))
        lines.append("")
        lines.append(bold("Required Unity components:"))
        for comp in system.get("unity_components_required", []):
            lines.append(bullet(comp))
        if system.get("notes"):
            lines.append(f"\n{blockquote(system['notes'])}")
        lines.append("")

    lines.append(h3("Scene Management"))
    sm = unity_architecture.get("scene_management", {})
    lines.append(bold("Scenes:"))
    for scene in sm.get("scenes_list", []):
        lines.append(bullet(scene))
    lines.append(f"\n{bold('Main Menu → Demo:')} {sm.get('main_menu_to_demo', '')}")
    lines.append(f"\n{bold('Demo → Summary:')} {sm.get('demo_to_summary', '')}")
    lines.append(f"\n{bold('Summary → Main Menu:')} {sm.get('summary_to_main_menu', '')}\n")

    lines.append(h3("Local Save Design"))
    lsd = unity_architecture.get("local_save_design", {})
    lines.append(f"{bold('Method:')} {lsd.get('chosen_method', '')}")
    lines.append(f"\n{lsd.get('justification', '')}\n")
    schema = lsd.get("save_data_schema", {})
    lines.append(bold("Data schema:"))
    lines.append(f"- {bold('Kodeks entries:')} {schema.get('kodeks_entries', '')}")
    lines.append(f"- {bold('Quest state:')} {schema.get('quest_state', '')}")
    lines.append(f"- {bold('Collected artifacts:')} {schema.get('collected_artifacts', '')}")
    lines.append(f"\n{bold('Save class:')} {code(lsd.get('save_class_name', ''))}")
    lines.append(f"\n{bold('Save trigger:')} {lsd.get('save_trigger', '')}\n")

    lines.append(h3("Touch Input Architecture"))
    ti = unity_architecture.get("touch_input_architecture", {})
    lines.append(f"- {bold('Package:')} {code(ti.get('unity_package', ''))}")
    lines.append(f"- {bold('Tap detection:')} {ti.get('tap_detection_approach', '')}")
    lines.append(f"- {bold('Raycast:')} {ti.get('raycast_approach', '')}")
    lines.append(f"- {bold('Move trigger:')} {ti.get('move_trigger', '')}")
    lines.append(f"- {bold('Interact trigger:')} {ti.get('interact_trigger', '')}\n")

    lines.append(h3("DOTween UI Patterns"))
    for pattern in unity_architecture.get("dotween_ui_patterns", []):
        lines.append(f"\n**{pattern['panel']}**")
        lines.append(f"- Show: `{pattern['show_tween']}`")
        lines.append(f"- Hide: `{pattern['hide_tween']}`")
        if pattern.get("notes"):
            lines.append(f"- {pattern['notes']}")
    lines.append("")

    lines.append(h3("Dialogue Data Format"))
    ddf = unity_architecture.get("dialogue_data_format", {})
    lines.append(f"{bold('Storage:')} {ddf.get('storage', '')}")
    lines.append(f"\n{ddf.get('structure_description', '')}")
    lines.append(f"\n{bold('Butuanon embedding:')} {ddf.get('butuanon_embedding_approach', '')}\n")

    lines.append(h3("Kodeks Logging Pipeline"))
    kp = unity_architecture.get("kodeks_pipeline", {})
    lines.append(f"- {bold('Log trigger:')} {kp.get('log_trigger', '')}")
    lines.append(f"- {bold('In-session storage:')} {kp.get('storage_during_session', '')}")
    lines.append(f"- {bold('Display timing:')} {kp.get('display_timing', '')}")
    lines.append(f"- {bold('Panel architecture:')} {kp.get('panel_architecture', '')}\n")

    lines.append(h3("Data Flow Scenarios"))
    for scenario in unity_architecture.get("data_flow_scenarios", []):
        lines.append(f"\n**{scenario['scenario']}**")
        for i, step in enumerate(scenario.get("step_by_step", []), 1):
            lines.append(f"{i}. {step}")
    lines.append("")

    lines.append(h3("Recommended Folder Structure"))
    fs = unity_architecture.get("folder_structure", {})
    for category, items in fs.items():
        lines.append(f"\n{bold(category.capitalize()+'/')}")
        for item in items:
            lines.append(bullet(item))
    lines.append("")

    lines.append(hr())
    lines.append("\n*This Game Design Document was generated by the SugiHandi Agent Pipeline.*\n")
    lines.append(f"*Demo scope: one scene, maritime Butuan. Full-game content is [FUTURE/CUT].*\n")
    lines.append(f"*© {datetime.now().year} SugiHandi. All rights reserved.*\n")

    return "\n".join(lines)


async def run_pipeline() -> None:
    """Main orchestration pipeline — runs all agents and compiles the GDD."""

    print("\n" + "═" * 60)
    print("  🌊 SUGIHANDI — Game Design Agent Pipeline (Demo Build)")
    print("═" * 60)
    print(f"  Started: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("═" * 60 + "\n")

    # ── Run all specialist agents concurrently ───────────────────────────────
    print("🚀 Launching all specialist agents...\n")

    results = await asyncio.gather(
        run_narrative_agent(),
        run_systems_agent(),
        run_level_design_agent(),
        run_art_director_agent(),
        run_unity_architecture_agent(),
        return_exceptions=True,
    )

    narrative, systems, level_design, art_direction, unity_architecture = results

    # ── Check for errors ─────────────────────────────────────────────────────
    errors = []
    agent_names = ["Narrative", "Systems", "Level Design", "Art Direction", "Unity Architecture"]
    for name, result in zip(agent_names, results):
        if isinstance(result, Exception):
            errors.append(f"{name}: {result}")

    if errors:
        print("\n❌ Some agents encountered errors:")
        for err in errors:
            print(f"   • {err}")
        print("\nPartial results will still be saved.")

    # ── Save individual JSON outputs ─────────────────────────────────────────
    print("\n💾 Saving individual outputs...")
    if not isinstance(narrative, Exception):
        save_json(narrative, "narrative.json")
    if not isinstance(systems, Exception):
        save_json(systems, "systems.json")
    if not isinstance(level_design, Exception):
        save_json(level_design, "level_design.json")
    if not isinstance(art_direction, Exception):
        save_json(art_direction, "art_direction.json")
    if not isinstance(unity_architecture, Exception):
        save_json(unity_architecture, "unity_architecture.json")

    # ── Compile the full GDD ─────────────────────────────────────────────────
    print("\n📄 Compiling Game Design Document...")
    generated_at = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

    gdd_markdown = build_gdd_markdown(
        narrative=narrative if not isinstance(narrative, Exception) else {},
        systems=systems if not isinstance(systems, Exception) else {},
        level_design=level_design if not isinstance(level_design, Exception) else {},
        art_direction=art_direction if not isinstance(art_direction, Exception) else {},
        unity_architecture=unity_architecture if not isinstance(unity_architecture, Exception) else {},
        generated_at=generated_at,
    )

    gdd_path = OUTPUT_DIR / "game_design_document.md"
    with open(gdd_path, "w", encoding="utf-8") as f:
        f.write(gdd_markdown)

    # ── Done ─────────────────────────────────────────────────────────────────
    print("\n" + "═" * 60)
    print("  ✅ PIPELINE COMPLETE")
    print("═" * 60)
    print(f"\n  📋 Game Design Document: {gdd_path}")
    print(f"  📁 Individual JSONs:     {OUTPUT_DIR}")
    print(f"\n  Generated at: {generated_at}")
    print("═" * 60 + "\n")


if __name__ == "__main__":
    asyncio.run(run_pipeline())
