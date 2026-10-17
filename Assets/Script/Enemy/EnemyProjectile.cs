using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Prefab peluru: Rigidbody2D (Kinematic, Gravity 0) + Collider2D (Is Trigger) + tag "EnemyProjectile"
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyProjectile : MonoBehaviour
    {
        public float lifetime = 5f;
        public LayerMask obstacleMask;   // dinding: peluru hancur saat menabrak

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            Destroy(gameObject, lifetime);
        }

        public void Init(Vector2 dir, float speed)
        {
            rb.velocity = dir * speed;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Shield ditangani ShieldBlocker, abaikan di sini
            if (other.GetComponent<ShieldBlocker>() != null) return;

            if (other.CompareTag("Player"))
            {
                var death = other.GetComponentInParent<PlayerDeath>();
                if (death != null) death.TryHit(transform.position);
                Destroy(gameObject);
                return;
            }

            if (((1 << other.gameObject.layer) & obstacleMask.value) != 0)
                Destroy(gameObject);
        }
    }
}