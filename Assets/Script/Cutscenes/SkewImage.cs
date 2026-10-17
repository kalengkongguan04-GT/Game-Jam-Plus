using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class SkewImage : BaseMeshEffect
{
    public enum Side { Both, LeftOnly, RightOnly }

    [Tooltip("Pilih sisi mana yang dimiringkan. Sisi lain tetap lurus.")]
    public Side skewSide = Side.Both;

    [Tooltip("Positif = miring ke kanan di bagian atas, negatif = ke kiri.")]
    public float skewX = 100f;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        Rect r = graphic.rectTransform.rect;
        UIVertex v = new UIVertex();

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);

            bool isLeftVertex = v.position.x < r.center.x;

            bool apply = skewSide == Side.Both
                      || (skewSide == Side.LeftOnly && isLeftVertex)
                      || (skewSide == Side.RightOnly && !isLeftVertex);

            if (apply)
            {
                float t = Mathf.InverseLerp(r.yMin, r.yMax, v.position.y);
                v.position.x += Mathf.Lerp(-skewX, skewX, t) * 0.5f;
                vh.SetUIVertex(v, i);
            }
        }
    }
}