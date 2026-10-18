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
        [Tooltip("Jeda antar panel (detik). Isi kecil, misal 0 sampai 0.3.")]
        public float staggerDelay = 0f;
        [Tooltip("Suara khusus step ini. Kosong = pakai Panel Sound default.")]
        public AudioClip sound;
        public UnityEvent onStepStart;
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
            if (list == null) list = new PanelEntry[0];
            finalPositions[s] = new Vector2[list.Length];

            for (int p = 0; p < list.Length; p++)
            {
                if (list[p].panel == null)
                {
                    Debug.LogWarning($"[{name}] Step {s}, Panel {p} masih None. Isi di Inspector.", this);
                    continue;
                }
                RectTransform rt = (RectTransform)list[p].panel.transform;
                finalPositions[s][p] = rt.anchoredPosition;
                list[p].panel.alpha = 0f;
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
        if (isAnimating)
        {
            FinishCurrentStep();
            return;
        }

        if (currentStep + 1 < steps.Length)
        {
            currentStep++;
            steps[currentStep].onStepStart?.Invoke();

            // Suara tiap panel baru, kecuali step pertama
            if (currentStep > 0 && AudioManager.Instance != null)
            {
                AudioClip s = steps[currentStep].sound;
                if (s != null) AudioManager.Instance.PlaySfx(s);
                else AudioManager.Instance.PlayPanel();
            }

            animRoutine = StartCoroutine(ShowStep(currentStep));
        }
        else
        {
            FinishPage();
        }
    }

    private void FinishPage()
    {
        if (onPageFinished != null && onPageFinished.GetPersistentEventCount() > 0)
        {
            onPageFinished.Invoke();
        }
        else
        {
            ComicManager manager = FindObjectOfType<ComicManager>();
            if (manager != null) manager.NextPage();
            else Debug.LogWarning($"[{name}] On Page Finished kosong dan ComicManager tidak ditemukan.");
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
        PanelStep step = steps[stepIndex];
        int count = step.panels != null ? step.panels.Length : 0;

        if (count == 0)
        {
            isAnimating = false;
            yield break;
        }

        isAnimating = true;
        float stagger = Mathf.Max(0f, step.staggerDelay);
        float totalTime = (count - 1) * stagger + fadeDuration;

        float elapsed = 0f;
        while (elapsed < totalTime)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int p = 0; p < count; p++)
            {
                PanelEntry entry = step.panels[p];
                if (entry.panel == null) continue;

                float localT = elapsed - p * stagger;
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

        if (currentStep >= 0 && currentStep < steps.Length)
        {
            PanelStep step = steps[currentStep];
            int count = step.panels != null ? step.panels.Length : 0;
            for (int p = 0; p < count; p++)
            {
                if (step.panels[p].panel == null) continue;
                step.panels[p].panel.alpha = 1f;
                ((RectTransform)step.panels[p].panel.transform).anchoredPosition = finalPositions[currentStep][p];
            }
        }
        isAnimating = false;
    }
}