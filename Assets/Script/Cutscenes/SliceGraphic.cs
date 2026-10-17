using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Slice Graphic")]
public class SliceGraphic : Graphic
{
    [Tooltip("Positif = miring ke kanan (/), negatif = miring ke kiri (\\)")]
    [SerializeField] private float slant = 120f;

    public float Slant
    {
        get { return slant; }
        set { slant = value; SetVerticesDirty(); }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = GetPixelAdjustedRect();
        float s = Mathf.Clamp(Mathf.Abs(slant), 0f, r.width * 0.9f);

        Vector2 bl, br, tl, tr;
        if (slant >= 0f)
        {
            bl = new Vector2(r.xMin, r.yMin);
            br = new Vector2(r.xMax - s, r.yMin);
            tl = new Vector2(r.xMin + s, r.yMax);
            tr = new Vector2(r.xMax, r.yMax);
        }
        else
        {
            bl = new Vector2(r.xMin + s, r.yMin);
            br = new Vector2(r.xMax, r.yMin);
            tl = new Vector2(r.xMin, r.yMax);
            tr = new Vector2(r.xMax - s, r.yMax);
        }

        UIVertex[] quad = new UIVertex[4];
        Vector2[] pos = { bl, tl, tr, br };
        for (int i = 0; i < 4; i++)
        {
            quad[i] = UIVertex.simpleVert;
            quad[i].position = pos[i];
            quad[i].color = color;
        }

        vh.AddUIVertexQuad(quad);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
