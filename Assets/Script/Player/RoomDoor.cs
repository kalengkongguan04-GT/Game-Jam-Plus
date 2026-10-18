using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cainos.PixelArtTopDown_Basic
{
    // Pasang di objek pintu.
    // Alur: semua musuh di spawner terkait mati -> kunci muncul -> player ambil kunci
    //       (satu atau beberapa, sesuai Keys Required) -> dekati pintu -> pintu terbuka.
    //
    // Pintu butuh Collider2D SOLID (bukan trigger) yang menghalangi jalan. Collider ini dimatikan saat pintu terbuka.
    public class RoomDoor : MonoBehaviour
    {
        [Header("Ruangan yang harus dibersihkan")]
        public List<EnemySpawner> spawners = new List<EnemySpawner>();

        [Header("Kunci")]
        [Min(1)] public int keysRequired = 1;   // jumlah kunci yang harus diambil untuk membuka pintu
        public GameObject keyPrefab;            // satu jenis kunci, dibuat sebanyak Keys Required
        [Tooltip("Kalau kuncinya beda-beda (mis. merah dan biru): isi dengan prefab masing-masing, satu prefab = satu kunci, urutannya = urutan Key Spawn Points. Kalau diisi, Key Prefab dan Keys Required diabaikan, dan SEMUA kunci di daftar ini harus diambil.")]
        public List<GameObject> keyPrefabs = new List<GameObject>();
        [Tooltip("Titik muncul kunci. Satu titik per kunci. Kalau kurang dari jumlah kunci, sisanya diletakkan berderet di samping titik terakhir.")]
        public List<Transform> keySpawnPoints = new List<Transform>();
        [Tooltip("ALTERNATIF: kunci yang sudah ditaruh di scene. Semuanya disembunyikan dan baru muncul saat ruangan bersih. Kosongkan kalau pakai Key Prefab.")]
        public List<GameObject> sceneKeys = new List<GameObject>();
        public float fallbackKeySpacing = 0.8f; // jarak antar kunci kalau titik muncul kurang

        [Header("Pintu")]
        public Collider2D doorCollider;   // collider penghalang (kalau kosong, cari otomatis di objek ini)
        public GameObject closedVisual;   // opsional: tampilan pintu tertutup
        public GameObject openVisual;     // opsional: tampilan pintu terbuka
        public float openRadius = 1.5f;   // jarak player ke pintu supaya bisa membuka
        public bool requireInteractKey = false; // true = harus tekan tombol interact, false = otomatis terbuka
        public KeyCode interactKey = KeyCode.E;

        [Header("Event (SFX, efek, dll)")]
        public UnityEvent onKeySpawned;
        public UnityEvent onKeyCollected;    // dipanggil tiap kali satu kunci diambil
        public UnityEvent onAllKeysCollected;
        public UnityEvent onDoorOpened;

        // (terkumpul, dibutuhkan): dipakai UI penghitung kunci
        public event System.Action<int, int> OnKeysChanged;

        public int KeysCollected { get; private set; }
        // Kalau pakai Key Prefabs atau Scene Keys, semua kunci di daftar wajib diambil
        public int KeysRequired => TotalKeys();
        public bool HasKey => KeysCollected >= KeysRequired;   // semua kunci yang dibutuhkan sudah terkumpul
        public bool IsOpen { get; private set; }

        // true selama kunci sudah dilepas (ruangan bersih) dan pintu belum terbuka
        public bool KeyAvailable => keysSpawned && !IsOpen;

        private bool keysSpawned;
        private readonly List<GameObject> keyInstances = new List<GameObject>();
        private Transform player;

        private void Awake()
        {
            if (doorCollider == null) doorCollider = GetComponent<Collider2D>();
            foreach (var k in sceneKeys)
                if (k != null) k.SetActive(false); // sembunyikan total sampai ruangan bersih
            if (openVisual != null) openVisual.SetActive(false);
            if (closedVisual != null) closedVisual.SetActive(true);
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            OnKeysChanged?.Invoke(KeysCollected, KeysRequired);
        }

        private void Update()
        {
            if (IsOpen) return;

            bool cleared = AllCleared();

            if (cleared && !keysSpawned)
            {
                SpawnKeys();
            }
            else if (!cleared && keysSpawned)
            {
                // Player mati dan ruangan di-reset: musuh muncul lagi, semua kunci dibatalkan
                ResetKeys();
            }

            if (keysSpawned && HasKey) TryOpen();
        }

        private bool AllCleared()
        {
            if (spawners == null || spawners.Count == 0) return false;
            foreach (var s in spawners)
                if (s == null || !s.IsCleared) return false;
            return true;
        }

        // Jumlah kunci yang dimunculkan: sebanyak Scene Keys kalau dipakai, kalau tidak sebanyak Keys Required
        private int TotalKeys()
        {
            int scene = 0;
            foreach (var k in sceneKeys) if (k != null) scene++;
            if (scene > 0) return scene;

            int prefabs = 0;
            foreach (var k in keyPrefabs) if (k != null) prefabs++;
            return prefabs > 0 ? prefabs : keysRequired;
        }

        private void SpawnKeys()
        {
            keysSpawned = true;
            keyInstances.Clear();

            int sceneCount = 0;
            foreach (var k in sceneKeys)
            {
                if (k == null) continue;
                sceneCount++;
                k.SetActive(true);
                Register(k);
            }

            if (sceneCount == 0)
            {
                if (keyPrefabs.Count > 0)
                {
                    // Kunci berbeda-beda: tiap prefab muncul di titik dengan nomor urut yang sama
                    for (int i = 0; i < keyPrefabs.Count; i++)
                        if (keyPrefabs[i] != null)
                            Register(Instantiate(keyPrefabs[i], GetSpawnPosition(i), Quaternion.identity));
                }
                else if (keyPrefab != null)
                {
                    for (int i = 0; i < keysRequired; i++)
                        Register(Instantiate(keyPrefab, GetSpawnPosition(i), Quaternion.identity));
                }
                else
                {
                    Debug.LogWarning("[RoomDoor] Isi Key Prefab, Key Prefabs, atau Scene Keys.", this);
                    return;
                }
            }

            onKeySpawned.Invoke();
            OnKeysChanged?.Invoke(KeysCollected, KeysRequired);
        }

        private void Register(GameObject key)
        {
            keyInstances.Add(key);
            var pickup = key.GetComponentInChildren<KeyPickup>();
            if (pickup != null) pickup.door = this;
            else Debug.LogWarning("[RoomDoor] Objek kunci tidak punya script KeyPickup.", key);
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (index < keySpawnPoints.Count && keySpawnPoints[index] != null)
                return keySpawnPoints[index].position;

            // Titik kurang: letakkan berderet ke kanan dari titik terakhir yang ada (atau dari pintu)
            Vector3 basePos = transform.position;
            int baseIndex = 0;
            for (int i = Mathf.Min(index, keySpawnPoints.Count) - 1; i >= 0; i--)
            {
                if (keySpawnPoints[i] != null) { basePos = keySpawnPoints[i].position; baseIndex = i; break; }
            }
            return basePos + Vector3.right * fallbackKeySpacing * (index - baseIndex);
        }

        private void ResetKeys()
        {
            foreach (var k in keyInstances)
            {
                if (k == null) continue;
                if (sceneKeys.Contains(k)) k.SetActive(false);
                else Destroy(k);
            }
            foreach (var k in sceneKeys)
                if (k != null) k.SetActive(false);

            keyInstances.Clear();
            keysSpawned = false;
            KeysCollected = 0;
            OnKeysChanged?.Invoke(KeysCollected, KeysRequired);
        }

        // Dipanggil KeyPickup tiap kali satu kunci diambil
        public void CollectKey()
        {
            KeysCollected = Mathf.Min(KeysCollected + 1, TotalKeys());
            onKeyCollected.Invoke();
            OnKeysChanged?.Invoke(KeysCollected, KeysRequired);

            if (HasKey) onAllKeysCollected.Invoke();
        }

        private void TryOpen()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
                else return;
            }

            if (Vector2.Distance(player.position, transform.position) > openRadius) return;
            if (requireInteractKey && !Input.GetKeyDown(interactKey)) return;

            Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;

            if (doorCollider != null) doorCollider.enabled = false;
            if (closedVisual != null) closedVisual.SetActive(false);
            if (openVisual != null) openVisual.SetActive(true);

            // Kunci status ruangan: kalau player mati setelah pintu terbuka, musuh tidak muncul lagi
            foreach (var s in spawners)
                if (s != null) s.Commit();

            onDoorOpened.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, openRadius);

            Gizmos.color = Color.cyan;
            foreach (var t in keySpawnPoints)
                if (t != null) Gizmos.DrawWireSphere(t.position, 0.3f);
        }
    }
}