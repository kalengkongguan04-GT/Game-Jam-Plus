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
        public string projectileTag = "EnemyProjectile";

        public bool IsDead { get; private set; }

        private Vector3 respawnPoint;
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
        }

        public void SetCheckpoint(Vector3 position) => respawnPoint = position;

        // Dipanggil peluru/musuh. Kembalikan true kalau player benar-benar kena.
        public bool TryHit(Vector2 sourcePosition)
        {
            if (IsDead) return false;
            if (invincibleWhileDashing && skills != null && skills.IsDashing) return false;

            // Shield aktif dan serangan datang dari sisi depan shield -> terblok
            if (skills != null && skills.IsShielding && skills.shieldPivot != null)
            {
                Vector2 facing = skills.shieldPivot.right;
                Vector2 toSource = (sourcePosition - (Vector2)transform.position).normalized;
                if (Vector2.Dot(facing, toSource) > 0f) return false;
            }

            Die();
            return true;
        }

        public void Die()
        {
            if (IsDead) return;
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
            yield return new WaitForSeconds(respawnDelay);

            // Hapus semua peluru yang masih melayang
            foreach (var p in GameObject.FindGameObjectsWithTag(projectileTag))
                Destroy(p);

            // Pindah ke checkpoint, isi ulang energi
            transform.position = respawnPoint;
            rb.velocity = Vector2.zero;
            if (energy != null) energy.RefillFull();

            if (skills != null) skills.enabled = true;
            if (controller != null) controller.movementLocked = false;
            IsDead = false;
        }
    }
}