using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Tüm Editor araçlarının kullandığı merkezi UI sabitleri.
/// Renk, boyut, font veya spacing değiştirmek istersen buradan değiştir —
/// tüm Setup araçları buradan okur.
/// </summary>
public static class UIStyle
{
    // ── Renkler ──────────────────────────────────────────────────────────────
    public static readonly Color32 BgDark        = new Color32(0x00, 0x05, 0x0C, 0xFF); // #00050C  — panel/buton arkaplanı
    public static readonly Color32 BgHover       = new Color32(0x00, 0x08, 0x41, 0xFF); // #000841  — hover
    public static readonly Color32 BgPressed     = new Color32(0x00, 0x93, 0xBC, 0xFF); // #0093BC  — pressed
    public static readonly Color32 BgPanel       = new Color32(0x00, 0x00, 0x00, 0xFF); // #000000  — stats/liste panel
    public static readonly Color32 OutlineColor  = new Color32(0x00, 0x05, 0x0C, 0xFF); // #00050C
    public static readonly Color   TextWhite     = Color.white;
    public static readonly Color   TextGold      = new Color(1f, 0.85f, 0.2f, 1f);      // altın — coin display
    public static readonly Color   TextRed       = Color.red;                            // sold out
    public static readonly Color   DividerColor  = new Color(1f, 1f, 1f, 0.15f);
    public static readonly Color   ShopSlotBg    = new Color(0.2f, 0.2f, 0.2f, 0.85f);
    public static readonly Color   SoldOutOverlay= new Color(0f, 0f, 0f, 0.65f);

    // ── Font ─────────────────────────────────────────────────────────────────
    public const string FONT_PATH = "Assets/Star Crush SDF.asset";
    public static TMP_FontAsset LoadFont() =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

    // ── Font boyutları ────────────────────────────────────────────────────────
    public const int FontSizeSmall  = 14;
    public const int FontSizeMid    = 16;
    public const int FontSizeNormal = 18;
    public const int FontSizeLarge  = 20;
    public const int FontSizeTitle  = 26;
    public const int FontSizeHero   = 52;

    // ── Buton ColorBlock ─────────────────────────────────────────────────────
    public static ColorBlock ButtonColors()
    {
        var cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = BgDark;
        cb.highlightedColor = BgHover;
        cb.pressedColor     = BgPressed;
        cb.selectedColor    = BgDark;
        cb.disabledColor    = BgDark;
        cb.colorMultiplier  = 1f;
        return cb;
    }

    // ── Outline ──────────────────────────────────────────────────────────────
    public static Outline AddOutline(GameObject go)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor    = OutlineColor;
        o.effectDistance = new Vector2(1.5f, -1.5f);
        return o;
    }

    // ── Shop boyutları ────────────────────────────────────────────────────────
    public const float ShopSlotSize       = 65f;   // ShopSlot kare boyutu
    public const float ShopPanelWidth     = 280f;
    public const float ShopPanelHeight    = 115f;
    public const float ShopSlotRowHeight  = 65f;
    public const float ShopBottomRowH     = 36f;
    public const float RerollFlexWidth    = 1.5f;
    public const float CoinFlexWidth      = 0.5f;

    // ── Perk List UI boyutları ────────────────────────────────────────────────
    public const float PerkBtnWidth       = 160f;
    public const float PerkBtnHeight      = 45f;
    public const float PerkPanelWidth     = 320f;
    public const int   PerkListFontSize   = 18;
    public const int   PerkBtnFontSize    = 24;

    // ── Stats Panel boyutları ────────────────────────────────────────────────
    public const float StatsPanelWidthDeath  = 480f;
    public const float StatsPanelHeightDeath = 440f;
    public const float StatsPanelWidthPause  = 480f;
    public const float StatsPanelHeightPause = 520f;
    public const float MainMenuBtnWidth      = 260f;
    public const float MainMenuBtnHeight     = 48f;
}
