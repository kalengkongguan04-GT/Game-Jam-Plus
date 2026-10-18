using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Objek dengan Collider2D (Is Trigger). Menyentuh = checkpoint tersimpan.
    public class Checkpoint : MonoBehaviour
    {
        public SpriteRenderer sprite;          // opsional: berubah warna saat aktif
        public Color activeColor = Color.cyan;

        private bool activated;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated || !other.CompareTag("Player")) return;

            var death = other.GetComponentInParent<PlayerDeath>();
            if (death == null) return;

            death.SetCheckpoint(transform.position);
            EnemySpawner.CommitAllCleared(); // ruangan yang sudah bersih sampai titik ini tidak akan diulang
            activated = true;
            if (sprite != null) sprite.color = activeColor;
        }
    }
}