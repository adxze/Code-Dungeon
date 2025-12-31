using System;
using UnityEngine;

public class ChestWin : MonoBehaviour
{
    [SerializeField] private CodeGameController codeGameController;
    [SerializeField] private GameMenuManager gameMenuManager;
    [SerializeField] private GameObject winCanvas;
    [Header("Level Unlocking")]
    [SerializeField] private string levelIdToUnlock;
    private bool hasTriggeredWin;

    void Awake()
    {
        if (gameMenuManager == null)
        {
            gameMenuManager = FindObjectOfType<GameMenuManager>();
        }

        if (codeGameController == null)
        {
            codeGameController = FindObjectOfType<CodeGameController>();
        }
    }

    void Start()
    {
        if (winCanvas != null)
        {
            winCanvas.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggeredWin)
        {
            return;
        }

        if (other.gameObject.CompareTag("Player"))
        {
            hasTriggeredWin = true;

            if (winCanvas != null)
            {
                winCanvas.SetActive(true);
            }

            if (gameMenuManager != null)
            {
                gameMenuManager.ForceCloseTerminal();
            }

            if (codeGameController != null)
            {
                codeGameController.AbortExecutionOnWin();
            }

            if (!string.IsNullOrWhiteSpace(levelIdToUnlock) && LevelProgressManager.Instance != null)
            {
                LevelProgressManager.Instance.UnlockLevel(levelIdToUnlock);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameOver();
            }

            // Time.timeScale = 0f;
            // freezing the entire thing is bad / not that good
            Debug.Log("The Player WIN!");
        }
    }

}
