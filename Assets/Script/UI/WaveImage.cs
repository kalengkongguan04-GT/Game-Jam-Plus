using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class WaveImage : BaseMeshEffect
{
    [SerializeField] private float amplitude = 6f;    // seberapa jauh bergoyang (pixel)
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float waveLength = 300f; // makin besar, makin landai
    [Tooltip("Bagian bawah dibuat diam, atas bergoyang")]
    [SerializeField] private bool anchorBottom = true;

    private float time;

    private void Update()
    {
        time += Time.unscaledDeltaTime * speed;
        if (graphic != null) graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        Rect r = graphic.rectTransform.rect;
        UIVertex v = new UIVertex();

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);

            float h = Mathf.InverseLerp(r.yMin, r.yMax, v.position.y); // 0 bawah, 1 atas
            float weight = anchorBottom ? h : 1f;

            v.position.x += Mathf.Sin(time + v.position.y / waveLength) * amplitude * weight;

            vh.SetUIVertex(v, i);
        }
    }
}