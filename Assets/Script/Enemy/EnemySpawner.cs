using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    public enum SpawnMode
    {
        Instant,    // semua musuh muncul sekaligus
        Staggered   // musuh muncul satu per satu dengan jeda
    }

    public enum EnemyMixMode
    {
        FixedRatio,      // komposisi pasti sesuai persentase (mis. 10 musuh, 70/30 -> 7 dan 3), urutannya diacak
        WeightedRandom   // tiap musuh diundi sendiri, jadi komposisi bisa bervariasi tiap kali
    }

    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("Label otomatis (nama prefab + persentase). Tidak perlu diedit.")]
        public string label;
        public GameObject prefab;
        [Tooltip("Bobot relatif. Angka 70 dan 30 berarti 70% dan 30%. Total tidak harus 100.")]
        [Min(0f)] public float weight = 1f;
    }

    // Pasang di objek kosong dengan BoxCollider2D (Is Trigger) yang menutupi area ruangan.
    // Atur ukuran area lewat Size di BoxCollider2D, JANGAN lewat Scale objek.
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Musuh")]
        public List<SpawnEntry> enemies = new List<SpawnEntry>();
        public EnemyMixMode mixMode = EnemyMixMode.FixedRatio;
        public int enemyCount = 5;                 // total musuh yang akan di-spawn

        [Header("Lama (satu prefab) - hanya dipakai kalau daftar di atas kosong")]
        public GameObject enemyPrefab;

        [Header("Kapan spawn")]
        public bool spawnOnStart = true;           // spawn saat game mulai
        public bool spawnWhenPlayerEnters = false; // atau tunggu player masuk ruangan (sekali saja)

        [Header("Cara spawn")]
        public SpawnMode spawnMode = SpawnMode.Staggered;
        public float initialDelay = 1f;            // (Staggered) jeda sebelum musuh pertama muncul
        public float minDelayBetweenSpawns = 0.5f; // (Staggered) jeda minimum antar musuh
        public float maxDelayBetweenSpawns = 1.5f; // (Staggered) jeda maksimum, diacak di antara min-max
        [Tooltip("Batas musuh hidup bersamaan dari spawner ini. 0 = tanpa batas. " +
                 "Kalau penuh, musuh berikutnya menunggu sampai ada yang mati.")]
        public int maxAliveAtOnce = 0;

        [Header("Aturan posisi")]
        public float minDistanceBetweenEnemies = 1.5f; // supaya tidak numpuk
        public float minDistanceFromPlayer = 4f;       // supaya tidak spawn menempel player
        public float spawnCheckRadius = 0.5f;          // ukuran "badan" musuh saat cek tembok
        public LayerMask obstacleMask;                 // layer Wall / objek penghalang
        public int maxAttemptsPerEnemy = 30;

        // Semua spawner di scene (dipakai sistem respawn)
        public static readonly List<EnemySpawner> All = new List<EnemySpawner>();

        private BoxCollider2D area;
        private Transform player;
        private readonly List<GameObject> spawned = new List<GameObject>();
        private bool hasSpawned;
        private bool spawnFinished; // semua musuh sudah selesai di-spawn

        // Jumlah musuh yang masih hidup dari spawner ini
        public int AliveCount
        {
            get
            {
                spawned.RemoveAll(e => e == null);
                return spawned.Count;
            }
        }

        private void OnEnable()  { All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        private void Awake()
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
        }

        // Memperbarui label tiap entri di Inspector: "NamaPrefab - 70%"
        private void OnValidate()
        {
            float total = 0f;
            foreach (var e in enemies)
                if (e != null && e.prefab != null && e.weight > 0f) total += e.weight;

            foreach (var e in enemies)
            {
                if (e == null) continue;
                string name = e.prefab != null ? e.prefab.name : "(prefab kosong)";
                float pct = (e.prefab != null && e.weight > 0f && total > 0f) ? e.weight / total * 100f : 0f;
                e.label = $"{name} - {pct:0.#}%";
            }
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;

            if (spawnOnStart && !spawnWhenPlayerEnters) SpawnAll();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (spawnWhenPlayerEnters && !hasSpawned && other.CompareTag("Player"))
                SpawnAll();
        }

        public void SpawnAll()
        {
            if (hasSpawned) return;

            if (GetValidEntries().Count == 0)
            {
                Debug.LogWarning("[EnemySpawner] Tidak ada prefab valid. Isi daftar Enemies (prefab + weight lebih dari 0).", this);
                return;
            }

            hasSpawned = true;
            StartCoroutine(SpawnRoutine());
        }

        // ---------- Sistem respawn ----------
        // Ruangan dianggap "selesai" kalau semua musuh sudah di-spawn dan semuanya sudah mati.
        public bool IsCleared => spawnFinished && AliveCount == 0;

        // Ruangan yang sudah selesai SAAT checkpoint diaktifkan dikunci: tidak ikut reset lagi.
        public bool Committed { get; private set; }

        public void Commit()
        {
            if (IsCleared) Committed = true;
        }

        // Dipanggil Checkpoint: kunci semua ruangan yang sudah bersih
        public static void CommitAllCleared()
        {
            foreach (var s in new List<EnemySpawner>(All))
                if (s != null) s.Commit();
        }

        // Dipanggil PlayerDeath setelah player pindah ke checkpoint: ruangan yang belum dikunci diulang dari awal
        public static void ResetAllUncommitted()
        {
            foreach (var s in new List<EnemySpawner>(All))
                if (s != null) s.ResetSpawner();
        }

        public void ResetSpawner()
        {
            if (Committed) return; // sudah bersih sebelum checkpoint: tetap bersih

            StopAllCoroutines();
            foreach (var e in spawned)
                if (e != null) Destroy(e);
            spawned.Clear();

            hasSpawned = false;
            spawnFinished = false;

            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            // Spawn ulang sesuai cara kerja spawner ini
            if (spawnOnStart && !spawnWhenPlayerEnters)
            {
                SpawnAll();
            }
            else if (spawnWhenPlayerEnters && player != null && area.OverlapPoint(player.position))
            {
                // Player respawn di dalam area: OnTriggerEnter tidak akan terpanggil lagi, jadi spawn langsung
                SpawnAll();
            }
        }

        // ---------- Pemilihan prefab ----------
        private List<SpawnEntry> GetValidEntries()
        {
            var list = new List<SpawnEntry>();
            foreach (var e in enemies)
                if (e != null && e.prefab != null && e.weight > 0f) list.Add(e);

            // Kompatibilitas: kalau daftar kosong tapi slot lama terisi, pakai itu
            if (list.Count == 0 && enemyPrefab != null)
                list.Add(new SpawnEntry { prefab = enemyPrefab, weight = 1f });

            return list;
        }

        private GameObject PickWeighted(List<SpawnEntry> entries)
        {
            float total = 0f;
            foreach (var e in entries) total += e.weight;

            float roll = Random.Range(0f, total);
            float cumulative = 0f;
            foreach (var e in entries)
            {
                cumulative += e.weight;
                if (roll <= cumulative) return e.prefab;
            }
            return entries[entries.Count - 1].prefab;
        }

        // Komposisi pasti: bagi jumlah musuh sesuai persentase (metode sisa terbesar), lalu acak urutannya
        private List<GameObject> BuildFixedPlan(List<SpawnEntry> entries)
        {
            float total = 0f;
            foreach (var e in entries) total += e.weight;

            int n = entries.Count;
            int[] counts = new int[n];
            float[] remainder = new float[n];
            int assigned = 0;

            for (int i = 0; i < n; i++)
            {
                float exact = entries[i].weight / total * enemyCount;
                counts[i] = Mathf.FloorToInt(exact);
                remainder[i] = exact - counts[i];
                assigned += counts[i];
            }

            // Sisa slot diberikan ke entri dengan pecahan terbesar
            while (assigned < enemyCount)
            {
                int best = 0;
                for (int i = 1; i < n; i++)
                    if (remainder[i] > remainder[best]) best = i;
                counts[best]++;
                remainder[best] = -1f;
                assigned++;
            }

            var plan = new List<GameObject>();
            for (int i = 0; i < n; i++)
                for (int c = 0; c < counts[i]; c++)
                    plan.Add(entries[i].prefab);

            // Acak urutan (Fisher-Yates) supaya jenis musuh tidak muncul berkelompok
            for (int i = plan.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (plan[i], plan[j]) = (plan[j], plan[i]);
            }
            return plan;
        }

        // ---------- Proses spawn ----------
        private IEnumerator SpawnRoutine()
        {
            bool staggered = spawnMode == SpawnMode.Staggered;

            var entries = GetValidEntries();
            List<GameObject> plan = mixMode == EnemyMixMode.FixedRatio ? BuildFixedPlan(entries) : null;

            if (staggered && initialDelay > 0f)
                yield return new WaitForSeconds(initialDelay);

            int spawnedTotal = 0;
            int consecutiveFails = 0;

            while (spawnedTotal < enemyCount)
            {
                // Kalau sudah penuh, tunggu sampai ada musuh yang mati
                if (maxAliveAtOnce > 0 && AliveCount >= maxAliveAtOnce)
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                if (TryFindSpawnPoint(out Vector2 pos))
                {
                    GameObject prefab = plan != null ? plan[spawnedTotal] : PickWeighted(entries);
                    spawned.Add(Instantiate(prefab, pos, Quaternion.identity));
                    spawnedTotal++;
                    consecutiveFails = 0;

                    if (staggered)
                    {
                        float wait = Random.Range(minDelayBetweenSpawns, Mathf.Max(minDelayBetweenSpawns, maxDelayBetweenSpawns));
                        yield return new WaitForSeconds(wait);
                    }
                }
                else
                {
                    consecutiveFails++;
                    if (consecutiveFails >= 10)
                    {
                        Debug.LogWarning($"[EnemySpawner] Hanya {spawnedTotal} dari {enemyCount} musuh yang muat. " +
                                         "Perbesar area atau kecilkan Min Distance.", this);
                        spawnFinished = true;
                        yield break;
                    }
                    // Mungkin player/musuh sedang menghalangi, coba lagi sebentar lagi
                    yield return new WaitForSeconds(0.3f);
                }
            }

            spawnFinished = true;
        }

        private bool TryFindSpawnPoint(out Vector2 result)
        {
            Bounds b = area.bounds;
            spawned.RemoveAll(e => e == null);

            for (int attempt = 0; attempt < maxAttemptsPerEnemy; attempt++)
            {
                Vector2 candidate = new Vector2(
                    Random.Range(b.min.x, b.max.x),
                    Random.Range(b.min.y, b.max.y));

                // 1. Tidak di dalam tembok / objek penghalang
                if (Physics2D.OverlapCircle(candidate, spawnCheckRadius, obstacleMask)) continue;

                // 2. Tidak terlalu dekat player
                if (player != null && Vector2.Distance(candidate, player.position) < minDistanceFromPlayer) continue;

                // 3. Tidak menumpuk dengan musuh lain yang masih hidup (pakai posisi terkini mereka)
                bool tooClose = false;
                foreach (var e in spawned)
                {
                    if (Vector2.Distance(candidate, e.transform.position) < minDistanceBetweenEnemies)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                result = candidate;
                return true;
            }

            result = Vector2.zero;
            return false;
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
        }
    }
}