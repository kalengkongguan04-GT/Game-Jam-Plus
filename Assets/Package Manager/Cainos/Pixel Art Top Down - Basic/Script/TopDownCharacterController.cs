using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    public class TopDownCharacterController : MonoBehaviour
    {
        public float speed;

        // BARU: dikunci oleh PlayerSkills saat dash supaya velocity dash tidak ditimpa WASD
        [HideInInspector] public bool movementLocked;

        [SerializeField] private Animator animator;

        [Header("Opsional - Sound Effect Langkah Kaki")]
        public AudioClip footstepSound;
        public float footstepInterval = 0.35f; // jarak waktu antar bunyi langkah, detik
        [Range(0f, 1f)] public float footstepVolume = 0.7f;


        private AudioSource footstepSource; // khusus footstep, terpisah dari AudioManager, supaya bisa di-Stop() langsung
        private float footstepTimer = 0f;
        private Rigidbody2D rb; // BARU: di-cache, tidak GetComponent tiap frame

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.volume = footstepVolume;
        }


        private void Update()
        {
            // BARU: saat dash, jangan proses input gerak
            if (movementLocked)
            {
                HandleFootstepSound(false);
                return;
            }

            Vector2 dir = Vector2.zero;
            if (Input.GetKey(KeyCode.A))
            {
                dir.x = -1;
                animator.SetInteger("Direction", 3);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                dir.x = 1;
                animator.SetInteger("Direction", 2);
            }

            if (Input.GetKey(KeyCode.W))
            {
                dir.y = 1;
                animator.SetInteger("Direction", 1);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                dir.y = -1;
                animator.SetInteger("Direction", 0);
            }

            dir.Normalize();
            bool isMoving = dir.magnitude > 0;

            HandleFootstepSound(isMoving);

            rb.velocity = speed * dir;

            animator.SetBool("IsRunning", isMoving);
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