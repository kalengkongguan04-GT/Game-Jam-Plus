using UnityEngine;
using UnityEngine.SceneManagement;

public class ComicManager : MonoBehaviour
{
    [Header("Halaman komik (urut dari pertama)")]
    [SerializeField] private GameObject[] pages;

    [Header("Scene tujuan setelah komik selesai")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Opsi")]
    [SerializeField] private bool allowSkip = true;   // tombol Escape untuk lewati komik

    private int currentPage = 0;

    private void Start()
    {
        // Matikan semua halaman, lalu nyalakan halaman pertama
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(false);

        if (pages.Length > 0)
            pages[0].SetActive(true);
        else
            LoadGameplay();
    }

    private void Update()
    {
        if (allowSkip && Input.GetKeyDown(KeyCode.Escape))
            LoadGameplay();
    }

    // Dipanggil dari On Page Finished milik tiap ComicPage
    public void NextPage()
    {
        pages[currentPage].SetActive(false);
        currentPage++;

        if (currentPage < pages.Length)
            pages[currentPage].SetActive(true);
        else
            LoadGameplay();
    }

    public void LoadGameplay()
{
    if (SceneFader.Instance != null)
        SceneFader.Instance.LoadScene(gameplaySceneName);
    else
        SceneManager.LoadScene(gameplaySceneName);
}
}