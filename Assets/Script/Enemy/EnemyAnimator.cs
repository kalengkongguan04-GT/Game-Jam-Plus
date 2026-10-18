using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    public enum AttackAnimStart
    {
        WindupStart,  // animasi attack mulai bersamaan dengan aba-aba (cocok kalau animasinya sudah termasuk gerak bersiap)
        AttackFire    // animasi attack mulai saat serangan keluar (peluru ditembak / area serang aktif)
    }

    // Pasang di prefab musuh (satu objek dengan EnemyAI).
    // Membaca kecepatan Rigidbody2D dan status EnemyAI tiap frame, lalu mengisi parameter Animator.
    //
    // Dibuat untuk musuh yang hanya punya animasi: Jalan Kanan, Jalan Kiri, dan Attack.
    //   - Idle  = animasi jalan yang dibekukan (freezeWhenIdle)
    //   - Mati  = sprite memudar (fallbackDeathFade) kalau tidak ada trigger Die
    //
    // Parameter Animator (semuanya opsional, yang tidak ada dilewati tanpa error):
    //   Direction (Int)     : 2 = kanan, 3 = kiri
    //   Attack    (Trigger) : serangan
    //   IsMoving  (Bool)    : jalan atau diam (kalau kamu punya animasi idle)
    //   Windup    (Trigger) : aba-aba (kalau kamu punya animasi aba-aba)
    //   Die       (Trigger) : mati (kalau kamu punya animasi mati)
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyAnimator : MonoBehaviour
    {
        [Header("Referensi (kosong = dicari otomatis)")]
        public Animator animator;
        public SpriteRenderer spriteRenderer;

        [Header("Menghadap kiri/kanan")]
        [Tooltip("false = pakai parameter Direction (animasi jalan kiri dan kanan terpisah, ini kasus kamu). true = satu animasi lalu sprite dibalik.")]
        public bool useFlipX = false;
        [Tooltip("(useFlipX) Centang kalau gambar asli sprite menghadap kanan.")]
        public bool spriteFacesRight = true;
        [Tooltip("Kalau arah hadap terbalik di game, centang ini.")]
        public bool invertFacing = false;

        [Header("Gerak")]
        public float moveThreshold = 0.25f;
        public float stopDelay = 0.1f;
        [Tooltip("Animasi jalan dibekukan saat musuh diam (jadi pose idle).")]
        public bool freezeWhenIdle = true;

        [Header("Attack (satu animasi untuk semua arah)")]
        public AttackAnimStart attackAnimStart = AttackAnimStart.WindupStart;
        [Tooltip("Nama state attack di Animator. Dipakai untuk tahu kapan animasi attack sedang main.")]
        public string attackStateName = "Attack";
        [Tooltip("Centang kalau animasi Attack hanya SATU arah, supaya sprite otomatis dibalik saat musuh menyerang ke sisi lain.")]
        public bool attackSingleDirection = false;
        [Tooltip("Arah gambar asli animasi Attack. Dipakai kalau Attack Single Direction dicentang.")]
        public bool attackSpriteFacesRight = true;

        [Header("Mati")]
        [Tooltip("Kalau tidak ada animasi mati: sprite memudar selama Death Destroy Delay di EnemyAI.")]
        public bool fallbackDeathFade = true;

        [Header("Nama parameter Animator")]
        public string moveBool = "IsMoving";
        public string directionInt = "Direction";
        public string windupTrigger = "Windup";
        public string attackTrigger = "Attack";
        public string dieTrigger = "Die";

        private const int DirRight = 2;
        private const int DirLeft = 3;

        private EnemyAI ai;
        private Rigidbody2D rb;
        private Transform player;
        private readonly HashSet<string> parameters = new HashSet<string>();

        private bool facingRight = true;
        private bool lastRight = true;
        private float lastMoveTime = -10f;
        private float runUntil;           // animator dipaksa jalan sampai waktu ini (supaya transisi bisa terjadi)
        private float attackStartedTime = -10f;

        private void Awake()
        {
            ai = GetComponent<EnemyAI>();
            rb = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (animator != null)
                foreach (var p in animator.parameters) parameters.Add(p.name);
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        private void OnEnable()
        {
            if (ai == null) ai = GetComponent<EnemyAI>();
            ai.OnWindupStart += HandleWindup;
            ai.OnAttackFire += HandleFire;
            ai.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (ai == null) return;
            ai.OnWindupStart -= HandleWindup;
            ai.OnAttackFire -= HandleFire;
            ai.OnDied -= HandleDied;
        }

        private void Update()
        {
            if (animator == null || ai.IsDead) return;

            bool attackAnim = IsAttackAnimActive();

            // ---- Jalan atau diam ----
            bool moving = false;
            if (!ai.IsAttacking)
            {
                if (rb.velocity.magnitude > moveThreshold) lastMoveTime = Time.time;
                moving = Time.time - lastMoveTime <= stopDelay;
            }
            SetBool(moveBool, moving);

            // ---- Arah hadap ----
            if (!ai.IsAttacking)
            {
                if (ai.IsChasing && player != null)
                {
                    float dx = player.position.x - transform.position.x;
                    if (Mathf.Abs(dx) > 0.05f) facingRight = dx > 0f;
                }
                else if (Mathf.Abs(rb.velocity.x) > moveThreshold)
                {
                    facingRight = rb.velocity.x > 0f;
                }
            }

            bool right = invertFacing ? !facingRight : facingRight;

            // Arah berubah: beri waktu animator untuk pindah state walau sedang dibekukan
            if (right != lastRight)
            {
                runUntil = Time.time + 0.2f;
                lastRight = right;
            }

            if (useFlipX)
            {
                if (spriteRenderer != null)
                    spriteRenderer.flipX = spriteFacesRight ? !right : right;
            }
            else
            {
                SetInt(directionInt, right ? DirRight : DirLeft);

                // Animasi attack satu arah: balik sprite hanya selama animasi attack
                if (attackSingleDirection && spriteRenderer != null)
                    spriteRenderer.flipX = attackAnim && (attackSpriteFacesRight ? !right : right);
            }

            // ---- Idle: bekukan animasi jalan ----
            if (freezeWhenIdle)
            {
                bool run = moving || attackAnim || Time.time < runUntil || animator.IsInTransition(0);
                animator.speed = run ? 1f : 0f;
            }
        }

        private bool IsAttackAnimActive()
        {
            if (Time.time - attackStartedTime < 0.15f) return true;
            if (string.IsNullOrEmpty(attackStateName)) return ai.IsAttacking;

            return animator.GetCurrentAnimatorStateInfo(0).IsName(attackStateName)
                || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(attackStateName));
        }

        // ---------- Event dari EnemyAI ----------
        private void HandleWindup()
        {
            SetTrigger(windupTrigger);
            if (attackAnimStart == AttackAnimStart.WindupStart) PlayAttack();
        }

        private void HandleFire()
        {
            if (attackAnimStart == AttackAnimStart.AttackFire) PlayAttack();
        }

        private void PlayAttack()
        {
            attackStartedTime = Time.time;
            runUntil = Time.time + 0.2f;
            if (animator != null) animator.speed = 1f; // animator yang beku tidak bisa pindah state
            SetTrigger(attackTrigger);
        }

        private void HandleDied()
        {
            SetBool(moveBool, false);

            if (animator != null && parameters.Contains(dieTrigger))
            {
                animator.speed = 1f;
                SetTrigger(dieTrigger);
            }
            else if (fallbackDeathFade && spriteRenderer != null)
            {
                if (animator != null) animator.speed = 0f; // bekukan di pose terakhir
                StartCoroutine(FadeOut(ai.deathDestroyDelay));
            }
        }

        private IEnumerator FadeOut(float duration)
        {
            if (duration <= 0f) yield break;

            Color c = spriteRenderer.color;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                c.a = 1f - Mathf.Clamp01(t / duration);
                spriteRenderer.color = c;
                yield return null;
            }
        }

        // ---------- Helper aman: parameter yang tidak ada dilewati ----------
        private void SetBool(string name, bool value)
        {
            if (animator != null && parameters.Contains(name)) animator.SetBool(name, value);
        }

        private void SetInt(string name, int value)
        {
            if (animator != null && parameters.Contains(name)) animator.SetInteger(name, value);
        }

        private void SetTrigger(string name)
        {
            if (animator != null && parameters.Contains(name)) animator.SetTrigger(name);
        }
    }
}