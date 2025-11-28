using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenu;
    public GameObject terminalUI;

    [Header("Transition Settings")]
    public float transitionDuration = 0.5f;
    public float slideDistance = 1000f;

    private CanvasGroup menuGroup;
    private CanvasGroup terminalGroup;
    private RectTransform menuRect;
    private RectTransform terminalRect;

    private Vector2 menuStartPos;
    private Vector2 terminalStartPos;

    [Header("Input Actions")]
    public InputActionReference openMenuAction;
    public InputActionReference openTerminalAction;

    private void Awake()
    {
        menuGroup = mainMenu.GetComponent<CanvasGroup>();
        terminalGroup = terminalUI.GetComponent<CanvasGroup>();
        menuRect = mainMenu.GetComponent<RectTransform>();
        terminalRect = terminalUI.GetComponent<RectTransform>();

        // Record the starting positions
        menuStartPos = menuRect.anchoredPosition;
        terminalStartPos = terminalRect.anchoredPosition;

        // Start closed
        mainMenu.SetActive(false);
        terminalUI.SetActive(false);
        menuGroup.alpha = 0;
        terminalGroup.alpha = 0;
    }

    private void OnEnable()
    {
        openMenuAction.action.performed += _ => ToggleMenu();
        openTerminalAction.action.performed += _ => ToggleTerminal();
    }

    private void OnDisable()
    {
        openMenuAction.action.performed -= _ => ToggleMenu();
        openTerminalAction.action.performed -= _ => ToggleTerminal();
    }

    private void ToggleMenu()
    {
        if (terminalUI.activeSelf)
        {
            StartCoroutine(SwitchUI(terminalGroup, terminalRect, terminalUI, menuGroup, menuRect, mainMenu, terminalStartPos, menuStartPos));
        }
        else
        {
            if (!mainMenu.activeSelf)
                StartCoroutine(SlideIn(menuGroup, menuRect, mainMenu, menuStartPos));
            else
                StartCoroutine(SlideOut(menuGroup, menuRect, mainMenu, menuStartPos));
        }
    }

    private void ToggleTerminal()
    {
        if (mainMenu.activeSelf)
        {
            return;
        }
        else
        {
            if (!terminalUI.activeSelf)
                StartCoroutine(SlideIn(terminalGroup, terminalRect, terminalUI, terminalStartPos));
            else
                StartCoroutine(SlideOut(terminalGroup, terminalRect, terminalUI, terminalStartPos));
        }
    }

    private IEnumerator SwitchUI(CanvasGroup fromGroup, RectTransform fromRect, GameObject fromGO,
                                 CanvasGroup toGroup, RectTransform toRect, GameObject toGO,
                                 Vector2 fromPos, Vector2 toPos)
    {
        yield return StartCoroutine(SlideOut(fromGroup, fromRect, fromGO, fromPos));
        yield return StartCoroutine(SlideIn(toGroup, toRect, toGO, toPos));
    }

    private IEnumerator SlideIn(CanvasGroup group, RectTransform rect, GameObject go, Vector2 targetPos)
    {
        go.SetActive(true);
        group.alpha = 0;
        float time = 0f;

        Vector2 startPos = targetPos + new Vector2(-slideDistance, 0); // start left of original
        rect.anchoredPosition = startPos;

        while (time < transitionDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / transitionDuration);

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            group.alpha = t;
            yield return null;
        }

        rect.anchoredPosition = targetPos;
        group.alpha = 1f;
    }

    private IEnumerator SlideOut(CanvasGroup group, RectTransform rect, GameObject go, Vector2 targetPos)
    {
        float time = 0f;
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = targetPos + new Vector2(-slideDistance, 0); // slide left relative to original

        while (time < transitionDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / transitionDuration);

            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            group.alpha = 1 - t;
            yield return null;
        }

        rect.anchoredPosition = endPos;
        group.alpha = 0f;
        go.SetActive(false);
    }
}
