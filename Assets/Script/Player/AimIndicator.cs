using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Panah kecil di kaki player yang menunjuk ke arah cursor.
    // Pasang di Player. Isi arrowPivot dengan objek kosong di kaki player,
    // dan sprite panah sebagai child-nya yang digeser di sumbu X lokal.
    public class AimIndicator : MonoBehaviour
    {
        [Header("Panah")]
        public Transform arrowPivot;       // titik putar panah (di kaki player)
        public Transform arrowSprite;      // objek panah (child dari arrowPivot)
        [Min(0f)] public float arrowRadius = 0.6f; // jarak panah dari pivot (satuan lokal), lihat lingkaran di Scene view

        [Header("Titik keluar laser")]
        [Tooltip("Jarak titik keluar laser DI DEPAN pusat panah. 0 = tepat di panah, positif = lebih ke depan, negatif = lebih ke belakang.")]
        public float laserStartOffset = 0.15f;
        public float rotateSpeed = 0f;     // 0 = langsung mengikuti, >0 = halus (derajat/detik, mis. 720)

        [Header("Cursor")]
        public bool hideCursor = true;
        public bool confineCursorToWindow = true; // cursor tidak bisa keluar jendela game
        public bool showCursorWhenPaused = true;  // cursor muncul saat Time.timeScale = 0 (menu pause)

        // Arah bidik terkini, bisa dipakai script lain
        public Vector2 Direction { get; private set; } = Vector2.down;

        private Camera cam;
        private PlayerDeath death;

        private void Awake()
        {
            cam = Camera.main;
            death = GetComponent<PlayerDeath>();
        }

        private void OnEnable()
        {
            ApplyCursorState();
        }

        private void OnDisable()
        {
            // Kembalikan cursor normal kalau script ini dimatikan
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            ApplyCursorState();

            // Arah dihitung dari ArrowPivot. PlayerSkills memakai titik dan arah yang SAMA persis,
            // jadi panah, laser, shield, dan dash selalu searah.
            Direction = ComputeDirection();

            if (arrowPivot == null) return;

            // Sembunyikan panah saat player mati
            bool show = death == null || !death.IsDead;
            if (arrowPivot.gameObject.activeSelf != show) arrowPivot.gameObject.SetActive(show);
            if (!show) return;

            ApplyRadius();

            float target = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
            if (rotateSpeed <= 0f)
            {
                arrowPivot.rotation = Quaternion.Euler(0f, 0f, target);
            }
            else
            {
                float current = arrowPivot.eulerAngles.z;
                float next = Mathf.MoveTowardsAngle(current, target, rotateSpeed * Time.unscaledDeltaTime);
                arrowPivot.rotation = Quaternion.Euler(0f, 0f, next);
            }
        }

        // Dipanggil Unity saat nilai di Inspector berubah, supaya perubahan radius langsung terlihat di Scene view
        private void OnValidate()
        {
            ApplyRadius();
        }

        private void ApplyRadius()
        {
            if (arrowSprite == null) return;
            arrowSprite.localPosition = new Vector3(arrowRadius, 0f, 0f);
        }

        // Lingkaran radius panah, muncul di Scene view saat Player dipilih
        private void OnDrawGizmosSelected()
        {
            if (arrowPivot == null) return;

            // Jarak asli di dunia = radius lokal x scale pivot
            float worldRadius = arrowRadius * Mathf.Abs(arrowPivot.lossyScale.x);

            Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
            Gizmos.DrawWireSphere(arrowPivot.position, worldRadius);

            // Titik pivot
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(arrowPivot.position, worldRadius * 0.08f);

            // Garis arah panah (pivot -> posisi panah)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(arrowPivot.position, arrowPivot.position + arrowPivot.right * worldRadius);

            // Titik keluar laser (merah)
            float muzzleDist = (arrowRadius + laserStartOffset) * Mathf.Abs(arrowPivot.lossyScale.x);
            Vector3 muzzle = arrowPivot.position + arrowPivot.right * muzzleDist;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(muzzle, Mathf.Max(0.05f, worldRadius * 0.12f));
        }

        // Titik asal bidikan = posisi ArrowPivot (kalau belum diisi, pusat player)
        public Vector2 Origin => arrowPivot != null ? (Vector2)arrowPivot.position : (Vector2)transform.position;

        // Hitung arah dari Origin ke cursor saat ini (selalu dihitung ulang, tidak telat satu frame)
        public Vector2 ComputeDirection()
        {
            if (cam == null) cam = Camera.main;

            Vector3 sp = Input.mousePosition;
            sp.z = Mathf.Abs(cam.transform.position.z - transform.position.z); // benar untuk kamera orthographic maupun perspective
            Vector2 mouseWorld = cam.ScreenToWorldPoint(sp);

            Vector2 d = mouseWorld - Origin;
            return d.sqrMagnitude < 0.0001f ? Direction : d.normalized;
        }

        // Titik keluar laser = di panah (radius panah + offset), searah cursor
        public Vector2 GetMuzzlePosition()
        {
            float scale = arrowPivot != null ? Mathf.Abs(arrowPivot.lossyScale.x) : 1f;
            return Origin + ComputeDirection() * (arrowRadius + laserStartOffset) * scale;
        }

        private void ApplyCursorState()
        {
            if (!hideCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            }

            bool paused = showCursorWhenPaused && Time.timeScale == 0f;
            Cursor.visible = paused;
            Cursor.lockState = paused ? CursorLockMode.None
                             : (confineCursorToWindow ? CursorLockMode.Confined : CursorLockMode.None);
        }
    }
}