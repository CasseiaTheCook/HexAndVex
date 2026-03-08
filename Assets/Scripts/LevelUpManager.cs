using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq; // Listeleri filtrelemek için eklendi

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

        // YENİ: Şu an biten bölüm Boss bölümü mü? (5, 10, 15...)
        bool isBossReward = (RunManager.instance.currentLevel > 0 && RunManager.instance.currentLevel % 5 == 0);

        for (int i = 0; i < 3; i++)
        {
            GameObject randomPerk = null;
            int safetyBreak = 0; 
            
            // Seçilen perk zaten listede varsa, farklı bir tane bulana kadar dene
            while (randomPerk == null || currentChoices.Contains(randomPerk))
            {
                randomPerk = GetRandomPerkByRarity(isBossReward);

                safetyBreak++;
                if (safetyBreak > 50) 
                {
                    // EĞER HAVUZDA YETERİNCE KART KALMADIYSA (Örn: Sadece 2 Legendary kaldıysa)
                    // Oyunu dondurmamak için eldeki diğer yeteneklere yönel.
                    randomPerk = GetAnyValidFallback();
                    break;
                }
            }

            if (randomPerk != null)
            {
                currentChoices.Add(randomPerk);

                BasePerk perkScript = randomPerk.GetComponent<BasePerk>();
                choiceTexts[i].text = perkScript.perkName + "\n" + perkScript.description;

                //if (choiceIcons[i] != null && perkScript.perkIcon != null)
                //    choiceIcons[i].sprite = perkScript.perkIcon;

                int index = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => SelectPerk(index));
                
                choiceButtons[i].gameObject.SetActive(true); // Butonu aç
            }
            else
            {
                // Oyundaki BÜTÜN perkleri aldıysa ve destede kart kalmadıysa butonu kapat
                choiceButtons[i].gameObject.SetActive(false); 
            }
        }
    }

    public GameObject GetRandomPerkByRarity(bool isBossReward)
    {
        // 1. BOSS KONTROLÜ (Garantili Legendary)
        if (isBossReward && legendaryPerks.Count > 0)
        {
            return legendaryPerks[Random.Range(0, legendaryPerks.Count)];
        }

        // 2. NORMAL ZAR (%60 Common, %30 Rare, %10 Epic)
        float roll = Random.Range(0f, 100f);

        if (roll < 10f && epicPerks.Count > 0) 
        {
            return epicPerks[Random.Range(0, epicPerks.Count)]; // %10 İhtimal (0-10 arası)
        }
        else if (roll < 40f && rarePerks.Count > 0) 
        {
            return rarePerks[Random.Range(0, rarePerks.Count)]; // %30 İhtimal (10-40 arası)
        }
        else if (commonPerks.Count > 0)
        {
            return commonPerks[Random.Range(0, commonPerks.Count)]; // %60 İhtimal (40-100 arası)
        }

        return null;
    }

    // YEDEK PLAN: Eğer istenen nadirlikte kart bittiyse (veya Boss'ta yeterli Legendary kalmadıysa) 
    // elde olan, henüz ekranda olmayan ilk kartı ver.
    private GameObject GetAnyValidFallback()
    {
        List<GameObject> allAvailable = new List<GameObject>();
        
        allAvailable.AddRange(epicPerks.Where(p => !currentChoices.Contains(p)));
        allAvailable.AddRange(rarePerks.Where(p => !currentChoices.Contains(p)));
        allAvailable.AddRange(commonPerks.Where(p => !currentChoices.Contains(p)));
        // Legendaryleri normal havuza bilerek katmadım, o sadece Boss'ta çıksın diye. İstersen ekleyebilirsin.

        if (allAvailable.Count > 0)
        {
            return allAvailable[Random.Range(0, allAvailable.Count)];
        }
        return null;
    }

    public void SelectPerk(int index)
    {
        GameObject chosenPerk = currentChoices[index];

        // =======================================================
        // YENİ: KARTI DESTEDEN YIRTIP ATMA SİSTEMİ (TEK SEFERLİK)
        // Seçilen kart ait olduğu listeden kalıcı olarak silinir.
        // =======================================================
        if (commonPerks.Contains(chosenPerk)) commonPerks.Remove(chosenPerk);
        if (rarePerks.Contains(chosenPerk)) rarePerks.Remove(chosenPerk);
        if (epicPerks.Contains(chosenPerk)) epicPerks.Remove(chosenPerk);
        if (legendaryPerks.Contains(chosenPerk)) legendaryPerks.Remove(chosenPerk);

        List<BasePerk> existingPerks = new List<BasePerk>(RunManager.instance.activePerks);

        RunManager.instance.AddPerk(chosenPerk);
        levelUpPanel.SetActive(false);

        RunManager.instance.currentLevel++; 

        foreach (var perk in existingPerks)
            if (perk != null) perk.OnLevelStart();

        LevelGenerator.instance.GenerateNextLevel(); 
    }
}