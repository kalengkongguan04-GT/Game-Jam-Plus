using UnityEngine;
using UnityEngine.UI;

namespace Cainos.PixelArtTopDown_Basic
{
    // Pasang di Slider (UI). Isi referensi PlayerEnergy.
    [RequireComponent(typeof(Slider))]
    public class EnergyBarUI : MonoBehaviour
    {
        public PlayerEnergy energy;
        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }

        private void OnEnable()  { if (energy != null) energy.OnEnergyChanged += Refresh; }
        private void OnDisable() { if (energy != null) energy.OnEnergyChanged -= Refresh; }

        private void Refresh(float current, float max) => slider.value = current / max;
    }
}