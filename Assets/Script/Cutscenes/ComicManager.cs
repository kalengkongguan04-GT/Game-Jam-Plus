using UnityEngine;
using UnityEngine.SceneManagement;

public class ComicManager : MonoBehaviour
{
    [Header("Halaman komik (urut dari pertama)")]
    [SerializeField] private GameObject[] pages;

    [Header("Scene tujuan setelah komik selesai")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Opsi")]
    [SerializeField] private bool allowSkip = false;

    private int currentPage = 0;
    private bool loading;

    private void Start()
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("ComicManager: Pages kosong, langsung ke gameplay.");
            LoadGameplay();
            return;
        }

        for (int i = 0; i < pages.Length; i++)
            if (pages[i] != null) pages[i].SetActive(false);

        pages[0].SetActive(true);
        Debug.Log("ComicManager: mulai dari " + pages[0].name);
    }

    private void Update()
    {
        if (allowSkip && Input.GetKeyDown(KeyCode.Escape))
            LoadGameplay();
    }

    // Dipanggil saat sebuah halaman selesai
    public void NextPage()
{
    if (loading) return;

    int old = currentPage;
    currentPage++;

    if (currentPage < pages.Length)
    {
        // Nyalakan halaman baru DULU
        pages[currentPage].SetActive(true);

        // Pastikan halaman baru digambar di depan halaman lama
        pages[currentPage].transform.SetAsLastSibling();

        // Matikan halaman lama setelah halaman baru sempat tampil
        StartCoroutine(DisableAfterFrame(pages[old]));
    }
    else
    {
        LoadGameplay();
    }
}

private System.Collections.IEnumerator DisableAfterFrame(GameObject page)
{
    yield return null;
    yield return null;
    if (page != null) page.SetActive(false);
}

    public void LoadGameplay()
    {
        if (loading) return;
        loading = true;

        Debug.Log("ComicManager: pindah ke scene " + gameplaySceneName);

        if (SceneFader.Instance != null)
            SceneFader.Instance.LoadScene(gameplaySceneName);
        else
            SceneManager.LoadScene(gameplaySceneName);
    }
}