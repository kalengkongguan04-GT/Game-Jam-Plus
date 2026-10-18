using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    public enum EnemyAttackType
    {
        Ranged,   // menembakkan peluru dari jauh
        Melee     // mendekat lalu menyerang jarak dekat
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour, IHittable
    {
        // Daftar semua musuh yang sedang hidup (dipakai pemisahan antar musuh dan cheat)
        public static readonly List<EnemyAI> All = new List<EnemyAI>();

        [Header("Tipe Serangan")]
        public EnemyAttackType attackType = EnemyAttackType.Ranged;

        [Header("Layer")]
        [Tooltip("Nama layer yang dipaksakan ke musuh dan semua child-nya saat muncul (dipakai laser). Kosongkan untuk menonaktifkan.")]
        public string enemyLayerName = "Enemy";
        [Tooltip("Debug: tulis log kalau layer musuh berubah saat game berjalan, beserta objek yang sedang disentuh musuh.")]
        public bool debugLayerChanges = false;

        [Header("Deteksi")]
        public float detectRadius = 6f;       // mulai mengejar/menyerang
        public float loseRadius = 8f;         // berhenti kalau player lebih jauh dari ini
        public LayerMask obstacleMask;        // layer dinding, supaya tidak menyerang lewat tembok

        [Header("Gerak")]
        public float moveSpeed = 2f;
        public float keepDistance = 4f;       // (Ranged) jarak ideal dari player

        [Header("Anti-Numpuk")]
        public float separationRadius = 1.0f;    // musuh yang lebih dekat dari ini saling mendorong menjauh
        public float separationStrength = 2f;    // kekuatan dorongan (satuan kecepatan)

        [Header("Ranged - Tembak")]
        public GameObject projectilePrefab;
        public Transform firePoint;           // opsional, kalau kosong pakai posisi musuh
        public float shootCooldown = 1.5f;
        public float windupTime = 0.4f;       // jeda "aba-aba" sebelum menembak, memberi waktu player menghindar
        public float projectileSpeed = 7f;

        [Header("Melee - Serang Jarak Dekat")]
        public float meleeTriggerRange = 1.4f;   // jarak ke player untuk mulai menyerang
        public float meleeWindup = 0.5f;         // aba-aba, arah serangan DIKUNCI di awal aba-aba
        public float meleeHitOffset = 0.9f;      // jarak pusat area serang dari musuh, searah arah terkunci
        public float meleeHitRadius = 0.8f;      // ukuran area serang (lingkaran)
        public float meleeActiveTime = 0.15f;    // lama area serang aktif
        public float meleeLungeSpeed = 5f;       // dorongan maju saat menyerang (0 = tidak maju)
        public float meleeRecovery = 0.5f;       // diam setelah menyerang (waktu player menyerang balik)
        public float meleeCooldown = 0.5f;       // jeda sebelum bisa menyerang lagi setelah recovery
        public GameObject meleeTelegraph;        // opsional: lingkaran merah (sprite) yang menunjukkan area serang saat aba-aba

        [Header("Animasi / Mati")]
        public float deathDestroyDelay = 0.6f;   // lama menunggu animasi mati sebelum musuh dihapus (0 = langsung)

        // Status untuk EnemyAnimator
        public bool IsChasing => isChasing;
        public bool IsAttacking => isAttacking;
        public bool IsDead { get; private set; }

        // Event untuk animasi / SFX
        public event System.Action OnWindupStart;   // aba-aba serangan mulai
        public event System.Action OnAttackFire;    // peluru keluar / area serang aktif
        public event System.Action OnDied;

        private Rigidbody2D rb;
        private Transform player;
        private bool isChasing;
        private bool isAttacking;
        private float nextAttackTime;
        private int lastLayer;
        private Collider2D ownCollider;

        private void OnEnable()  { All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;      // game top-down: tidak boleh ada gravitasi
            rb.freezeRotation = true;  // musuh tidak ikut berputar saat bertabrakan
            if (meleeTelegraph != null) meleeTelegraph.SetActive(false);
            ownCollider = GetComponent<Collider2D>();
            ApplyEnemyLayer();
            lastLayer = gameObject.layer;
        }

        private void Start()
        {
            ApplyEnemyLayer(); // dipanggil lagi, kalau ada script lain yang mengubah layer saat Awake
            lastLayer = gameObject.layer;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        private void Update()
        {
            if (IsDead || player == null) return;

            float dist = Vector2.Distance(transform.position, player.position);

            // Hysteresis: mulai di detectRadius, berhenti di loseRadius (biar tidak bolak-balik di garis batas)
            if (!isChasing && dist <= detectRadius && HasLineOfSight()) isChasing = true;
            else if (isChasing && dist > loseRadius) isChasing = false;

            if (!isChasing)
            {
                if (!isAttacking) rb.velocity = GetSeparation() * separationStrength;
                return;
            }

            // Gerak (hanya kalau sedang tidak menyerang; saat menyerang, coroutine yang mengatur gerak)
            if (!isAttacking)
            {
                Vector2 toPlayer = (player.position - transform.position).normalized;
                float stopDistance = attackType == EnemyAttackType.Melee ? meleeTriggerRange * 0.8f : keepDistance;
                Vector2 move = dist > stopDistance ? toPlayer * moveSpeed : Vector2.zero;
                move += GetSeparation() * separationStrength;
                rb.velocity = Vector2.ClampMagnitude(move, moveSpeed * 1.5f);
            }

            // Serang
            if (!isAttacking && Time.time >= nextAttackTime && HasLineOfSight())
            {
                if (attackType == EnemyAttackType.Ranged)
                    StartCoroutine(Shoot());
                else if (dist <= meleeTriggerRange)
                    StartCoroutine(MeleeAttack());
            }
        }

        // Debug: ketahuan kapan dan di mana layer musuh berubah, dan objek apa yang sedang disentuh saat itu
        private void LateUpdate()
        {
            if (!debugLayerChanges || gameObject.layer == lastLayer) return;

            string touching = "";
            if (ownCollider != null)
            {
                var results = new List<Collider2D>();
                ownCollider.OverlapCollider(new ContactFilter2D().NoFilter(), results);
                foreach (var c in results)
                {
                    string parentName = c.transform.parent != null ? c.transform.parent.name : "-";
                    touching += $"\n   - {c.name} (parent: {parentName}, trigger: {c.isTrigger})";
                }
            }

            Debug.Log($"[LayerWatch] {name}: layer {LayerMask.LayerToName(lastLayer)} -> {LayerMask.LayerToName(gameObject.layer)} " +
                      $"di posisi {transform.position}. Yang disentuh:{touching}", this);
            lastLayer = gameObject.layer;
        }

        private static bool warnedMissingLayer;

        // Pastikan layer musuh (dan semua child-nya) benar, apa pun layer asli prefab-nya
        private void ApplyEnemyLayer()
        {
            if (string.IsNullOrEmpty(enemyLayerName)) return;

            int layer = LayerMask.NameToLayer(enemyLayerName);
            if (layer < 0)
            {
                if (!warnedMissingLayer)
                {
                    Debug.LogWarning($"[EnemyAI] Layer \"{enemyLayerName}\" tidak ada. Buat di Project Settings > Tags and Layers, " +
                                     "atau ubah/kosongkan field Enemy Layer Name.", this);
                    warnedMissingLayer = true;
                }
                return;
            }
            SetLayerRecursively(gameObject, layer);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        // Dorongan menjauh dari musuh lain yang terlalu dekat, supaya tidak menumpuk
        private Vector2 GetSeparation()
        {
            Vector2 push = Vector2.zero;
            Vector2 myPos = transform.position;

            foreach (var other in All)
            {
                if (other == this || other == null) continue;

                Vector2 diff = myPos - (Vector2)other.transform.position;
                float d = diff.magnitude;
                if (d >= separationRadius) continue;

                // Kalau posisinya persis sama, pakai arah unik per musuh supaya mereka bisa berpisah
                if (d < 0.001f)
                {
                    float a = (GetInstanceID() % 360) * Mathf.Deg2Rad;
                    diff = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    d = 0f;
                }

                push += diff.normalized * (1f - d / separationRadius);
            }
            return push;
        }

        private bool HasLineOfSight()
        {
            Vector2 from = transform.position;
            Vector2 to = player.position;
            return !Physics2D.Linecast(from, to, obstacleMask);
        }

        // ================= RANGED =================
        private IEnumerator Shoot()
        {
            isAttacking = true;
            rb.velocity = Vector2.zero;
            OnWindupStart?.Invoke();
            yield return new WaitForSeconds(windupTime);
            OnAttackFire?.Invoke();

            if (player != null && projectilePrefab != null)
            {
                Vector2 origin = firePoint != null ? firePoint.position : transform.position;
                Vector2 dir = ((Vector2)player.position - origin).normalized; // arah dikunci saat menembak
                GameObject bullet = Instantiate(projectilePrefab, origin, Quaternion.identity);
                var proj = bullet.GetComponent<EnemyProjectile>();
                if (proj != null) proj.Init(dir, projectileSpeed);
            }

            nextAttackTime = Time.time + shootCooldown;
            isAttacking = false;
        }

        // ================= MELEE =================
        private IEnumerator MeleeAttack()
        {
            isAttacking = true;
            rb.velocity = Vector2.zero;

            // 1. ABA-ABA: arah dikunci di awal, jadi player bisa menghindar dengan bergerak/dash
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            ShowTelegraph(dir, true);
            OnWindupStart?.Invoke();
            yield return new WaitForSeconds(meleeWindup);
            ShowTelegraph(dir, false);
            OnAttackFire?.Invoke();

            // 2. SERANG: area serang aktif sebentar, musuh sedikit maju
            float t = 0f;
            bool contact = false;
            while (t < meleeActiveTime)
            {
                rb.velocity = dir * meleeLungeSpeed;
                if (!contact) contact = CheckMeleeHit(dir); // satu serangan = satu kontak
                t += Time.deltaTime;
                yield return null;
            }
            rb.velocity = Vector2.zero;

            // 3. RECOVERY: musuh diam, kesempatan player untuk membalas
            yield return new WaitForSeconds(meleeRecovery);

            nextAttackTime = Time.time + meleeCooldown;
            isAttacking = false;
        }

        private bool CheckMeleeHit(Vector2 dir)
        {
            Vector2 center = (Vector2)transform.position + dir * meleeHitOffset;

            foreach (var col in Physics2D.OverlapCircleAll(center, meleeHitRadius))
            {
                if (!col.CompareTag("Player")) continue;

                var death = col.GetComponentInParent<PlayerDeath>();
                // TryHit sudah mengurus shield (blok dari depan) dan i-frame dash
                if (death != null) death.TryHit(transform.position);
                return true;
            }
            return false;
        }

        private void ShowTelegraph(Vector2 dir, bool show)
        {
            if (meleeTelegraph == null) return;

            if (show)
            {
                // Posisi dan ukuran dalam satuan dunia (sprite lingkaran 1 unit diameter)
                meleeTelegraph.transform.position = (Vector2)transform.position + dir * meleeHitOffset;
                Transform parent = meleeTelegraph.transform.parent;
                float parentScale = parent != null ? Mathf.Abs(parent.lossyScale.x) : 1f;
                meleeTelegraph.transform.localScale = Vector3.one * (meleeHitRadius * 2f / Mathf.Max(0.0001f, parentScale));
            }
            meleeTelegraph.SetActive(show);
        }

        // Dipanggil PlayerSkills saat laser mengenai musuh
        public void OnLaserHit()
        {
            if (IsDead) return;
            IsDead = true;

            StopAllCoroutines();
            ShowTelegraph(Vector2.right, false);
            rb.velocity = Vector2.zero;
            All.Remove(this); // tidak ikut dorong-mendorong musuh lain lagi

            foreach (var c in GetComponentsInChildren<Collider2D>())
                c.enabled = false;

            OnDied?.Invoke();
            Destroy(gameObject, deathDestroyDelay);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, loseRadius);

            if (attackType == EnemyAttackType.Melee)
            {
                // Jarak mulai menyerang (cyan)
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, meleeTriggerRange);

                // Area serang (magenta), digambar ke kanan sebagai contoh arah
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position + Vector3.right * meleeHitOffset, meleeHitRadius);
            }
        }
    }
}