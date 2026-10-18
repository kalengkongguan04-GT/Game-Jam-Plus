using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Prefab kunci: SpriteRenderer + Collider2D (Is Trigger).
    // RoomDoor mengisi field "door" otomatis saat kunci dimunculkan.
    public class KeyPickup : MonoBehaviour
    {
        [HideInInspector] public RoomDoor door;

        [Header("Animasi melayang")]
        public float bobHeight = 0.15f;
        public float bobSpeed = 3f;

        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (door != null) door.CollectKey();
            Destroy(gameObject);
        }
    }
}