using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RGBText : BaseMeshEffect
{
    public float hiz = 1.5f;        // Renklerin akış hızı
    public float dalgaBoyu = 0.8f;  // Harfler arasındaki renk farkı

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> vertices = new List<UIVertex>();
        vh.GetUIVertexStream(vertices);

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex v = vertices[i];

            // Yazının her bir harfinin köşesine göre bir kayma hesaplıyoruz
            float offset = (v.position.x + v.position.y) * dalgaBoyu;
            
            // Zaman + Pozisyon = Dalga Efekti
            float hue = (Time.time * hiz + (offset * 0.01f)) % 1f;
            
            // Rengi ata
            v.color = Color.HSVToRGB(hue, 1f, 1f);
            vertices[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertices);
    }

    void Update()
    {
        if (graphic != null)
            graphic.SetVerticesDirty();
    }
}
