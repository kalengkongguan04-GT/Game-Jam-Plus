using UnityEngine;

public class IdleFloat : MonoBehaviour
{
    [Header("Naik turun")]
    [SerializeField] private float floatAmount = 8f;     // pixel
    [SerializeField] private float floatSpeed = 1.2f;

    [Header("Napas (membesar mengecil)")]
    [SerializeField] private float breatheAmount = 0.015f; // 0.015 = 1.5%
    [SerializeField] private float breatheSpeed = 1.5f;

    [Header("Miring pelan")]
    [SerializeField] private float swayAngle = 1f;       // derajat
    [SerializeField] private float swaySpeed = 0.8f;

    private RectTransform rt;
    private Vector2 startPos;
    private Vector3 startScale;
    private float offset;

    private void Awake()
    {
        rt = (RectTransform)transform;
        startPos = rt.anchoredPosition;
        startScale = rt.localScale;
        offset = Random.value * 10f; // supaya beberapa object tidak serempak
    }

    private void Update()
    {
        float t = Time.unscaledTime + offset;

        float y = Mathf.Sin(t * floatSpeed) * floatAmount;
        rt.anchoredPosition = startPos + new Vector2(0f, y);

        float s = 1f + Mathf.Sin(t * breatheSpeed) * breatheAmount;
        rt.localScale = startScale * s;

        float a = Mathf.Sin(t * swaySpeed) * swayAngle;
        rt.localRotation = Quaternion.Euler(0f, 0f, a);
    }
}