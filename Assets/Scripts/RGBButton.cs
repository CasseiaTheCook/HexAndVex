using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RGBButton : BaseMeshEffect
{
    public float hiz = 1.2f;        
    public float dalgaSikligi = 0.5f; 

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> vertices = new List<UIVertex>();
        vh.GetUIVertexStream(vertices);

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex v = vertices[i];

            // Butonun genişliğine göre renk geçişini ayarla
            float offset = (v.position.x) * dalgaSikligi;
            
            float hue = (Time.time * hiz + (offset * 0.01f)) % 1f;
            
            v.color = Color.HSVToRGB(hue, 0.8f, 1f); // Buton çok göz yormasın diye doygunluğu 0.8 yaptım
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
