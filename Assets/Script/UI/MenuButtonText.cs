using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonText : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.85f, 0.4f);
    [SerializeField] private Color pressedColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private float hoverScale = 1.1f;

    private Vector3 baseScale;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        baseScale = transform.localScale;
        Apply(normalColor, 1f);
    }

    private void OnEnable() { Apply(normalColor, 1f); }

    public void OnPointerEnter(PointerEventData e) { Apply(hoverColor, hoverScale); }
    public void OnPointerExit(PointerEventData e)  { Apply(normalColor, 1f); }
    public void OnPointerDown(PointerEventData e)  { Apply(pressedColor, 1f); }
    public void OnPointerUp(PointerEventData e)    { Apply(hoverColor, hoverScale); }

    private void Apply(Color c, float scale)
    {
        if (label != null) label.color = c;
        transform.localScale = baseScale * scale;
    }
}