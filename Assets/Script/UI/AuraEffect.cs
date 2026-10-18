using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class AuraEffect : MonoBehaviour
{
    [Header("Naik dari bawah (berulang)")]
    [SerializeField] private float riseDistance = 60f;   // pixel
    [SerializeField] private float riseSpeed = 0.4f;     // siklus per detik

    [Header("Denyut terang redup")]
    [Range(0f, 1f)] [SerializeField] private float minAlpha = 0.45f;
    [Range(0f, 1f)] [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float pulseSpeed = 1.6f;

    [Header("Membesar mengecil")]
    [SerializeField] private float scaleAmount = 0.04f;
    [SerializeField] private float scaleSpeed = 1.2f;

    [Header("Goyang samping")]
    [SerializeField] private float swayAmount = 6f;
    [SerializeField] private float swaySpeed = 0.9f;

    private RectTransform rt;
    private CanvasGroup group;
    private Vector2 startPos;
    private Vector3 startScale;

    private void Awake()
    {
        rt = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        startPos = rt.anchoredPosition;
        startScale = rt.localScale;
    }

    private void Update()
    {
        float t = Time.unscaledTime;

        // Naik dari bawah, lalu mulai lagi (0 sampai 1)
        float cycle = Mathf.Repeat(t * riseSpeed, 1f);
        float y = Mathf.Lerp(-riseDistance, 0f, cycle);

        float x = Mathf.Sin(t * swaySpeed) * swayAmount;
        rt.anchoredPosition = startPos + new Vector2(x, y);

        float s = 1f + Mathf.Sin(t * scaleSpeed) * scaleAmount;
        rt.localScale = startScale * s;

        // Denyut, ditambah fade di awal dan akhir siklus supaya tidak "loncat"
        float pulse = Mathf.Lerp(minAlpha, maxAlpha, Mathf.Sin(t * pulseSpeed) * 0.5f + 0.5f);
        float edge = Mathf.Sin(cycle * Mathf.PI); // 0 di awal dan akhir, 1 di tengah
        group.alpha = pulse * Mathf.Lerp(0.6f, 1f, edge);
    }
}