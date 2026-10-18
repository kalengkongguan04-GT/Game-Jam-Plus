using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Cheat untuk testing. Pasang di Player.
    // Hanya aktif di Unity Editor dan Development Build, otomatis hilang di build final.
    public class DevCheats : MonoBehaviour
    {
        public KeyCode godModeKey = KeyCode.Alpha1;         // tidak bisa mati
        public KeyCode infiniteEnergyKey = KeyCode.Alpha2;  // energi tidak habis
        public KeyCode killAllEnemiesKey = KeyCode.Alpha3;  // hancurkan semua musuh

        private PlayerDeath death;
        private PlayerEnergy energy;

        private void Awake()
        {
            death = GetComponent<PlayerDeath>();
            energy = GetComponent<PlayerEnergy>();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (Input.GetKeyDown(godModeKey) && death != null)
                death.godMode = !death.godMode;

            if (Input.GetKeyDown(infiniteEnergyKey) && energy != null)
            {
                energy.infinite = !energy.infinite;
                if (energy.infinite) energy.RefillFull();
            }

            if (Input.GetKeyDown(killAllEnemiesKey))
            {
                foreach (var e in new List<EnemyAI>(EnemyAI.All))
                    if (e != null) e.OnLaserHit();
            }
        }

        private void OnGUI()
        {
            string text = $"[{godModeKey}] God Mode: {(death != null && death.godMode ? "ON" : "off")}\n" +
                          $"[{infiniteEnergyKey}] Infinite Energy: {(energy != null && energy.infinite ? "ON" : "off")}\n" +
                          $"[{killAllEnemiesKey}] Kill All Enemies";
            GUI.Label(new Rect(10f, 10f, 320f, 80f), text);
        }
#endif
    }
}