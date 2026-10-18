using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    [Header("Tujuan")]
    [SerializeField] private string nextSceneName = "Level2";

    [Header("Syarat")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Kalau true, portal terkunci sampai Unlock() dipanggil (misal setelah musuh habis)")]
    [SerializeField] private bool startLocked = false;

    [Header("Delay (opsional)")]
    [SerializeField] private float delay = 0f;

    private bool isLocked;
    private bool isLoading;

    private void Start()
    {
        isLocked = startLocked;

        // Cek setup di awal biar ketahuan kalau ada yang salah
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            Debug.LogError($"[LevelTransition] Scene '{nextSceneName}' TIDAK ketemu. " +
                           "Cek nama scene (huruf besar/kecil) dan pastikan sudah masuk File > Build Settings > Scenes In Build.", this);

        if (GetComponent<Collider>() == null && GetComponent<Collider2D>() == null)
            Debug.LogError("[LevelTransition] Objek ini tidak punya Collider. Tambahkan Box Collider (3D) atau Box Collider 2D.", this);
    }

    public void Unlock() => isLocked = false;
    public void Lock() => isLocked = true;

    public void LoadNextLevel()
    {
        if (isLoading) return;
        isLoading = true;
        Time.timeScale = 1f;

        Debug.Log($"[LevelTransition] Pindah ke scene '{nextSceneName}'...");

        if (delay > 0f) Invoke(nameof(DoLoad), delay);
        else DoLoad();
    }

    private void DoLoad()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    // ---- Trigger 3D ----
    private void OnTriggerEnter(Collider other) => TryEnter(other.gameObject,
        other.attachedRigidbody ? other.attachedRigidbody.gameObject : null);

    // ---- Trigger 2D ----
    private void OnTriggerEnter2D(Collider2D other) => TryEnter(other.gameObject,
        other.attachedRigidbody ? other.attachedRigidbody.gameObject : null);

    // ---- Kalau yang kepanggil ini, berarti Is Trigger belum dicentang ----
    private void OnCollisionEnter(Collision c) => WarnNotTrigger(c.gameObject);
    private void OnCollisionEnter2D(Collision2D c) => WarnNotTrigger(c.gameObject);

    private void WarnNotTrigger(GameObject who)
    {
        Debug.LogWarning($"[LevelTransition] '{who.name}' nabrak portal tapi 'Is Trigger' BELUM dicentang di collider portal.", this);
    }

    private void TryEnter(GameObject hit, GameObject rbObject)
    {
        Debug.Log($"[LevelTransition] Ada yang masuk trigger: '{hit.name}' (tag: {hit.tag})");

        if (isLocked) { Debug.Log("[LevelTransition] Gagal: portal masih TERKUNCI (Start Locked / belum Unlock())."); return; }
        if (isLoading) return;

        // Cek tag di collider, di objek rigidbody-nya, atau di parent paling atas
        bool isPlayer = hit.CompareTag(playerTag)
                     || (rbObject != null && rbObject.CompareTag(playerTag))
                     || hit.transform.root.CompareTag(playerTag);

        if (!isPlayer)
        {
            Debug.LogWarning($"[LevelTransition] Gagal: '{hit.name}' tidak bertag '{playerTag}'. Set Tag player di Inspector.");
            return;
        }

        LoadNextLevel();
    }
}