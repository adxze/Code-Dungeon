using System.Collections;
using UnityEngine;

public class Fade : MonoBehaviour
{

    private CanvasGroup canvasGroup;
    public float from = 1f;
    public float to = 0f;
    public float duration = 1f;

    void Awake()
    {
        gameObject.SetActive(true);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = from;
        StartCoroutine(FadeInCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator FadeInCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
