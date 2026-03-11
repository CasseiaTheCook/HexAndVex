using UnityEngine;
using TMPro;

/// <summary>
/// Zar görselini atlama mekanizmasını kontrol eden sınıf
/// </summary>
public class DiceSkipController : MonoBehaviour
{
    public static DiceSkipController instance;

    public TextMeshProUGUI skipIndicatorUI; // Durumu göstermek için UI
    private Color enabledColor = Color.green;
    private Color disabledColor = Color.white;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateIndicator();
    }

    void Update()
    {
        // E tuşu: Zar görselini atlama mekanizmasını aç/kapat
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (TurnManager.instance != null)
            {
                TurnManager.instance.ToggleSkipDiceVisuals();
                UpdateIndicator();
            }
        }
    }

    private void UpdateIndicator()
    {
        if (skipIndicatorUI != null && TurnManager.instance != null)
        {
            bool isSkipping = TurnManager.instance.GetSkipDiceVisuals();
            skipIndicatorUI.text = isSkipping ? "✓ DICE SKIP: ON" : "DICE SKIP: OFF";
            skipIndicatorUI.color = isSkipping ? enabledColor : disabledColor;
        }
    }
}
