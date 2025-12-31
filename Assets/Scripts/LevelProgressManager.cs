using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks which levels are unlocked and toggles level buttons/entries in the UI.
/// Uses PlayerPrefs to persist unlock state between sessions.
/// </summary>
public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }

    private const string PrefKeyPrefix = "LevelUnlocked_";

    [Header("Default Unlocked Levels")]
    [SerializeField] private List<string> defaultUnlockedLevels = new() { "Level1" };

    [Serializable]
    public struct LevelUIEntry
    {
        public string levelId;
        public GameObject levelObject;
    }

    [Header("Level UI")]
    [Tooltip("Assign level buttons/entries here so they can be shown/hidden automatically.")]
    [SerializeField] private List<LevelUIEntry> levelEntries = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureDefaultUnlocked();
    }

    private void Start()
    {
        RefreshUI();
    }

    public bool IsUnlocked(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId)) return false;

        int defaultValue = defaultUnlockedLevels.Contains(levelId) ? 1 : 0;
        return PlayerPrefs.GetInt(GetPrefKey(levelId), defaultValue) == 1;
    }

    public void UnlockLevel(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            Debug.LogWarning("LevelProgressManager: levelId is empty, cannot unlock.");
            return;
        }

        if (IsUnlocked(levelId))
        {
            return;
        }

        PlayerPrefs.SetInt(GetPrefKey(levelId), 1);
        PlayerPrefs.Save();
        RefreshUI();
        Debug.Log($"Level unlocked: {levelId}");
    }

    public void RefreshUI()
    {
        foreach (var entry in levelEntries)
        {
            if (entry.levelObject == null) continue;
            bool unlocked = IsUnlocked(entry.levelId);
            entry.levelObject.SetActive(unlocked);
        }
    }

    public void ResetProgress()
    {
        // Clear saved unlocks for known levels
        foreach (var entry in levelEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.levelId)) continue;
            PlayerPrefs.DeleteKey(GetPrefKey(entry.levelId));
        }
        foreach (var levelId in defaultUnlockedLevels)
        {
            if (string.IsNullOrWhiteSpace(levelId)) continue;
            PlayerPrefs.DeleteKey(GetPrefKey(levelId));
        }

        PlayerPrefs.Save();
        EnsureDefaultUnlocked();
        RefreshUI();
        Debug.Log("LevelProgressManager: progress reset and defaults reapplied.");
    }

    private void EnsureDefaultUnlocked()
    {
        foreach (var levelId in defaultUnlockedLevels)
        {
            if (string.IsNullOrWhiteSpace(levelId)) continue;
            PlayerPrefs.SetInt(GetPrefKey(levelId), 1);
        }
        PlayerPrefs.Save();
    }

    private string GetPrefKey(string levelId)
    {
        return $"{PrefKeyPrefix}{levelId}";
    }
}
