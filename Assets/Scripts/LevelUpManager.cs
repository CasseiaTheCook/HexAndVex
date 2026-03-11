using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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

    public TMP_Text[] choiceTitleTexts;
    public TMP_Text[] choiceLevelTexts;
    public TMP_Text[] choiceDescriptionTexts;
    public TMP_Text[] choiceRarityTexts;

    public Image[] choiceIcons;

    [Header("Reroll Butonu")]
    public Button rerollPerkButton; // Inspector'dan bağla

    private List<GameObject> currentChoices = new List<GameObject>();

    [Header("Animasyon Ayarları")]
    public CanvasGroup levelUpCanvasGroup; 

    [Header("Debug")]
    [HideInInspector] public GameObject forcedPerk;

    private int hoveredCardIndex = -1;
    private bool[] cardAnimDone;

    void Awake()
    {
        if (instance == null) instance = this;
        SetupCardHoverListeners();
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
                // forcedPerk max seviyeye ulaşınca SelectPerk'te otomatik temizlenir
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

                if (choiceRarityTexts != null && choiceRarityTexts.Length > i && choiceRarityTexts[i] != null)
                {
                    PerkRarity detectedRarity = GetRarityFromList(randomPerk);
                    choiceRarityTexts[i].text = detectedRarity.ToString().ToUpperInvariant();
                    choiceRarityTexts[i].color = GetRarityColor(detectedRarity);
                }

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
        
        // Reroll butonu
        if (rerollPerkButton != null)
        {
            rerollPerkButton.gameObject.SetActive(RunManager.instance.hasPerkReroll);
            rerollPerkButton.onClick.RemoveAllListeners();
            rerollPerkButton.onClick.AddListener(RerollPerkChoices);
        }

        Time.timeScale = 0f;
        StopAllCoroutines();
        StartCoroutine(FadeInAndPopRoutine());
    }

    public void RerollPerkChoices()
    {
        if (RunManager.instance == null || !RunManager.instance.hasPerkReroll) return;
        RunManager.instance.hasPerkReroll = false;
        if (rerollPerkButton != null) rerollPerkButton.gameObject.SetActive(false);

        // Mevcut seçimleri temizleyip yeniden göster
        currentChoices.Clear();
        StopAllCoroutines();
        ShowLevelUpScreen();
    }

    private bool IsPerkMaxedOut(GameObject perkPrefab)
    {
        if (perkPrefab == null || RunManager.instance == null) return true;
        
        BasePerk checkPerk = perkPrefab.GetComponent<BasePerk>();

        // CanBeOffered kontrolü — koşullu perkler (GeneSplice vb.)
        if (!checkPerk.CanBeOffered()) return true;

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

        // Lv0: Epic %10 / Rare %30 / Common %60
        // Lv1: Epic %17 / Rare %33 / Common %50
        // Lv2: Epic %25 / Rare %33 / Common %42
        // Lv3: Epic %33 / Rare %33 / Common %33
        int cloverLv = RunManager.instance != null ? RunManager.instance.luckyCloverLevel : 0;
        float epicThresh  = cloverLv == 0 ? 10f : cloverLv == 1 ? 17f : cloverLv == 2 ? 25f : 33f;
        float rareThresh  = cloverLv == 0 ? 40f : cloverLv == 1 ? 50f : cloverLv == 2 ? 58f : 66f;

        float roll = Random.Range(0f, 100f);
        if (roll < epicThresh && epicPerks.Count > 0)       return epicPerks[Random.Range(0, epicPerks.Count)];
        else if (roll < rareThresh && rarePerks.Count > 0)  return rarePerks[Random.Range(0, rarePerks.Count)];
        else if (commonPerks.Count > 0)                     return commonPerks[Random.Range(0, commonPerks.Count)];
        return null;
    }

    private PerkRarity GetRarityFromList(GameObject perk)
    {
        if (legendaryPerks.Contains(perk)) return PerkRarity.Legendary;
        if (epicPerks.Contains(perk))      return PerkRarity.Epic;
        if (rarePerks.Contains(perk))      return PerkRarity.Rare;
        // Secret perkler normal havuzda olmaz ama güvenlik için kontrol
        BasePerk bp = perk.GetComponent<BasePerk>();
        if (bp != null && bp.rarity == PerkRarity.Secret) return PerkRarity.Secret;
        return PerkRarity.Common;
    }

    private Color GetRarityColor(PerkRarity rarity)
    {
        switch (rarity)
        {
            case PerkRarity.Common:    return new Color(0.8f, 0.8f, 0.8f); // Gri
            case PerkRarity.Rare:      return new Color(0.2f, 0.5f, 1f);   // Mavi
            case PerkRarity.Epic:      return new Color(0.6f, 0.2f, 1f);   // Mor
            case PerkRarity.Legendary: return new Color(1f, 0.6f, 0f);     // Turuncu/Altın
            case PerkRarity.Secret:    return new Color(1f, 0.27f, 0.27f); // Kırmızı
            default:                   return Color.white;
        }
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
            if (forcedPerk == chosenPerk) forcedPerk = null;
        }

        foreach (var perk in existingPerks)
            if (perk != null && !perk.isDisabled) perk.OnLevelStart();

        StartCoroutine(FadeOutAndShrinkRoutine());
    }

    private IEnumerator FadeInAndPopRoutine()
    {
        // Panel fade in
        if (levelUpCanvasGroup != null) levelUpCanvasGroup.alpha = 0f;
        levelUpPanel.transform.localScale = Vector3.one;

        // Hide all cards initially
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null && choiceButtons[i].gameObject.activeSelf)
                choiceButtons[i].transform.localScale = Vector3.zero;
        }

        // Fade in the panel background
        float fadeDur = 0.2f;
        float elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeDur;
            if (levelUpCanvasGroup != null) levelUpCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        if (levelUpCanvasGroup != null) levelUpCanvasGroup.alpha = 1f;

        // Spawn cards one by one with delay
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null || !choiceButtons[i].gameObject.activeSelf) continue;
            yield return StartCoroutine(CardPopIn(choiceButtons[i].transform));
            if (i < choiceButtons.Length - 1)
                yield return new WaitForSecondsRealtime(0.15f);
        }

        // Mark all cards as animation-done
        cardAnimDone = new bool[choiceButtons.Length];
        for (int i = 0; i < cardAnimDone.Length; i++) cardAnimDone[i] = true;

        // Start idle bounce on all visible cards
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null && choiceButtons[i].gameObject.activeSelf)
                StartCoroutine(CardIdleBounce(choiceButtons[i].transform));
        }
    }

    private IEnumerator CardPopIn(Transform card)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayCard();
        float dur = 0.25f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float ease = 1f - (1f - t) * (1f - t);
            float s = Mathf.Lerp(0f, 1.08f, ease);
            card.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        dur = 0.1f;
        elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float s = Mathf.Lerp(1.08f, 1f, t);
            card.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        card.localScale = Vector3.one;
    }

    private IEnumerator CardIdleBounce(Transform card)
    {
        int cardIdx = -1;
        for (int i = 0; i < choiceButtons.Length; i++)
            if (choiceButtons[i] != null && choiceButtons[i].transform == card) { cardIdx = i; break; }

        while (card != null && card.gameObject.activeSelf && levelUpPanel.activeSelf)
        {
            float target = (hoveredCardIndex == cardIdx) ? 1.1f : 1f;
            float cur = card.localScale.x;
            float s = Mathf.MoveTowards(cur, target, Time.unscaledDeltaTime * 3f);
            card.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    private void SetupCardHoverListeners()
    {
        if (choiceButtons == null) return;
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null) continue;
            EventTrigger trigger = choiceButtons[i].GetComponent<EventTrigger>();
            if (trigger == null) trigger = choiceButtons[i].gameObject.AddComponent<EventTrigger>();

            int idx = i;

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((_) => { hoveredCardIndex = idx; });
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((_) => { if (hoveredCardIndex == idx) hoveredCardIndex = -1; });
            trigger.triggers.Add(exitEntry);
        }
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

        Time.timeScale = 1f;

        // Gene Splice gibi upgrade sekanslarının görünmesi için kısa bekleme
        yield return new WaitForSeconds(1f);

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