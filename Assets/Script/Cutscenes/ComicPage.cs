using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ComicPage : MonoBehaviour
{
    public enum SlideFrom { Bottom, Top, Left, Right }

    [System.Serializable]
    public class PanelEntry
    {
        public CanvasGroup panel;
        public SlideFrom slideFrom = SlideFrom.Bottom;
        [Tooltip("Jarak geser panel ini. 0 = pakai Slide Distance umum.")]
        public float customDistance = 0f;
    }

    [System.Serializable]
    public class PanelStep
    {
        public PanelEntry[] panels;
        [Tooltip("Jeda antar panel dalam step ini. 0 = muncul bersamaan.")]
        public float staggerDelay = 0f;
        public UnityEvent onStepStart;   // BARU: dipanggil saat step ini dimulai
    }

    [Header("Step (1 step = 1 klik)")]
    [SerializeField] private PanelStep[] steps;

    [Header("Animasi")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float slideDistance = 80f;

    [Header("Opsi")]
    [SerializeField] private bool showFirstStepAutomatically = false;

    [Header("Event")]
    [SerializeField] private UnityEvent onPageFinished;

    private Vector2[][] finalPositions;
    private int currentStep = -1;
    private bool isAnimating;
    private bool initialized;
    private int enabledFrame;
    private Coroutine animRoutine;

    private void Awake()
    {
        finalPositions = new Vector2[steps.Length][];
        for (int s = 0; s < steps.Length; s++)
        {
            PanelEntry[] list = steps[s].panels;
            finalPositions[s] = new Vector2[list.Length];
            for (int p = 0; p < list.Length; p++)
            {
                RectTransform rt = (RectTransform)list[p].panel.transform;
                finalPositions[s][p] = rt.anchoredPosition;
                list[p].panel.alpha = 0f;
            }
        }
        initialized = true;
    }

    private void OnEnable() { enabledFrame = Time.frameCount; }

    private void Start()
    {
        if (showFirstStepAutomatically) Next();
    }

    private void Update()
    {
        if (!initialized) return;
        if (Time.frameCount == enabledFrame) return;
        if (WasClickedThisFrame()) Next();
    }

    private bool WasClickedThisFrame()
    {
        if (Input.GetMouseButtonDown(0)) return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
        return false;
    }

    public void Next()
    {
        if (isAnimating) { FinishCurrentStep(); return; }

        if (currentStep + 1 < steps.Length)
        {
            currentStep++;
            steps[currentStep].onStepStart?.Invoke();   // BARU: picu event step
            animRoutine = StartCoroutine(ShowStep(currentStep));
        }
        else
        {
            onPageFinished?.Invoke();
        }
    }

    private Vector2 GetStartOffset(SlideFrom from, float customDistance)
    {
        float d = customDistance > 0f ? customDistance : slideDistance;
        switch (from)
        {
            case SlideFrom.Bottom: return new Vector2(0f, -d);
            case SlideFrom.Top:    return new Vector2(0f,  d);
            case SlideFrom.Left:   return new Vector2(-d, 0f);
            case SlideFrom.Right:  return new Vector2( d, 0f);
            default:               return Vector2.zero;
        }
    }

    private IEnumerator ShowStep(int stepIndex)
    {
        isAnimating = true;
        PanelStep step = steps[stepIndex];
        int count = step.panels.Length;
        float totalTime = (count - 1) * step.staggerDelay + fadeDuration;

        float elapsed = 0f;
        while (elapsed < totalTime)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int p = 0; p < count; p++)
            {
                PanelEntry entry = step.panels[p];
                float localT = elapsed - p * step.staggerDelay;
                float k = Mathf.Clamp01(localT / fadeDuration);
                k = 1f - (1f - k) * (1f - k);

                Vector2 endPos = finalPositions[stepIndex][p];
                Vector2 startPos = endPos + GetStartOffset(entry.slideFrom, entry.customDistance);

                entry.panel.alpha = k;
                ((RectTransform)entry.panel.transform).anchoredPosition = Vector2.Lerp(startPos, endPos, k);
            }
            yield return null;
        }
        FinishCurrentStep();
    }

    private void FinishCurrentStep()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        if (currentStep >= 0)
        {
            PanelStep step = steps[currentStep];
            for (int p = 0; p < step.panels.Length; p++)
            {
                step.panels[p].panel.alpha = 1f;
                ((RectTransform)step.panels[p].panel.transform).anchoredPosition = finalPositions[currentStep][p];
            }
        }
        isAnimating = false;
    }
}