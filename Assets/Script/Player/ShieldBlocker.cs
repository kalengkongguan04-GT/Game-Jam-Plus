using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Pasang di child "Shield" (Collider2D dengan Is Trigger dicentang).
    // Peluru musuh harus ber-tag sesuai projectileTag dan punya Rigidbody2D.
    public class ShieldBlocker : MonoBehaviour
    {
        public string projectileTag = "EnemyProjectile";

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(projectileTag)) return;
            Destroy(other.gameObject); // nanti bisa diganti efek pantul/partikel
        }
    }
}