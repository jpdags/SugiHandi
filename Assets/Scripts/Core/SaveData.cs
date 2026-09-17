// SaveData.cs — Serializable save data container.
// Stored as JSON at Application.persistentDataPath/sugihandi_save.json
// This is the ONLY thing written to disk — one flat file, simple for a demo build.

using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // ── Kodeks ───────────────────────────────────────────────────────────────
    /// <summary>Keys of Butuanon words the player has encountered.
    /// Each key maps to a KodeksEntryData ScriptableObject via Resources.Load.</summary>
    public List<string> loggedKodeksWordKeys = new List<string>();

    // ── Quest ────────────────────────────────────────────────────────────────
    /// <summary>The ID of the active (or last completed) quest.</summary>
    public string activeQuestId = "";

    /// <summary>Index of the furthest completed quest step (0-based).
    /// -1 means quest not yet accepted.</summary>
    public int completedQuestStepIndex = -1;

    /// <summary>Whether the quest has been fully completed this session.</summary>
    public bool questCompleted = false;

    // ── Artifacts ────────────────────────────────────────────────────────────
    /// <summary>IDs of artifacts the player has examined.</summary>
    public List<string> examinedArtifactIds = new List<string>();

    // ── Puzzle ───────────────────────────────────────────────────────────────
    /// <summary>IDs of environmental puzzles the player has completed.</summary>
    public List<string> completedPuzzleIds = new List<string>();

    // ── Meta ─────────────────────────────────────────────────────────────────
    /// <summary>Timestamp of last save (ISO 8601).</summary>
    public string lastSavedAt = "";
}
