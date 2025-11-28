using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChange : MonoBehaviour
{

    [Header("Fade Settings")]
    public CanvasGroup fadeBlack;   // Fullscreen black with CanvasGroup
    public float fadeDuration = 1f;

    private void Awake()
    {
        
    }

    private void Start()
    {
        if (fadeBlack != null)
        {
            fadeBlack.gameObject.SetActive(true);
        }
    }

    // -----------------------------
    // PUBLIC API
    // -----------------------------
    public void FadeToBlackAndLoad(string sceneName)
    {
        StartCoroutine(FadeThenLoad(sceneName));
    }

    // -----------------------------
    // FADE + LOAD
    // -----------------------------
    private IEnumerator FadeThenLoad(string sceneName)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeBlack.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        fadeBlack.alpha = 1;

        SceneManager.LoadScene(sceneName);
    }
}
