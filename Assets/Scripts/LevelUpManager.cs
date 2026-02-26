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
            GameObject randomPerk = null;

            // --- BENZERSİZ SEÇİM DÖNGÜSÜ ---
            // Seçilen perk zaten listede varsa, farklı bir tane bulana kadar tekrar dene
            int safetyBreak = 0; // Sonsuz döngüye girmemesi için güvenlik kilidi
            while (randomPerk == null || currentChoices.Contains(randomPerk))
            {
                randomPerk = GetRandomPerkByRarity();

                safetyBreak++;
                if (safetyBreak > 100) break;
            }

            currentChoices.Add(randomPerk);

            // Butonun üzerindeki yazıyı ve görseli güncelle
            BasePerk perkScript = randomPerk.GetComponent<BasePerk>();
            choiceTexts[i].text = perkScript.perkName + "\n" + perkScript.description;

            // İkonu eklediysen burayı aktif edebilirsin
            //if (choiceIcons[i] != null && perkScript.perkIcon != null)
                //ChoiceIcons[i].sprite = perkScript.perkIcon;

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
        RunManager.instance.AddPerk(currentChoices[index]);
        levelUpPanel.SetActive(false);

        // --- YENİ SİSTEM: Sahne yükleme, sadece haritayı sıfırla ---
        RunManager.instance.currentLevel++; // Odayı 1 artır
        LevelGenerator.instance.GenerateNextLevel(); // Yeni haritayı ve düşmanları çiz!
    }
}