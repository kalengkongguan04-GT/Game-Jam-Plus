using System.Collections;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour, IHittable
    {
        [Header("Deteksi")]
        public float detectRadius = 6f;       // mulai mengejar/menyerang
        public float loseRadius = 8f;         // berhenti kalau player lebih jauh dari ini
        public LayerMask obstacleMask;        // layer dinding, supaya tidak menembak lewat tembok

        [Header("Gerak")]
        public float moveSpeed = 2f;
        public float keepDistance = 4f;       // jarak ideal dari player

        [Header("Serangan")]
        public GameObject projectilePrefab;
        public Transform firePoint;           // opsional, kalau kosong pakai posisi musuh
        public float shootCooldown = 1.5f;
        public float windupTime = 0.4f;       // jeda "aba-aba" sebelum menembak, memberi waktu player menghindar
        public float projectileSpeed = 7f;

        private Rigidbody2D rb;
        private Transform player;
        private bool isChasing;
        private bool isWindingUp;
        private float nextShootTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;      // game top-down: tidak boleh ada gravitasi
            rb.freezeRotation = true;  // musuh tidak ikut berputar saat bertabrakan
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        private void Update()
        {
            if (player == null) return;

            float dist = Vector2.Distance(transform.position, player.position);

            // Hysteresis: mulai di detectRadius, berhenti di loseRadius (biar tidak bolak-balik di garis batas)
            if (!isChasing && dist <= detectRadius && HasLineOfSight()) isChasing = true;
            else if (isChasing && dist > loseRadius) isChasing = false;

            if (!isChasing)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            // Gerak: dekati sampai keepDistance, lalu diam
            Vector2 toPlayer = (player.position - transform.position).normalized;
            if (!isWindingUp && dist > keepDistance) rb.velocity = toPlayer * moveSpeed;
            else rb.velocity = Vector2.zero;

            // Tembak
            if (!isWindingUp && Time.time >= nextShootTime && HasLineOfSight())
                StartCoroutine(Shoot());
        }

        private bool HasLineOfSight()
        {
            Vector2 from = transform.position;
            Vector2 to = player.position;
            return !Physics2D.Linecast(from, to, obstacleMask);
        }

        private IEnumerator Shoot()
        {
            isWindingUp = true;
            rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(windupTime);   // TODO: mainkan animasi/efek aba-aba di sini

            if (player != null && projectilePrefab != null)
            {
                Vector2 origin = firePoint != null ? firePoint.position : transform.position;
                Vector2 dir = ((Vector2)player.position - origin).normalized; // arah dikunci saat menembak
                GameObject bullet = Instantiate(projectilePrefab, origin, Quaternion.identity);
                var proj = bullet.GetComponent<EnemyProjectile>();
                if (proj != null) proj.Init(dir, projectileSpeed);
            }

            nextShootTime = Time.time + shootCooldown;
            isWindingUp = false;
        }

        // Dipanggil PlayerSkills saat laser mengenai musuh
        public void OnLaserHit()
        {
            Destroy(gameObject); // ganti dengan animasi mati kalau sudah ada
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, loseRadius);
        }
    }
}