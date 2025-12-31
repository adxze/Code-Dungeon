using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles Game Over UI navigation. Expose the target scene names in the inspector,
/// then wire up the buttons to call GoToNextStage() or GoBack().
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string nextStageSceneName;
    [SerializeField] private string backSceneName;

    public void GoToNextStage()
    {
        LoadSceneByName(nextStageSceneName);
    }

    public void GoBack()
    {
        LoadSceneByName(backSceneName);
    }

    // Use this from a Unity Button OnClick and pass the scene name argument directly
    public void LoadScene(string sceneName)
    {
        LoadSceneByName(sceneName);
    }

    private void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("GameOverUI: Scene name is empty. Assign it in the inspector.");
            return;
        }

        // Ensure game resumes in case timeScale was paused.
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
