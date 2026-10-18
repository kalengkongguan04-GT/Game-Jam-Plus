using System.Collections;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Mati satu hit, lalu respawn di checkpoint terakhir.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerDeath : MonoBehaviour
    {
        public float respawnDelay = 0.6f;
        public bool invincibleWhileDashing = true;   // i-frame saat dash
        public bool godMode = false;                 // cheat/testing: tidak bisa mati sama sekali
        public float respawnGraceTime = 1f;          // kebal sebentar setelah respawn (detik), 0 = tidak ada
        public string projectileTag = "EnemyProjectile";
        public DeathScreen deathScreen;              // opsional: layar fade + tulisan DEAD (dicari otomatis kalau kosong)

        public bool IsDead { get; private set; }

        private Vector3 respawnPoint;
        private float graceUntil;
        private Rigidbody2D rb;
        private PlayerEnergy energy;
        private PlayerSkills skills;
        private TopDownCharacterController controller;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            energy = GetComponent<PlayerEnergy>();
            skills = GetComponent<PlayerSkills>();
            controller = GetComponent<TopDownCharacterController>();
            respawnPoint = transform.position; // posisi awal = checkpoint pertama
            if (deathScreen == null) deathScreen = FindObjectOfType<DeathScreen>(true);
        }

        public void SetCheckpoint(Vector3 position) => respawnPoint = position;

        // Dipanggil peluru/musuh. Kembalikan true kalau player benar-benar kena.
        // dashCanDodge / shieldCanBlock: apakah dash/shield boleh menyelamatkan player dari serangan ini.
        // shieldIgnoresDirection: true = shield aktif melindungi dari arah mana pun (dipakai jebakan laser).
        public bool TryHit(Vector2 sourcePosition, bool dashCanDodge = true, bool shieldCanBlock = true, bool shieldIgnoresDirection = false)
        {
            if (IsDead || godMode || Time.time < graceUntil) return false;
            if (dashCanDodge && invincibleWhileDashing && skills != null && skills.IsDashing) return false;

            // Shield aktif dan serangan datang dari sisi depan shield -> terblok
            if (shieldCanBlock && skills != null && skills.IsShielding && skills.shieldPivot != null)
            {
                if (shieldIgnoresDirection) return false;
                Vector2 facing = skills.shieldPivot.right;
                Vector2 toSource = (sourcePosition - (Vector2)skills.shieldPivot.position).normalized; // dari titik shield, bukan pusat player
                if (Vector2.Dot(facing, toSource) > 0f) return false;
            }

            Die();
            return true;
        }

        public void Die()
        {
            if (IsDead || godMode) return;
            StartCoroutine(DieRoutine());
        }

        private IEnumerator DieRoutine()
        {
            IsDead = true;

            // Matikan kontrol
            if (skills != null) { skills.ResetState(); skills.enabled = false; }
            if (controller != null) controller.movementLocked = true;
            rb.velocity = Vector2.zero;

            // TODO: animasi/efek mati bisa dimainkan di sini
            yield return new WaitForSeconds(respawnDelay); // jeda singkat supaya player sempat melihat apa yang membunuhnya

            // Layar memudar ke hitam, tulisan DEAD tampil beberapa detik
            if (deathScreen != null) yield return deathScreen.PlayIn();

            // Hapus semua peluru yang masih melayang
            foreach (var p in GameObject.FindGameObjectsWithTag(projectileTag))
                Destroy(p);

            // Pindah ke checkpoint, isi ulang energi
            transform.position = respawnPoint;
            rb.velocity = Vector2.zero;
            if (energy != null) energy.RefillFull();

            // Ulang ruangan yang belum bersih saat checkpoint terakhir (musuh muncul lagi dari awal)
            EnemySpawner.ResetAllUncommitted();
            graceUntil = Time.time + respawnGraceTime;

            // Layar mulai terang lagi (tidak ditunggu, player langsung bisa bergerak)
            if (deathScreen != null) deathScreen.PlayOut();

            if (skills != null) skills.enabled = true;
            if (controller != null) controller.movementLocked = false;
            IsDead = false;
        }
    }
}