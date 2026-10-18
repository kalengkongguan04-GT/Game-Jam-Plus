using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeInOnStart : MonoBehaviour
{
    [SerializeField] private float duration = 0.6f;

    private IEnumerator Start()
    {
        CanvasGroup g = GetComponent<CanvasGroup>();
        g.alpha = 1f;
        g.blocksRaycasts = false;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = 1f - (t / duration);
            yield return null;
        }
        g.alpha = 0f;
    }
}