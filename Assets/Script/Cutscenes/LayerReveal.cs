using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class LayerReveal : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public CanvasGroup panel;
        [Tooltip("Dari mana layer datang. Pilih None kalau cuma fade.")]
        public Direction from = Direction.None;
        public float distance = 300f;
        public float duration = 0.6f;
    }

    public enum Direction { None, Right, Left, Top, Bottom }

    [Tooltip("Urutan = urutan klik. Elemen 0 muncul paling awal.")]
    [SerializeField] private Layer[] layers;
    [SerializeField] private UnityEvent onFinished;

    private Vector2[] finalPos;
    private int index = -1;
    private bool animating;
    private int enabledFrame;
    private Coroutine routine;

    private void Awake()
    {
        finalPos = new Vector2[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].panel == null) continue;
            finalPos[i] = ((RectTransform)layers[i].panel.transform).anchoredPosition;
            layers[i].panel.alpha = 0f;
        }
    }

    private void OnEnable() { enabledFrame = Time.frameCount; }

    private void Update()
    {
        if (Time.frameCount == enabledFrame) return;
        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            Next();
    }

    private void Next()
    {
        if (animating) { Finish(); return; }

        if (index + 1 < layers.Length)
        {
            index++;
            routine = StartCoroutine(Show(index));
        }
        else
        {
            if (onFinished != null && onFinished.GetPersistentEventCount() > 0)
                onFinished.Invoke();
            else
            {
                ComicManager m = FindObjectOfType<ComicManager>();
                if (m != null) m.NextPage();
            }
        }
    }

    private Vector2 Offset(Layer l)
    {
        switch (l.from)
        {
            case Direction.Right:  return new Vector2( l.distance, 0);
            case Direction.Left:   return new Vector2(-l.distance, 0);
            case Direction.Top:    return new Vector2(0,  l.distance);
            case Direction.Bottom: return new Vector2(0, -l.distance);
            default:               return Vector2.zero;
        }
    }

    private IEnumerator Show(int i)
    {
        animating = true;
        Layer l = layers[i];
        RectTransform rt = (RectTransform)l.panel.transform;
        Vector2 end = finalPos[i];
        Vector2 start = end + Offset(l);

        float t = 0f;
        while (t < l.duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / l.duration);
            k = 1f - (1f - k) * (1f - k);
            l.panel.alpha = k;
            rt.anchoredPosition = Vector2.Lerp(start, end, k);
            yield return null;
        }
        Finish();
    }

    private void Finish()
    {
        if (routine != null) StopCoroutine(routine);
        if (index >= 0 && index < layers.Length && layers[index].panel != null)
        {
            layers[index].panel.alpha = 1f;
            ((RectTransform)layers[index].panel.transform).anchoredPosition = finalPos[index];
        }
        animating = false;
    }
}