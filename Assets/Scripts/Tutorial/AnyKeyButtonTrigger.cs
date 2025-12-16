using UnityEngine;
using UnityEngine.UI;

public class AnyKeyButtonTrigger : MonoBehaviour
{
    public enum InputMode
    {
        AnyKey,
        SpecificKeys
    }

    [Header("Input Settings")]
    public InputMode inputMode = InputMode.AnyKey;

    [Tooltip("Used only when InputMode = SpecificKeys")]
    public KeyCode[] allowedKeys;

    [Header("Button")]
    [SerializeField] private Button targetButton;

    private bool triggered = false;

    void Update()
    {
        if (IsInputTriggered())
        {
            triggered = true;
            targetButton.onClick.Invoke();
        }
    }

    bool IsInputTriggered()
    {
        if (inputMode == InputMode.AnyKey)
            return Input.anyKeyDown;

        if (inputMode == InputMode.SpecificKeys)
        {
            foreach (KeyCode key in allowedKeys)
            {
                if (Input.GetKeyDown(key))
                    return true;
            }
        }

        return false;
    }
}
