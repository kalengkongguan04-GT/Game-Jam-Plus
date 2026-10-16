using UnityEngine;
using Cainos.PixelArtTopDown_Basic;

public class SimpleEnemy : MonoBehaviour, IHittable
{
    public void OnLaserHit()
    {
        Destroy(gameObject); // ganti dengan animasi mati, dll.
    }
}