using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class CreditsScroll : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;   // isi: CreditsPanel
    [SerializeField] private TMP_Text creditsText;     // isi: CreditsText
    [SerializeField] private float speed = 60f;
    [SerializeField] private float fastMultiplier = 4f;
    [Tooltip("Dipanggil saat credits sudah habis bergulir")]
    [SerializeField] private UnityEvent onFinished;

    private RectTransform content;
    private bool finished;

    private void OnEnable()
    {
        if (creditsText == null || viewport == null) return;

        content = creditsText.rectTransform;
        finished = false;

        // Mulai dari bawah layar
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, -viewport.rect.height);
    }

    private void Update()
    {
        if (content == null || finished) return;

        bool fast = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
        float s = speed * (fast ? fastMultiplier : 1f);

        content.anchoredPosition += new Vector2(0f, s * Time.unscaledDeltaTime);

        // Selesai kalau teks sudah lewat batas atas layar
        if (content.anchoredPosition.y > creditsText.preferredHeight)
        {
            finished = true;
            onFinished?.Invoke();
        }
    }
}