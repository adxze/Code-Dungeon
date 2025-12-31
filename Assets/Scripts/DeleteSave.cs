using UnityEngine;

/// <summary>
/// Simple helper to clear saved level unlocks. Wire this to a UI button's OnClick.
/// </summary>
public class DeleteSave : MonoBehaviour
{
    public void ResetUnlockedLevels()
    {
        if (LevelProgressManager.Instance != null)
        {
            LevelProgressManager.Instance.ResetProgress();
        }
        else
        {
            // Fallback: delete all known unlock keys pattern, if manager isn't present.
            Debug.LogWarning("DeleteSave: LevelProgressManager not found. Ensure it exists to reset properly.");
        }
    }
}
