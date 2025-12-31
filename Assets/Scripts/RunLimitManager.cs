using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Tracks how many times code can be run. When the limit is reached, shows a lose
/// canvas, marks the game over state, and blocks further runs. Also handles replaying
/// the current scene while skipping intro canvases on reload.
/// </summary>
public class RunLimitManager : MonoBehaviour
{
    public static RunLimitManager Instance { get; private set; }

    [Header("Run Limit")]
    [SerializeField] private int maxRuns = 3;
    private int currentRuns = 0;
    private bool triggerLoseOnRunComplete;

    [Header("Lose UI")]
    [SerializeField] private GameObject loseCanvas;
    [SerializeField] private GameMenuManager gameMenuManager;
    [SerializeField] private CodeGameController codeGameController;

    [Header("Intro Objects")]
    [Tooltip("Intro/tutorial objects that should be disabled if the level is restarted via Replay.")]
    [SerializeField] private GameObject[] introObjectsToDisableOnReplay;

    private const string SkipIntroKey = "SkipIntro_OnReload";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gameMenuManager == null)
        {
            gameMenuManager = FindObjectOfType<GameMenuManager>();
        }

        if (codeGameController == null)
        {
            codeGameController = FindObjectOfType<CodeGameController>();
        }
    }

    private void Start()
    {
        if (loseCanvas != null)
        {
            loseCanvas.SetActive(false);
        }

        ApplySkipIntroFlag();
    }

    /// <summary>
    /// Call before starting a new run. Increments the count and triggers loss when the limit is reached.
    /// Returns true if the run is allowed to proceed.
    /// </summary>
    public bool TryRegisterRun()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return false;
        }

        currentRuns++;

        if (currentRuns > maxRuns)
        {
            TriggerLose();
            return false;
        }

        triggerLoseOnRunComplete = currentRuns >= maxRuns;
        return true;
    }

    /// <summary>
    /// Call when a run finishes executing to trigger loss if the limit was hit.
    /// </summary>
    public void RunCompleted()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            triggerLoseOnRunComplete = false;
            return;
        }

        if (triggerLoseOnRunComplete)
        {
            TriggerLose();
        }

        triggerLoseOnRunComplete = false;
    }

    /// <summary>
    /// Displays lose UI and marks the game over state.
    /// </summary>
    public void TriggerLose()
    {
        triggerLoseOnRunComplete = false;
        GameManager.Instance?.SetGameOver();
        codeGameController?.AbortExecutionOnWin();
        gameMenuManager?.ForceCloseTerminal();

        if (loseCanvas != null)
        {
            loseCanvas.SetActive(true);
        }

        Debug.Log("RunLimitManager: run limit reached, showing lose screen.");
    }

    /// <summary>
    /// Button hook: reloads the current scene and skips intro objects on the next load.
    /// </summary>
    public void ReplayCurrentScene()
    {
        PlayerPrefs.SetInt(SkipIntroKey, 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    private void ApplySkipIntroFlag()
    {
        bool shouldSkipIntro = PlayerPrefs.GetInt(SkipIntroKey, 0) == 1;
        if (!shouldSkipIntro) return;

        foreach (var introObj in introObjectsToDisableOnReplay)
        {
            if (introObj != null)
            {
                introObj.SetActive(false);
            }
        }

        PlayerPrefs.DeleteKey(SkipIntroKey);
        PlayerPrefs.Save();

        EnsureEventSystemExists();
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null) return;

        var es = new GameObject("EventSystem (Auto)");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }
}
