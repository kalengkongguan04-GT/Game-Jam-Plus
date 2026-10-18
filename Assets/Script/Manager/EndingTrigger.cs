using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // OPSIONAL: ending baru muncul saat player MELEWATI pintu, bukan sekadar saat pintu terbuka.
    // Pasang di objek dengan Collider2D (Is Trigger) di balik pintu terakhir.
    public class EndingTrigger : MonoBehaviour
    {
        public EndingScreen ending;
        [Tooltip("Opsional: ending hanya jalan kalau pintu ini sudah terbuka.")]
        public RoomDoor requiredDoor;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (ending == null || !other.CompareTag("Player")) return;
            if (requiredDoor != null && !requiredDoor.IsOpen) return;
            ending.Show();
        }
    }
}