using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager instance;

    public GameObject levelUpPanel;
    
    [Header("Perk Listeleri")]
    public List<GameObject> commonPerks;
    public List<GameObject> rarePerks;
    public List<GameObject> epicPerks;
    public List<GameObject> legendaryPerks;

    [Header("UI Elemanları (3 Buton)")]
    public Button[] choiceButtons;
    public TMP_Text[] choiceTexts;
    public Image[] choiceIcons;

    private List<GameObject> currentChoices = new List<GameObject>();

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void ShowLevelUpScreen()
    {
        levelUpPanel.SetActive(true);
        currentChoices.Clear();

        for (int i = 0; i < 3; i++)
        {
            GameObject randomPerk = GetRandomPerkByRarity();
            currentChoices.Add(randomPerk);

            // Butonun üzerindeki yazıyı ve görseli güncelle
            BasePerk perkScript = randomPerk.GetComponent<BasePerk>();
            choiceTexts[i].text = perkScript.perkName + "\n" + perkScript.description;
            // choiceIcons[i].sprite = perkScript.perkIcon; // Eğer ikonu eklediysen açarsın
            
            // Butona tıklama olayını temizle ve yenisini ata
            int index = i; 
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => SelectPerk(index));
        }
    }

    private GameObject GetRandomPerkByRarity()
    {
        float roll = Random.Range(0f, 100f);

        // Balatro tarzı ağırlıklı şans (Common %60, Rare %25, Epic %10, Legendary %5)
        if (roll < 5f && legendaryPerks.Count > 0) return legendaryPerks[Random.Range(0, legendaryPerks.Count)];
        if (roll < 15f && epicPerks.Count > 0) return epicPerks[Random.Range(0, epicPerks.Count)];
        if (roll < 40f && rarePerks.Count > 0) return rarePerks[Random.Range(0, rarePerks.Count)];
        
        return commonPerks[Random.Range(0, commonPerks.Count)];
    }

    public void SelectPerk(int index)
    {
        // Seçilen perki RunManager'a ekle
        RunManager.instance.AddPerk(currentChoices[index]);
        
        levelUpPanel.SetActive(false);
        
        // Şimdilik tak diye diğer sahneye geçiyoruz (Fade yok)
        int nextScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(nextScene);
    }
}