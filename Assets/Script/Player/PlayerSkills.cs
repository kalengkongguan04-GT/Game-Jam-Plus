using System.Collections;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Objek yang bisa kena laser (musuh, switch, dll) implementasikan interface ini.
    public interface IHittable
    {
        void OnLaserHit();
    }

    [RequireComponent(typeof(PlayerEnergy))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerSkills : MonoBehaviour
    {
        [Header("Input")]
        public int hitMouseButton = 0;          // klik kiri
        public int defMouseButton = 1;          // klik kanan (tahan)
        public KeyCode dashKey = KeyCode.Space;

        [Header("Hit (Laser)")]
        public float hitCostPercent = 75f;      // % dari max energi
        public float laserRange = 12f;
        public float laserWidth = 0.8f;
        public float laserDuration = 0.25f;
        public LayerMask laserHitMask;          // layer musuh/dinding (JANGAN centang layer Player)
        public LineRenderer laserLine;          // opsional, untuk visual

        [Header("Def (Shield)")]
        public float shieldDrainPerSecond = 25f;
        public float shieldMinEnergyToStart = 10f;
        public Transform shieldPivot;           // pivot di tengah player, child "Shield" digeser keluar di sumbu X lokal

        [Header("Dash")]
        public float dashCostPercent = 33f;
        public float dashDistance = 4f;
        public float dashDuration = 0.12f;
        public float dashCooldown = 0.15f;

        public bool IsShielding { get; private set; }
        public bool IsDashing { get; private set; }

        private PlayerEnergy energy;
        private Rigidbody2D rb;
        private TopDownCharacterController controller;
        private Camera cam;
        private float lastDashTime = -99f;
        private bool laserActive;

        private void Awake()
        {
            energy = GetComponent<PlayerEnergy>();
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent<TopDownCharacterController>();
            cam = Camera.main;
            SetShield(false);
            if (laserLine != null) laserLine.enabled = false;
        }

        private void Update()
        {
            if (IsDashing) return;

            HandleShield();

            // Tidak boleh nembak/dash sambil menahan shield
            if (IsShielding) return;

            if (Input.GetMouseButtonDown(hitMouseButton)) TryHit();
            if (Input.GetKeyDown(dashKey)) TryDash();
        }

        // ---------- Util ----------
        private Vector2 AimDirection()
        {
            Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 d = (Vector2)m - (Vector2)transform.position;
            return d.sqrMagnitude < 0.0001f ? Vector2.down : d.normalized;
        }

        // ---------- HIT ----------
        private void TryHit()
        {
            if (laserActive) return;
            float cost = energy.maxEnergy * hitCostPercent / 100f;
            if (!energy.TryConsume(cost)) return;
            StartCoroutine(FireLaser(AimDirection()));
        }

        private IEnumerator FireLaser(Vector2 dir)
        {
            laserActive = true;
            Vector2 origin = transform.position;

            // Damage diberikan sekali di awal tembakan
            RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, laserWidth * 0.5f, dir, laserRange, laserHitMask);
            foreach (var h in hits)
            {
                var target = h.collider.GetComponentInParent<IHittable>();
                if (target != null) target.OnLaserHit();
            }

            if (laserLine != null)
            {
                laserLine.enabled = true;
                laserLine.useWorldSpace = true;
                laserLine.startWidth = laserLine.endWidth = laserWidth;
                laserLine.SetPosition(0, origin);
                laserLine.SetPosition(1, origin + dir * laserRange);
            }

            yield return new WaitForSeconds(laserDuration);

            if (laserLine != null) laserLine.enabled = false;
            laserActive = false;
        }

        // ---------- DEF ----------
        private void HandleShield()
        {
            bool wantShield = Input.GetMouseButton(defMouseButton);

            if (wantShield && !IsShielding && energy.Has(shieldMinEnergyToStart))
                SetShield(true);

            if (IsShielding)
            {
                if (!wantShield)
                {
                    SetShield(false);
                    return;
                }

                // Arahkan shield ke cursor
                float angle = Mathf.Atan2(AimDirection().y, AimDirection().x) * Mathf.Rad2Deg;
                shieldPivot.rotation = Quaternion.Euler(0f, 0f, angle);

                // Energi terkuras sesuai lama pemakaian
                if (!energy.Drain(shieldDrainPerSecond * Time.deltaTime))
                    SetShield(false);
            }
        }

        private void SetShield(bool on)
        {
            IsShielding = on;
            energy.regenPaused = on; // regen berhenti selama shield aktif
            if (shieldPivot != null) shieldPivot.gameObject.SetActive(on);
        }

        // ---------- DASH ----------
        private void TryDash()
        {
            if (Time.time < lastDashTime + dashDuration + dashCooldown) return;
            float cost = energy.maxEnergy * dashCostPercent / 100f;
            if (!energy.TryConsume(cost)) return;
            StartCoroutine(DoDash(AimDirection()));
        }

        private IEnumerator DoDash(Vector2 dir)
        {
            IsDashing = true;
            lastDashTime = Time.time;
            if (controller != null) controller.movementLocked = true;

            rb.velocity = dir * (dashDistance / dashDuration);
            yield return new WaitForSeconds(dashDuration);

            rb.velocity = Vector2.zero;
            if (controller != null) controller.movementLocked = false;
            IsDashing = false;
        }
    }
}