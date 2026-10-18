using System.Collections;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Pasang di Player (objek yang sama dengan PlayerSkills).
    //
    // Shield dan Dash: suara mengikuti status skill. Dibaca tiap frame dari PlayerSkills.IsShielding / IsDashing,
    // jadi suara mulai saat skill mulai dan BERHENTI saat skill selesai (termasuk saat energi habis atau player mati).
    // Laser: dipanggil dari PlayerSkills lewat PlayLaser() di titik laser ditembakkan.
    [RequireComponent(typeof(PlayerSkills))]
    public class SkillSounds : MonoBehaviour
    {
        [Header("Laser")]
        public AudioClip laserClip;
        [Range(0f, 1f)] public float laserVolume = 0.8f;
        [Tooltip("Variasi nada acak supaya tembakan beruntun tidak terdengar monoton. 1 dan 1 = tanpa variasi.")]
        public Vector2 laserPitchRange = new Vector2(0.95f, 1.05f);

        [Header("Dash")]
        public AudioClip dashClip;
        [Range(0f, 1f)] public float dashVolume = 0.8f;
        [Tooltip("Centang kalau suara dash panjang dan harus dipotong saat dash selesai.")]
        public bool cutDashOnEnd = false;

        [Header("Shield")]
        public AudioClip shieldStartClip;   // opsional: bunyi singkat saat shield muncul
        public AudioClip shieldLoopClip;    // suara dengung, diulang selama shield aktif
        public AudioClip shieldEndClip;     // opsional: bunyi singkat saat shield hilang
        [Range(0f, 1f)] public float shieldVolume = 0.6f;
        [Tooltip("Lama suara dengung memudar saat shield hilang (detik). 0 = langsung berhenti.")]
        public float shieldFadeOut = 0.1f;
        [Tooltip("Centang = suara Shield Start Clip dipotong saat shield hilang (pakai ini kalau suaranya panjang).")]
        public bool cutShieldStartOnEnd = true;

        [Header("Debug")]
        [Tooltip("Tulis log di Console setiap shield/dash mulai dan berhenti. Berguna untuk mengecek sinkronisasi.")]
        public bool debugLog = false;

        private PlayerSkills skills;
        private PlayerDeath death;

        private AudioSource laserSource;
        private AudioSource dashSource;
        private AudioSource shieldLoopSource;
        private AudioSource shieldStartSource;
        private AudioSource shieldEndSource;

        private bool wasShielding;
        private bool wasDashing;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            skills = GetComponent<PlayerSkills>();
            death = GetComponent<PlayerDeath>();

            laserSource = CreateSource(false);
            dashSource = CreateSource(false);
            shieldLoopSource = CreateSource(true);
            shieldStartSource = CreateSource(false);
            shieldEndSource = CreateSource(false);
        }

        private AudioSource CreateSource(bool loop)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f; // 2D, volume tidak tergantung jarak
            return src;
        }

        private void Update()
        {
            bool alive = death == null || !death.IsDead;
            bool active = alive && skills != null && skills.enabled;

            bool shielding = active && skills.IsShielding;
            if (shielding && !wasShielding) OnShieldStart();
            else if (!shielding && wasShielding) OnShieldEnd();
            wasShielding = shielding;

            bool dashing = active && skills.IsDashing;
            if (dashing && !wasDashing) OnDashStart();
            else if (!dashing && wasDashing) OnDashEnd();
            wasDashing = dashing;

            // Pengaman: shield sudah tidak aktif tapi suara shield masih jalan -> matikan
            if (!shielding && fadeRoutine == null &&
                (shieldLoopSource.isPlaying || (cutShieldStartOnEnd && shieldStartSource.isPlaying)))
            {
                shieldLoopSource.Stop();
                if (cutShieldStartOnEnd) shieldStartSource.Stop();
            }

            // Game di-pause: tahan dengung shield supaya tidak terus bunyi di menu
            if (shieldLoopSource.clip != null)
            {
                if (Time.timeScale == 0f) shieldLoopSource.Pause();
                else shieldLoopSource.UnPause();
            }
        }

        // ---------- Laser: panggil dari PlayerSkills saat laser ditembakkan ----------
        public void PlayLaser()
        {
            if (laserClip == null) return;
            laserSource.pitch = Random.Range(laserPitchRange.x, laserPitchRange.y);
            laserSource.PlayOneShot(laserClip, laserVolume);
        }

        // ---------- Dash ----------
        private void OnDashStart()
        {
            if (debugLog) Debug.Log("[SkillSounds] Dash START", this);
            if (dashClip == null) return;
            dashSource.clip = dashClip;
            dashSource.volume = dashVolume;
            dashSource.Play();
        }

        private void OnDashEnd()
        {
            if (debugLog) Debug.Log("[SkillSounds] Dash END", this);
            if (cutDashOnEnd) dashSource.Stop();
        }

        // ---------- Shield ----------
        private void OnShieldStart()
        {
            if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }

            if (debugLog) Debug.Log("[SkillSounds] Shield START", this);

            if (shieldStartClip != null)
            {
                shieldStartSource.clip = shieldStartClip; // pakai Play() (bukan PlayOneShot) supaya bisa dihentikan
                shieldStartSource.volume = shieldVolume;
                shieldStartSource.Play();
            }

            if (shieldLoopClip != null)
            {
                shieldLoopSource.clip = shieldLoopClip;
                shieldLoopSource.volume = shieldVolume;
                shieldLoopSource.Play();
            }
        }

        private void OnShieldEnd()
        {
            if (debugLog) Debug.Log("[SkillSounds] Shield END", this);

            if (cutShieldStartOnEnd) shieldStartSource.Stop();
            if (shieldEndClip != null) shieldEndSource.PlayOneShot(shieldEndClip, shieldVolume);

            if (shieldFadeOut <= 0f || !isActiveAndEnabled) shieldLoopSource.Stop();
            else fadeRoutine = StartCoroutine(FadeOutLoop());
        }

        private IEnumerator FadeOutLoop()
        {
            float start = shieldLoopSource.volume;
            float t = 0f;
            while (t < shieldFadeOut)
            {
                t += Time.unscaledDeltaTime;
                shieldLoopSource.volume = Mathf.Lerp(start, 0f, t / shieldFadeOut);
                yield return null;
            }
            shieldLoopSource.Stop();
            fadeRoutine = null;
        }

        // Pastikan tidak ada suara tersisa kalau script ini dimatikan
        private void OnDisable()
        {
            if (shieldLoopSource != null) shieldLoopSource.Stop();
            if (shieldStartSource != null) shieldStartSource.Stop();
            if (dashSource != null && cutDashOnEnd) dashSource.Stop();
            wasShielding = false;
            wasDashing = false;
        }
    }
}