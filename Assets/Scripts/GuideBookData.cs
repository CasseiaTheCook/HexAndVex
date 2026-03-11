using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GuideBookPage
{
    public string title;
    [TextArea(4, 12)] public string bodyText;
    public Sprite illustration; // opsiyonel — null olabilir
    public string category;     // "Combat", "Enemies", "Items", "Perks", "Movement"
}

[CreateAssetMenu(menuName = "HexAndVex/GuideBook Data")]
public class GuideBookData : ScriptableObject
{
    public List<GuideBookPage> pages = new List<GuideBookPage>();
}
