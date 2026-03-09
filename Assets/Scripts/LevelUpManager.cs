using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq; 

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
    
    // ==========================================
    // YENİ: Ayrı ayrı Text dizileri (Unity'den atamayı unutma!)
    // ==========================================
    public TMP_Text[] choiceTitleTexts;       // Sadece başlık için (Örn: "Swift Action")
    public TMP_Text[] choiceLevelTexts;       // Sadece level için (Örn: "Lv 2")
    public TMP_Text[] choiceDescriptionTexts; // Sadece açıklama için (Örn: "Grants extra moves...")
    
    public Image[] choiceIcons;

    private List<GameObject> currentChoices = new List<GameObject>();

    [Header("Animasyon Ayarları")]
    public CanvasGroup levelUpCanvasGroup; 

    [Header("Debug")]
    [HideInInspector] public GameObject forcedPerk;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void ShowLevelUpScreen()
    {
        levelUpPanel.SetActive(true);
        if (levelUpCanvasGroup != null) levelUpCanvasGroup.gameObject.SetActive(true);
        currentChoices.Clear();

        bool isBossReward = (RunManager.instance.currentLevel > 0 && RunManager.instance.currentLevel % 5 == 0);

        for (int i = 0; i < 3; i++)
        {
            GameObject randomPerk = null;
            int safetyBreak = 0;

            if (i == 0 && forcedPerk != null && !IsPerkMaxedOut(forcedPerk))
            {
                randomPerk = forcedPerk;
                forcedPerk = null; 
            }

            while (randomPerk == null || currentChoices.Contains(randomPerk) || IsPerkMaxedOut(randomPerk))
            {
                randomPerk = GetRandomPerkByRarity(isBossReward);
                safetyBreak++;
                if (safetyBreak > 50)
                {
                    randomPerk = GetAnyValidFallback();
                    break;
                }
            }

            if (randomPerk != null)
            {
                currentChoices.Add(randomPerk);
                BasePerk perkScript = randomPerk.GetComponent<BasePerk>();
                
                BasePerk existing = RunManager.instance.activePerks.Find(p => p.GetType() == perkScript.GetType());
                int displayLevel = (existing != null) ? existing.currentLevel + 1 : 1;

                // ==========================================
                // YENİ: Yazıları kendi özel Text'lerine aktarıyoruz
                // ==========================================
                if (choiceTitleTexts.Length > i && choiceTitleTexts[i] != null)
                    choiceTitleTexts[i].text = perkScript.perkName;

                if (choiceLevelTexts.Length > i && choiceLevelTexts[i] != null)
                    choiceLevelTexts[i].text = "Lv " + displayLevel.ToString();

                if (choiceDescriptionTexts.Length > i && choiceDescriptionTexts[i] != null)
                    choiceDescriptionTexts[i].text = perkScript.description;

                if (choiceIcons != null && choiceIcons.Length > i && choiceIcons[i] != null)
                {
                    if (perkScript.icon != null)
                    {
                        choiceIcons[i].sprite = perkScript.icon;
                        choiceIcons[i].color = Color.white;
                    }
                    else
                    {
                        choiceIcons[i].sprite = null;
                        choiceIcons[i].color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                    }
                }

                int index = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => SelectPerk(index));
                choiceButtons[i].gameObject.SetActive(true); 
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
        
        StopAllCoroutines();
        StartCoroutine(FadeInAndPopRoutine()); 
    }

    private bool IsPerkMaxedOut(GameObject perkPrefab)
    {
        if (perkPrefab == null || RunManager.instance == null) return true;
        
        BasePerk checkPerk = perkPrefab.GetComponent<BasePerk>();
        BasePerk existing = RunManager.instance.activePerks.Find(p => p.GetType() == checkPerk.GetType());
        
        if (existing != null && existing.currentLevel >= existing.maxLevel)
        {
            return true; 
        }
        return false;
    }

    public GameObject GetRandomPerkByRarity(bool isBossReward)
    {
        if (isBossReward && legendaryPerks.Count > 0) return legendaryPerks[Random.Range(0, legendaryPerks.Count)];
        float roll = Random.Range(0f, 100f);
        if (roll < 10f && epicPerks.Count > 0) return epicPerks[Random.Range(0, epicPerks.Count)]; 
        else if (roll < 40f && rarePerks.Count > 0) return rarePerks[Random.Range(0, rarePerks.Count)]; 
        else if (commonPerks.Count > 0) return commonPerks[Random.Range(0, commonPerks.Count)]; 
        return null;
    }

    private GameObject GetAnyValidFallback()
    {
        List<GameObject> allAvailable = new List<GameObject>();
        allAvailable.AddRange(epicPerks.Where(p => !currentChoices.Contains(p) && !IsPerkMaxedOut(p)));
        allAvailable.AddRange(rarePerks.Where(p => !currentChoices.Contains(p) && !IsPerkMaxedOut(p)));
        allAvailable.AddRange(commonPerks.Where(p => !currentChoices.Contains(p) && !IsPerkMaxedOut(p)));

        if (allAvailable.Count > 0) return allAvailable[Random.Range(0, allAvailable.Count)];
        return null;
    }

    public void SelectPerk(int index)
    {
        foreach (var btn in choiceButtons) btn.interactable = false;

        GameObject chosenPerk = currentChoices[index];
        List<BasePerk> existingPerks = new List<BasePerk>(RunManager.instance.activePerks);
        
        RunManager.instance.AddPerk(chosenPerk);
        RunManager.instance.currentLevel++;

        BasePerk checkScript = chosenPerk.GetComponent<BasePerk>();
        BasePerk activeInstance = RunManager.instance.activePerks.Find(p => p.GetType() == checkScript.GetType());
        
        if (activeInstance != null && activeInstance.currentLevel >= activeInstance.maxLevel)
        {
            if (commonPerks.Contains(chosenPerk)) commonPerks.Remove(chosenPerk);
            if (rarePerks.Contains(chosenPerk)) rarePerks.Remove(chosenPerk);
            if (epicPerks.Contains(chosenPerk)) epicPerks.Remove(chosenPerk);
            if (legendaryPerks.Contains(chosenPerk)) legendaryPerks.Remove(chosenPerk);
            Debug.Log($"🔥 {activeInstance.perkName} Max Seviyeye ulaştı! Havuzdan kalıcı olarak silindi.");
        }

        foreach (var perk in existingPerks)
            if (perk != null) perk.OnLevelStart();

        StartCoroutine(FadeOutAndShrinkRoutine());
    }

    private IEnumerator FadeInAndPopRoutine()
    {
        Transform panelTransform = levelUpPanel.transform; 
        if (levelUpCanvasGroup != null) levelUpCanvasGroup.alpha = 0f;
        panelTransform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        float popDuration = 0.20f; 
        float elapsed = 0f;
        
        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / popDuration;
            float easeOutQuad = 1 - (1 - t) * (1 - t);
            
            if (levelUpCanvasGroup != null) levelUpCanvasGroup.alpha = Mathf.Lerp(0f, 1f, easeOutQuad);
            panelTransform.localScale = Vector3.Lerp(new Vector3(0.2f, 0.2f, 0.2f), new Vector3(1.1f, 1.1f, 1.1f), easeOutQuad);
            yield return null;
        }

        float settleDuration = 0.15f;
        elapsed = 0f;
        
        while (elapsed < settleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / settleDuration;
            panelTransform.localScale = Vector3.Lerp(new Vector3(1.1f, 1.1f, 1.1f), Vector3.one, t);
            yield return null;
        }
        
        if (levelUpCanvasGroup != null) levelUpCanvasGroup.alpha = 1f;
        panelTransform.localScale = Vector3.one;
    }

    private IEnumerator FadeOutAndShrinkRoutine()
    {
        Transform panelTransform = levelUpPanel.transform;
        float duration = 0.2f; 
        float elapsed = 0f;
        
        Vector3 startScale = panelTransform.localScale;
        Vector3 endScale = new Vector3(0.2f, 0.2f, 0.2f); 
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float easeInQuad = t * t;
            
            if (levelUpCanvasGroup != null) levelUpCanvasGroup.alpha = Mathf.Lerp(1f, 0f, easeInQuad);
            panelTransform.localScale = Vector3.Lerp(startScale, endScale, easeInQuad);
            yield return null;
        }
        
        levelUpPanel.SetActive(false);
        if (levelUpCanvasGroup != null) levelUpCanvasGroup.gameObject.SetActive(false);
        foreach (var btn in choiceButtons) btn.interactable = true;

        if (ScreenFader.instance != null)
        {
            ScreenFader.instance.FadeAndLoad(() =>
            {
                LevelGenerator.instance.GenerateNextLevel();
            });
        }
        else
        {
            LevelGenerator.instance.GenerateNextLevel();
        }
    }
}