using System;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Menyimpan energi pemain. Mulai 100, regen 15 per detik.
    public class PlayerEnergy : MonoBehaviour
    {
        public float maxEnergy = 100f;
        public float regenPerSecond = 15f;

        public float Current { get; private set; }
        public float Normalized => Current / maxEnergy;

        // Diisi oleh PlayerSkills (mis. saat shield aktif, regen berhenti)
        public bool regenPaused;

        // Cheat/testing: energi tidak pernah berkurang
        public bool infinite;

        public event Action<float, float> OnEnergyChanged; // (current, max)

        private void Awake()
        {
            Current = maxEnergy;
        }

        private void Start()
        {
            OnEnergyChanged?.Invoke(Current, maxEnergy);
        }

        private void Update()
        {
            if (regenPaused || Current >= maxEnergy) return;
            Set(Current + regenPerSecond * Time.deltaTime);
        }

        public bool Has(float amount) => infinite || Current >= amount;

        // Pakai energi sekali bayar (Hit, Dash). Gagal kalau tidak cukup.
        public bool TryConsume(float amount)
        {
            if (infinite) return true;
            if (Current < amount) return false;
            Set(Current - amount);
            return true;
        }

        // Pakai energi sedikit demi sedikit (Shield). Kembalikan false kalau habis.
        public bool Drain(float amount)
        {
            if (infinite) return true;
            Set(Current - amount);
            return Current > 0f;
        }

        public void RefillFull() => Set(maxEnergy); // panggil saat respawn di checkpoint

        private void Set(float value)
        {
            Current = Mathf.Clamp(value, 0f, maxEnergy);
            OnEnergyChanged?.Invoke(Current, maxEnergy);
        }
    }
}