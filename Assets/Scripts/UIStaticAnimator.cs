using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIStaticAnimator : MonoBehaviour
{
    [Header("General")]
    public bool useUnscaledTime = true;

    // --- ROTATE ---
    [Header("Rotate")]
    public bool enableRotate = false;
    public float rotateSpeed = 90f; // degrees per second

    // --- PULSE (SCALE + ALPHA) ---
    [Header("Pulse (Scale + Alpha)")]
    public bool enablePulse = false;
    public float pulseDuration = 1f;         // full cycle
    public float pulseScaleMultiplier = 1.2f;
    [Range(0f, 1f)] public float pulseMinAlpha = 0.2f;
    [Range(0f, 1f)] public float pulseMaxAlpha = 1f;

    // --- SHAKE ---
    [Header("Shake (Position)")]
    public bool enableShake = false;
    public float shakeAmplitude = 5f;        // pixels
    public float shakeFrequency = 25f;       // times per second

    // --- MOVE HORIZONTAL ---
    [Header("Move Horizontal (Left-Right)")]
    public bool enableMoveHorizontal = false;
    public float moveHDistance = 20f;        // pixels
    public float moveHDuration = 1.5f;       // full cycle

    // --- MOVE VERTICAL ---
    [Header("Move Vertical (Up-Down)")]
    public bool enableMoveVertical = false;
    public float moveVDistance = 20f;        // pixels
    public float moveVDuration = 1.5f;       // full cycle

    // --- GLOW PULSE ---
    [Header("Glow Pulse (Alpha Multiplier)")]
    public bool enableGlowPulse = false;
    public float glowPulseDuration = 1.2f;
    [Range(1f, 4f)] public float glowMaxMultiplier = 1.3f;

    // --- COLOR SHIFT ---
    [Header("Color Shift (Graphic Only)")]
    public bool enableColorShift = false;
    public Color colorShiftA = Color.white;
    public Color colorShiftB = Color.cyan;
    public float colorShiftDuration = 2f;

    // --- HEARTBEAT ---
    [Header("Heartbeat (Double-Beat Scale)")]
    public bool enableHeartbeat = false;
    public float heartbeatDuration = 0.8f;
    public float heartbeatScaleMultiplier = 1.1f;

    // --- RIPPLE ---
    [Header("Ripple (Scale Up + Fade Out)")]
    public bool enableRipple = false;
    public float rippleDuration = 1f;
    public float rippleMaxScaleMultiplier = 1.5f;
    [Range(0f, 1f)] public float rippleMinAlpha = 0f;

    // --- ORBIT ---
    [Header("Orbit (Circular Motion)")]
    public bool enableOrbit = false;
    public enum OrbitCenterMode { Self, Custom }
    public OrbitCenterMode orbitCenterMode = OrbitCenterMode.Self;

    public Transform orbitCustomCenter;
    public float orbitRadius = 30f; 
    public float orbitSpeed = 90f;   // degrees per sec
    public float orbitAngleOffset = 0f;

    // --- Internal cache ---
    RectTransform _rect;
    CanvasGroup _canvasGroup;
    Graphic _graphic;

    Vector3 _baseScale;
    Vector2 _baseAnchoredPos;
    float _baseEulerZ;
    float _baseAlpha = 1f;
    Color _baseColor = Color.white;

    float _orbitAngle = 0f;
    Vector2 _orbitBaseCenter;

    // timers
    float _pulseTime;
    float _moveHTime;
    float _moveVTime;
    float _shakeTime;
    float _rotateAngle;

    float _glowTime;
    float _colorShiftTime;
    float _heartbeatTime;
    float _rippleTime;

    Vector2 _lastShakeOffset = Vector2.zero;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _graphic = GetComponent<Graphic>();

        CacheBaseState();
    }

    void OnEnable()
    {
        // reset timers when re-enabled
        _pulseTime = _moveHTime = _moveVTime = _shakeTime = 0f;
        _rotateAngle = 0f;
        _glowTime = _colorShiftTime = _heartbeatTime = _rippleTime = 0f;
        _lastShakeOffset = Vector2.zero;


        _orbitAngle = orbitAngleOffset;
        if (orbitCenterMode == OrbitCenterMode.Self)
        {
            _orbitBaseCenter = _rect.anchoredPosition;
        }
        CacheBaseState();
    }

    public void CacheBaseState()
    {
        if (!_rect) _rect = GetComponent<RectTransform>();
        if (!_canvasGroup) _canvasGroup = GetComponent<CanvasGroup>();
        if (!_graphic) _graphic = GetComponent<Graphic>();

        _baseScale = _rect.localScale;
        _baseAnchoredPos = _rect.anchoredPosition;
        _baseEulerZ = _rect.localEulerAngles.z;

        if (_graphic)
        {
            _baseColor = _graphic.color;
            _baseAlpha = _graphic.color.a;
        }
        else
        {
            _baseColor = Color.white;
            if (_canvasGroup)
                _baseAlpha = _canvasGroup.alpha;
            else
                _baseAlpha = 1f;
        }
    }

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // start from base every frame
        Vector3 finalScale = _baseScale;
        Vector2 finalPos = _baseAnchoredPos;
        float finalEulerZ = _baseEulerZ;
        float finalAlpha = _baseAlpha;
        Color finalRgbColor = _baseColor;
        finalRgbColor.a = 1f; // we’ll set alpha separately at the end

        // --- ROTATE (continuous spin) ---
        if (enableRotate)
        {
            _rotateAngle += rotateSpeed * dt;
            finalEulerZ = _baseEulerZ + _rotateAngle;
        }

        // --- PULSE (scale + alpha) ---
        if (enablePulse && pulseDuration > 0f)
        {
            _pulseTime += dt;
            float t = (_pulseTime / pulseDuration) * Mathf.PI * 2f;
            float k = (Mathf.Sin(t) + 1f) * 0.5f; // 0 → 1 → 0

            float scaleFactor = Mathf.Lerp(1f, pulseScaleMultiplier, k);
            finalScale = _baseScale * scaleFactor;

            float alpha = Mathf.Lerp(pulseMaxAlpha, pulseMinAlpha, k);
            finalAlpha = alpha;
        }

        // --- MOVE HORIZONTAL (left-right) ---
        if (enableMoveHorizontal && moveHDuration > 0f)
        {
            _moveHTime += dt;
            float t = (_moveHTime / moveHDuration) * Mathf.PI * 2f;
            float offset = Mathf.Sin(t) * moveHDistance;
            finalPos.x = _baseAnchoredPos.x + offset;
        }

        // --- MOVE VERTICAL (up-down) ---
        if (enableMoveVertical && moveVDuration > 0f)
        {
            _moveVTime += dt;
            float t = (_moveVTime / moveVDuration) * Mathf.PI * 2f;
            float offset = Mathf.Sin(t) * moveVDistance;
            finalPos.y = _baseAnchoredPos.y + offset;
        }

        // --- HEARTBEAT (double-beat scale) ---
        if (enableHeartbeat && heartbeatDuration > 0f)
        {
            _heartbeatTime += dt;
            float n = (_heartbeatTime / heartbeatDuration) % 1f;

            float beat = 0f;
            if (n < 0.3f)
            {
                // first strong beat
                beat = Mathf.Sin((n / 0.3f) * Mathf.PI);
            }
            else if (n < 0.6f)
            {
                // second weaker beat
                float t = (n - 0.3f) / 0.3f;
                beat = Mathf.Sin(t * Mathf.PI) * 0.8f;
            }
            // else rest (0)

            beat = Mathf.Clamp01(beat);
            float hbScale = Mathf.Lerp(1f, heartbeatScaleMultiplier, beat);

            // multiply on top of whatever scale we already have
            finalScale *= hbScale;
        }

        // --- RIPPLE (scale up + fade out loop) ---
        if (enableRipple && rippleDuration > 0f)
        {
            _rippleTime += dt;
            float r = (_rippleTime % rippleDuration) / rippleDuration; // 0..1

            float rippleScale = Mathf.Lerp(1f, rippleMaxScaleMultiplier, r);
            float rippleAlpha = Mathf.Lerp(1f, rippleMinAlpha, r);

            finalScale *= rippleScale;
            finalAlpha *= rippleAlpha;
        }

        // --- GLOW PULSE (alpha multiplier) ---
        if (enableGlowPulse && glowPulseDuration > 0f)
        {
            _glowTime += dt;
            float t = (_glowTime / glowPulseDuration) * Mathf.PI * 2f;
            float k = (Mathf.Sin(t) + 1f) * 0.5f; // 0..1
            float mult = Mathf.Lerp(1f, glowMaxMultiplier, k);

            finalAlpha *= mult;
            finalAlpha = Mathf.Clamp01(finalAlpha);
        }

        // --- COLOR SHIFT (only affects RGB, not alpha) ---
        if (enableColorShift && _graphic && colorShiftDuration > 0f)
        {
            _colorShiftTime += dt;
            float t = (_colorShiftTime / colorShiftDuration) * Mathf.PI * 2f;
            float k = (Mathf.Sin(t) + 1f) * 0.5f; // 0..1

            Color c = Color.Lerp(colorShiftA, colorShiftB, k);
            finalRgbColor.r = c.r;
            finalRgbColor.g = c.g;
            finalRgbColor.b = c.b;
        }

        // --- SHAKE (random jitter on top) ---
        if (enableShake && shakeAmplitude > 0f && shakeFrequency > 0f)
        {
            _shakeTime += dt;
            float step = 1f / shakeFrequency;

            if (_shakeTime >= step)
            {
                _shakeTime -= step;
                _lastShakeOffset = new Vector2(
                    Random.Range(-shakeAmplitude, shakeAmplitude),
                    Random.Range(-shakeAmplitude, shakeAmplitude)
                );
            }

            finalPos += _lastShakeOffset;
        }

        // --- ORBIT (circular around center) ---
        if (enableOrbit)
        {
            _orbitAngle += orbitSpeed * dt;
            float rad = _orbitAngle * Mathf.Deg2Rad;

            Vector2 center =
                orbitCenterMode == OrbitCenterMode.Self
                ? _orbitBaseCenter
                : (orbitCustomCenter ? (Vector2)orbitCustomCenter.position : _orbitBaseCenter);

            // If using anchors: convert world pos to anchored
            if (orbitCenterMode == OrbitCenterMode.Custom && orbitCustomCenter)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect.parent as RectTransform,
                    orbitCustomCenter.position,
                    null,
                    out center
                );
            }

            Vector2 orbitPos = new Vector2(
                center.x + Mathf.Cos(rad) * orbitRadius,
                center.y + Mathf.Sin(rad) * orbitRadius
            );

            finalPos = orbitPos;
        }


        // --- APPLY TRANSFORMS ---
        _rect.localScale = finalScale;
        _rect.anchoredPosition = finalPos;
        Vector3 euler = _rect.localEulerAngles;
        euler.z = finalEulerZ;
        _rect.localEulerAngles = euler;

        // --- APPLY COLOR / ALPHA ---
        finalAlpha = Mathf.Clamp01(finalAlpha);

        if (_canvasGroup)
        {
            _canvasGroup.alpha = finalAlpha;
        }

        if (_graphic)
        {
            Color c = finalRgbColor;
            c.a = finalAlpha;
            _graphic.color = c;
        }
    }
}
