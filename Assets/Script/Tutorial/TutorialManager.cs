using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Header("Panels (urut)")]
    [SerializeField] private GameObject[] panels;   // Move, Dash, Combat
    [SerializeField] private Button[] nextButtons;  // satu tombol per panel

    [Header("Setting")]
    [SerializeField] private string firstLevelName = "Level1";
    [SerializeField] private bool showOnlyOnce = true;

    private const string KEY = "TutorialDone";
    private int currentIndex = -1;

    private void Start()
    {
        // Hanya muncul di level pertama
        bool isLevel1 = SceneManager.GetActiveScene().name == firstLevelName;
        bool alreadyDone = showOnlyOnce && PlayerPrefs.GetInt(KEY, 0) == 1;

        if (!isLevel1 || alreadyDone)
        {
            gameObject.SetActive(false);
            return;
        }

        // Pasang listener tombol next
        for (int i = 0; i < nextButtons.Length; i++)
            nextButtons[i].onClick.AddListener(NextPanel);

        NextPanel(); // tampilkan panel pertama
    }

    private void NextPanel()
    {
        // Matikan panel sekarang
        if (currentIndex >= 0)
            panels[currentIndex].SetActive(false);

        currentIndex++;

        // Kalau sudah lewat panel terakhir, selesai
        if (currentIndex >= panels.Length)
        {
            FinishTutorial();
            return;
        }

        panels[currentIndex].SetActive(true);
    }

    private void FinishTutorial()
    {
        PlayerPrefs.SetInt(KEY, 1);
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }

    // Buat testing: panggil dari Inspector (klik kanan komponen > Reset Tutorial)
    [ContextMenu("Reset Tutorial")]
    private void ResetTutorial() => PlayerPrefs.DeleteKey(KEY);
}