using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FlashOnShow : MonoBehaviour
{
    [Header("Panel induk (kosongkan = otomatis ambil induk langsung)")]
    [SerializeField] private CanvasGroup parentPanel;

    [Header("Kedip")]
    [SerializeField] private int blinkCount = 3;
    [SerializeField] private float onDuration = 0.08f;
    [SerializeField] private float offDuration = 0.08f;
    [Range(0f, 1f)] [SerializeField] private float maxAlpha = 1f;

    [Header("Setelah selesai kedip")]
    [Tooltip("0 = overlay hilang. 1 = overlay tetap menimpa panel.")]
    [Range(0f, 1f)] [SerializeField] private float finalAlpha = 0f;

    [Header("Jeda")]
    [SerializeField] private float delayAfterShown = 0f;

    private CanvasGroup group;
    private bool played;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;

        if (parentPanel == null && transform.parent != null)
            parentPanel = transform.parent.GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        // Mulai kedip begitu panel induk sudah muncul penuh (selesai slide)
        if (!played && parentPanel != null && parentPanel.alpha >= 0.99f)
        {
            played = true;
            StartCoroutine(Play());
        }
    }

    private IEnumerator Play()
    {
        if (delayAfterShown > 0f)
            yield return new WaitForSecondsRealtime(delayAfterShown);

        for (int i = 0; i < blinkCount; i++)
        {
            group.alpha = maxAlpha;
            yield return new WaitForSecondsRealtime(onDuration);
            group.alpha = 0f;
            yield return new WaitForSecondsRealtime(offDuration);
        }

        group.alpha = finalAlpha;
    }
}