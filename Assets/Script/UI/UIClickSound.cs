using UnityEngine;
using UnityEngine.EventSystems;

public class UIClickSound : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public void OnPointerClick(PointerEventData e)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayHover();
    }
}