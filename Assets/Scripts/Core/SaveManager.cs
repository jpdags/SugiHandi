// SaveManager.cs — Handles all local save/load for SugiHandi demo.
//
// Method: JSON file at Application.persistentDataPath/sugihandi_save.json
// Justification over PlayerPrefs: JSON gives us a structured, inspectable file
// that survives app updates and is easy to wipe during testing.
//
// Usage:
//   SaveManager.Instance.Save(data);
//   SaveData data = SaveManager.Instance.Load();
//   SaveManager.Instance.DeleteSave();

using System;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_FILE_NAME = "sugihandi_save.json";
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Serialize the given SaveData to JSON and write to persistent storage.
    /// Called by ProgressTracker whenever state changes.
    /// </summary>
    public void Save(SaveData data)
    {
        data.lastSavedAt = DateTime.UtcNow.ToString("o");
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"[SaveManager] Saved to: {SaveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
        }
    }

    /// <summary>
    /// Load and deserialize SaveData from disk.
    /// Returns a new empty SaveData if no save file exists yet.
    /// </summary>
    public SaveData Load()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("[SaveManager] No save file found — returning fresh SaveData.");
            return new SaveData();
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveManager] Loaded save from: {SaveFilePath}");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load save: {e.Message} — returning fresh SaveData.");
            return new SaveData();
        }
    }

    /// <summary>
    /// Delete the save file. Use for testing or "new game" flow.
    /// </summary>
    public void DeleteSave()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("[SaveManager] Save file deleted.");
        }
    }

    /// <summary>
    /// Returns true if a save file exists on disk.
    /// </summary>
    public bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }
}
