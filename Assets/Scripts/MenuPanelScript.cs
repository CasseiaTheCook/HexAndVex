using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuPanelScript : BaseMeshEffect
{
    public float hiz = 1.0f;        // Renklerin dönüş hızı
    public float dalgaBoyu = 0.5f;  // Dalgaların birbirine yakınlığı

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> vertices = new List<UIVertex>();
        vh.GetUIVertexStream(vertices);

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex v = vertices[i];

            // Köşenin pozisyonuna göre bir "offset" (kayma) hesaplıyoruz
            // Bu sayede sol taraf farklı, sağ taraf farklı renk alır
            float offset = (v.position.x + v.position.y) * dalgaBoyu * 0.01f;
            
            // Hue değerini zaman + köşe pozisyonuna göre hesapla
            float hue = Mathf.Repeat(Time.time * hiz + (offset * 0.01f), 1f);
            
            // Renk kodunu oluştur (Doygunluk ve Parlaklık 1)
            Color renk = Color.HSVToRGB(hue, 1f, 1f);
            
            // Köşeye yeni rengi ver
            v.color = renk;
            vertices[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertices);
    }

    void Update()
    {
        // Paneli her karede yeniden çizdiriyoruz ki renkler aksın
        if (graphic != null)
            graphic.SetVerticesDirty();
    }
}
