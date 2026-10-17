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

    // Pasang di objek kosong dengan BoxCollider2D (Is Trigger) yang menutupi area ruangan.
    // Atur ukuran area lewat Size di BoxCollider2D, JANGAN lewat Scale objek.
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Musuh")]
        public GameObject enemyPrefab;
        public int enemyCount = 5;                 // total musuh yang akan di-spawn

        [Header("Kapan spawn")]
        public bool spawnOnStart = true;           // mulai saat game dimulai
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

        private BoxCollider2D area;
        private Transform player;
        private readonly List<GameObject> spawned = new List<GameObject>();
        private bool hasSpawned;

        // Jumlah musuh yang masih hidup dari spawner ini
        public int AliveCount
        {
            get
            {
                spawned.RemoveAll(e => e == null);
                return spawned.Count;
            }
        }

        private void Awake()
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
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
            if (enemyPrefab == null)
            {
                Debug.LogWarning("[EnemySpawner] Enemy Prefab belum diisi.", this);
                return;
            }

            hasSpawned = true;
            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            bool staggered = spawnMode == SpawnMode.Staggered;

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
                    spawned.Add(Instantiate(enemyPrefab, pos, Quaternion.identity));
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
                        yield break;
                    }
                    // Mungkin player/musuh sedang menghalangi, coba lagi sebentar lagi
                    yield return new WaitForSeconds(0.3f);
                }
            }
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