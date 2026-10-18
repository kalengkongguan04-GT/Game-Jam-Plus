using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    public class TopDownCharacterController : MonoBehaviour
    {
        public float speed;

        // Dikunci oleh PlayerSkills saat dash supaya velocity dash tidak ditimpa WASD
        [HideInInspector] public bool movementLocked;

        [SerializeField] private Animator animator;

        [Header("Animasi (hanya Idle, Jalan Kiri, Jalan Kanan)")]
        [Tooltip("true = karakter menghadap sisi kursor (kiri/kanan), false = menghadap arah jalan terakhir.")]
        public bool faceTowardCursor = false;
        [Tooltip("Tulis log di Console setiap kali arah hadap berubah, lengkap dengan status tombol.")]
        public bool debugAnimation = false;

        [Header("Opsional - Sound Effect Langkah Kaki")]
        public AudioClip footstepSound;
        public float footstepInterval = 0.35f; // jarak waktu antar bunyi langkah, detik
        [Range(0f, 1f)] public float footstepVolume = 0.7f;


        private AudioSource footstepSource; // khusus footstep, terpisah dari AudioManager, supaya bisa di-Stop() langsung
        private float footstepTimer = 0f;
        private Rigidbody2D rb;
        private AimIndicator aim;

        // Kode arah untuk parameter "Direction" di Animator: 2 = kanan, 3 = kiri
        // (0 = bawah dan 1 = atas dicadangkan kalau nanti ada animasi atas/bawah)
        private const int DirRight = 2;
        private const int DirLeft = 3;
        private int facingDirection = DirRight;
        private int lastDirLogged = DirRight;

        // Tombol yang TERAKHIR ditekan menang kalau dua tombol berlawanan tertahan bersamaan
        private int lastHorizontal = 1;   // -1 kiri, 1 kanan
        private int lastVertical = 1;     // -1 bawah, 1 atas

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            aim = GetComponent<AimIndicator>();

            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.volume = footstepVolume;

            if (animator != null) animator.SetInteger("Direction", facingDirection);
        }


        private void Update()
        {
            // Saat dash, jangan proses input gerak
            if (movementLocked)
            {
                HandleFootstepSound(false);
                return;
            }

            Vector2 dir = Vector2.zero;

            // Tombol yang terakhir ditekan menang. Jadi saat berganti arah tanpa melepas tombol lama
            // (mis. masih menahan A lalu menekan D), karakter langsung berbalik arah.
            if (Input.GetKeyDown(KeyCode.A)) lastHorizontal = -1;
            if (Input.GetKeyDown(KeyCode.D)) lastHorizontal = 1;
            if (Input.GetKeyDown(KeyCode.W)) lastVertical = 1;
            if (Input.GetKeyDown(KeyCode.S)) lastVertical = -1;

            bool left = Input.GetKey(KeyCode.A);
            bool right = Input.GetKey(KeyCode.D);
            bool up = Input.GetKey(KeyCode.W);
            bool down = Input.GetKey(KeyCode.S);

            if (left && right) dir.x = lastHorizontal;
            else if (left) dir.x = -1;
            else if (right) dir.x = 1;

            if (up && down) dir.y = lastVertical;
            else if (up) dir.y = 1;
            else if (down) dir.y = -1;

            // Arah hadap hanya ditentukan gerak kiri/kanan. Naik/turun tidak mengubahnya,
            // jadi saat jalan ke atas/bawah karakter tetap memakai animasi kiri/kanan terakhir.
            if (dir.x < 0) facingDirection = DirLeft;
            else if (dir.x > 0) facingDirection = DirRight;

            // Opsional: menghadap sisi kursor, bukan arah jalan
            if (faceTowardCursor && aim != null)
            {
                float aimX = aim.ComputeDirection().x;
                if (Mathf.Abs(aimX) > 0.05f) facingDirection = aimX < 0f ? DirLeft : DirRight;
            }

            dir.Normalize();
            bool isMoving = dir.magnitude > 0;

            HandleFootstepSound(isMoving);

            rb.velocity = speed * dir;

            if (debugAnimation && facingDirection != lastDirLogged)
            {
                Debug.Log($"[Anim] Direction {lastDirLogged} -> {facingDirection} | A:{left} D:{right} | faceTowardCursor:{faceTowardCursor}");
                lastDirLogged = facingDirection;
            }

            if (animator != null)
            {
                animator.SetInteger("Direction", facingDirection);
                animator.SetBool("IsRunning", isMoving);
            }
        }

        private void HandleFootstepSound(bool isMoving)
        {
            if (!isMoving)
            {
                footstepTimer = 0f; // biar begitu jalan lagi, langsung bunyi (bukan nunggu sisa interval lama)

                if (footstepSource.isPlaying)
                {
                    footstepSource.Stop(); // langsung berhenti, tidak menunggu clip selesai sendiri
                }

                return;
            }

            footstepTimer += Time.deltaTime;

            if (footstepTimer >= footstepInterval)
            {
                footstepTimer = 0f;

                if (footstepSound != null)
                {
                    footstepSource.PlayOneShot(footstepSound, footstepVolume);
                }
            }
        }
    }
}