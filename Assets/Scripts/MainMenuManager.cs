using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button optionsButton;
    public Button exitButton;
    public Button backButton;

    [Header("Menus")]
    public GameObject mainMenu;
    public GameObject optionsMenu;

    [Header("Scene")]
    public string gameSceneName = "GameScene";

    [Header("Transition")]
    public float transitionDuration = 1f;
    public float slideDistance = 3000f;

    [Header("Fade To Black")]
    public CanvasGroup fadeBlack; // a full-screen black Image with CanvasGroup

    private CanvasGroup mainGroup;
    private CanvasGroup optionsGroup;

    private void Start()
    {
        mainGroup = mainMenu.GetComponent<CanvasGroup>();
        optionsGroup = optionsMenu.GetComponent<CanvasGroup>();

        // Ensure correct states
        fadeBlack.gameObject.SetActive(true);
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
        fadeBlack.alpha = 0;

        startButton.onClick.AddListener(OnStartButtonClicked);
        optionsButton.onClick.AddListener(OnOptionsButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    // --------------------------
    // MENU BUTTONS
    // --------------------------
    private void OnStartButtonClicked()
    {
        StartCoroutine(FadeToBlackThenLoad());
    }

    private void OnOptionsButtonClicked()
    {
        StartCoroutine(SlideTransition(mainGroup, optionsGroup));
    }

    private void OnBackButtonClicked()
    {
        StartCoroutine(SlideTransition(optionsGroup, mainGroup));
    }

    private void OnExitButtonClicked()
    {
        Application.Quit();
    }

    // --------------------------
    // SLIDE + FADE TRANSITION
    // --------------------------
    private IEnumerator SlideTransition(CanvasGroup from, CanvasGroup to)
    {
        float time = 0f;

        RectTransform fromRect = from.GetComponent<RectTransform>();
        RectTransform toRect = to.GetComponent<RectTransform>();

        Vector2 fromStart = Vector2.zero;
        Vector2 fromEnd = new Vector2(slideDistance, 0);

        Vector2 toStart = new Vector2(-slideDistance, 0);
        Vector2 toEnd = Vector2.zero;

        // prepare "to"
        to.gameObject.SetActive(true);
        to.alpha = 0;
        toRect.anchoredPosition = toStart;

        while (time < transitionDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / transitionDuration);

            from.alpha = 1 - t;
            to.alpha = t;

            fromRect.anchoredPosition = Vector2.Lerp(fromStart, fromEnd, t);
            toRect.anchoredPosition = Vector2.Lerp(toStart, toEnd, t);

            yield return null;
        }

        // finalize
        from.alpha = 0;
        fromRect.anchoredPosition = fromEnd;
        from.gameObject.SetActive(false);

        to.alpha = 1;
        toRect.anchoredPosition = toEnd;
    }

    // --------------------------
    // FADE TO BLACK + LOAD SCENE
    // --------------------------
    private IEnumerator FadeToBlackThenLoad()
    {
        fadeBlack.gameObject.SetActive(true);

        float time = 0f;

        while (time < transitionDuration)
        {
            time += Time.deltaTime;
            fadeBlack.alpha = Mathf.Clamp01(time / transitionDuration);
            yield return null;
        }

        fadeBlack.alpha = 1;

        SceneManager.LoadScene(gameSceneName);
    }
}
