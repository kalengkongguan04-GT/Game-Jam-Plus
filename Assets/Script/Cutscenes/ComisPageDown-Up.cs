using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ComicPage : MonoBehaviour
{
    // Arah asal panel sebelum bergerak ke posisi akhirnya.
    // Bottom = muncul dari bawah lalu naik ke atas.
    public enum SlideFrom { Bottom, Top, Left, Right }

    [System.Serializable]
    public class PanelStep
    {
        public CanvasGroup[] panels;

        [Tooltip("Jeda antar panel dalam step ini. 0 = muncul bersamaan.")]
        public float staggerDelay = 0f;

        [Tooltip("Dari mana panel datang. Bottom = dari bawah ke atas.")]
        public SlideFrom slideFrom = SlideFrom.Bottom;
    }

    [Header("Step (1 step = 1 klik)")]
    [SerializeField] private PanelStep[] steps;

    [Header("Animasi")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float slideDistance = 80f; // jarak geser (dalam pixel canvas)

    [Header("Opsi")]
    [SerializeField] private bool showFirstStepAutomatically = false;

    [Header("Event")]
    [SerializeField] private UnityEvent onPageFinished;

    private Vector2[][] finalPositions; // [step][panel]
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
            CanvasGroup[] group = steps[s].panels;
            finalPositions[s] = new Vector2[group.Length];

            for (int p = 0; p < group.Length; p++)
            {
                RectTransform rt = (RectTransform)group[p].transform;
                finalPositions[s][p] = rt.anchoredPosition; // posisi hasil atur di editor
                group[p].alpha = 0f;                        // sembunyikan semua panel
            }
        }
        initialized = true;
    }

    private void OnEnable()
    {
        enabledFrame = Time.frameCount;
    }

    private void Start()
    {
        if (showFirstStepAutomatically) Next();
    }

    private void Update()
    {
        if (!initialized) return;
        if (Time.frameCount == enabledFrame) return;

        if (WasClickedThisFrame())
            Next();
    }

    private bool WasClickedThisFrame()
    {
        if (Input.GetMouseButtonDown(0)) return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
        return false;
    }

    public void Next()
    {
        if (isAnimating)
        {
            FinishCurrentStep();
            return;
        }

        if (currentStep + 1 < steps.Length)
        {
            currentStep++;
            animRoutine = StartCoroutine(ShowStep(currentStep));
        }
        else
        {
            onPageFinished?.Invoke();
        }
    }

    // Offset awal: panel digeser ke arah ini dari posisi akhirnya
    private Vector2 GetStartOffset(SlideFrom from)
    {
        switch (from)
        {
            case SlideFrom.Bottom: return new Vector2(0f, -slideDistance); // mulai di bawah, naik ke atas
            case SlideFrom.Top:    return new Vector2(0f,  slideDistance);
            case SlideFrom.Left:   return new Vector2(-slideDistance, 0f);
            case SlideFrom.Right:  return new Vector2( slideDistance, 0f);
            default:               return Vector2.zero;
        }
    }

    private IEnumerator ShowStep(int stepIndex)
    {
        isAnimating = true;

        PanelStep step = steps[stepIndex];
        int count = step.panels.Length;
        Vector2 startOffset = GetStartOffset(step.slideFrom);

        float totalTime = (count - 1) * step.staggerDelay + fadeDuration;

        float elapsed = 0f;
        while (elapsed < totalTime)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int p = 0; p < count; p++)
            {
                float localT = elapsed - p * step.staggerDelay;
                float k = Mathf.Clamp01(localT / fadeDuration);
                k = 1f - (1f - k) * (1f - k); // ease-out

                Vector2 endPos = finalPositions[stepIndex][p];
                Vector2 startPos = endPos + startOffset;

                step.panels[p].alpha = k;
                ((RectTransform)step.panels[p].transform).anchoredPosition =
                    Vector2.Lerp(startPos, endPos, k);
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
                step.panels[p].alpha = 1f;
                ((RectTransform)step.panels[p].transform).anchoredPosition =
                    finalPositions[currentStep][p];
            }
        }
        isAnimating = false;
    }
}