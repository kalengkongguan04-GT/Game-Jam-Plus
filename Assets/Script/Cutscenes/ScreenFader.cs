using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance;

    [SerializeField] private float fadeDuration = 0.6f;

    private CanvasGroup group;
    private bool loading;

    private void Awake()
    {
        Instance = this;
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    public void LoadScene(string sceneName)
    {
        if (loading) return;
        loading = true;
        StartCoroutine(Run(sceneName));
    }

    private IEnumerator Run(string sceneName)
    {
        group.blocksRaycasts = true;

        // 1. Fade ke hitam
        yield return Fade(0f, 1f);

        // 2. Load scene di belakang layar hitam
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        // 3. Pindah scene (layar tetap hitam sampai gameplay siap)
        op.allowSceneActivation = true;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        group.alpha = to;
    }
}