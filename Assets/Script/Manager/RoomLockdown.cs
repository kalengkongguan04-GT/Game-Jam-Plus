using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cainos.PixelArtTopDown_Basic
{
    // Pasang di objek kosong dengan BoxCollider2D yang menutupi ruangan (ukuran lewat Size, bukan Scale).
    // Saat player masuk ke ruangan dan musuhnya belum habis, semua Barriers (gerbang/tembok penutup pintu masuk)
    // diaktifkan sehingga player tidak bisa keluar. Saat semua musuh mati, Barriers dimatikan lagi.
    //
    // Kalau player mati di dalam ruangan, gerbang tetap tertutup selama layar mati, lalu otomatis terbuka
    // begitu player dipindah ke checkpoint di luar ruangan (dan tertutup lagi kalau masuk lagi).
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomLockdown : MonoBehaviour
    {
        [Header("Ruangan")]
        [Tooltip("Spawner di ruangan ini. Gerbang terkunci selama salah satunya belum selesai (belum bersih).")]
        public List<EnemySpawner> spawners = new List<EnemySpawner>();

        [Header("Gerbang")]
        [Tooltip("Objek penutup pintu masuk (sprite + collider SOLID). Dinyalakan saat terkunci, dimatikan saat terbuka.")]
        public List<GameObject> barriers = new List<GameObject>();
        [Tooltip("Player baru dikunci kalau sudah masuk sejauh ini dari tepi area, supaya gerbang tidak menutup tepat di atas player.")]
        public float entryMargin = 1.5f;

        [Header("Event (SFX, efek, dll)")]
        public UnityEvent onLock;
        public UnityEvent onUnlock;

        public bool IsLocked { get; private set; }

        private BoxCollider2D area;
        private Transform player;
        private PlayerDeath death;
        private bool wasDead;

        private void Awake()
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
            SetBarriers(false);
        }

        private void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                player = p.transform;
                death = p.GetComponentInParent<PlayerDeath>();
            }

            if (!IsLocked)
            {
                if (RoomActive() && InsideInner()) Lock();
            }
            else
            {
                bool dead = death != null && death.IsDead;
                bool justRespawned = wasDead && !dead;
                wasDead = dead;

                if (!RoomActive())
                {
                    Unlock(); // semua musuh mati
                }
                else if (justRespawned && !InsideInner())
                {
                    // Baru respawn dan tidak berada di area dalam ruangan (mis. checkpoint di luar atau di tepi pintu masuk)
                    Unlock();
                }
                else if (!dead && !area.OverlapPoint(player.position))
                {
                    Unlock(); // pengaman: player ada di luar area sama sekali
                }
            }
        }

        // Ruangan "aktif" = ada spawner yang belum selesai (belum bersih)
        private bool RoomActive()
        {
            foreach (var s in spawners)
                if (s != null && !s.IsCleared) return true;
            return false;
        }

        // Area dalam: area ruangan dikurangi entryMargin di tiap sisi
        private bool InsideInner()
        {
            Bounds b = area.bounds;
            Vector2 half = (Vector2)b.extents - Vector2.one * entryMargin;
            if (half.x <= 0f || half.y <= 0f) return area.OverlapPoint(player.position); // area terlalu kecil

            Vector2 d = (Vector2)player.position - (Vector2)b.center;
            return Mathf.Abs(d.x) <= half.x && Mathf.Abs(d.y) <= half.y;
        }

        private void Lock()
        {
            IsLocked = true;
            wasDead = death != null && death.IsDead;
            SetBarriers(true);
            onLock.Invoke();
        }

        private void Unlock()
        {
            IsLocked = false;
            SetBarriers(false);
            onUnlock.Invoke();
        }

        private void SetBarriers(bool on)
        {
            foreach (var b in barriers)
                if (b != null) b.SetActive(on);
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;

            // Area ruangan (merah)
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);

            // Area dalam tempat player dikunci (kuning)
            Vector3 inner = box.bounds.size - new Vector3(entryMargin * 2f, entryMargin * 2f, 0f);
            if (inner.x > 0f && inner.y > 0f)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
                Gizmos.DrawWireCube(box.bounds.center, inner);
            }
        }
    }
}