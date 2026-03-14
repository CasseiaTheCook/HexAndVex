using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DiceUIController : MonoBehaviour
{
    [HideInInspector] public GameObject dieUIPrefab;
    [HideInInspector] public Transform diceUIContainer;
    [HideInInspector] public TMP_Text totalDamageText;
    [HideInInspector] public Sprite[] diceSprites;
    [HideInInspector] public GameObject criticalText;
    [HideInInspector] public GameObject comboTextObj;
    [HideInInspector] public Image dicePanelBackground;

    [HideInInspector] public bool skipDiceAnim = false;
    [HideInInspector] public bool skipDiceVisuals = false;

    private bool isDiceAnimPlaying = false;
    public bool IsDiceAnimPlaying => isDiceAnimPlaying;

    private List<GameObject> spawnedDiceUI = new List<GameObject>();
    public List<GameObject> SpawnedDiceUI => spawnedDiceUI;

    private int comboCount = 0;
    private Coroutine comboFadeCoroutine;
    private Coroutine textPopCoroutine;

    public void BeginDiceAnim()
    {
        isDiceAnimPlaying = true;
        skipDiceAnim = false;
    }

    public void EndDiceAnim()
    {
        isDiceAnimPlaying = false;
        skipDiceAnim = false;
    }

    public void CheckFastModeSkip()
    {
        if (isDiceAnimPlaying && RunManager.instance != null && RunManager.instance.fastMode)
            skipDiceAnim = true;
    }

    public IEnumerator ShowDiceSequence(List<int> rolls)
    {
        if (skipDiceVisuals)
        {
            foreach (var die in spawnedDiceUI) Destroy(die);
            spawnedDiceUI.Clear();
            if (dicePanelBackground != null) dicePanelBackground.gameObject.SetActive(false);
            if (totalDamageText != null) totalDamageText.gameObject.SetActive(false);
            yield break;
        }

        foreach (var die in spawnedDiceUI) Destroy(die);
        spawnedDiceUI.Clear();
        if (totalDamageText != null)
        {
            totalDamageText.gameObject.SetActive(true);
            totalDamageText.text = "0";
            Color tc = totalDamageText.color; tc.a = 0f; totalDamageText.color = tc;
        }

        List<CanvasGroup> dieGroups = new List<CanvasGroup>();
        List<Animator> dieAnimators = new List<Animator>();
        List<TMP_Text> dieTexts = new List<TMP_Text>();
        List<Image> dieImages = new List<Image>();
        for (int i = 0; i < rolls.Count; i++)
        {
            GameObject newDie = Instantiate(dieUIPrefab, diceUIContainer);
            spawnedDiceUI.Add(newDie);
            CanvasGroup cg = newDie.GetComponent<CanvasGroup>();
            if (cg == null) cg = newDie.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            TMP_Text txt = newDie.GetComponentInChildren<TMP_Text>();
            if (txt != null) { Color tc = txt.color; tc.a = 0f; txt.color = tc; txt.text = ""; }
            dieGroups.Add(cg);
            dieAnimators.Add(newDie.GetComponent<Animator>());
            dieTexts.Add(txt);
            dieImages.Add(newDie.GetComponent<Image>());
        }

        if (dicePanelBackground != null)
        {
            dicePanelBackground.gameObject.SetActive(true);
            float e = 0f; Color c = dicePanelBackground.color; c.a = 0f; dicePanelBackground.color = c;
            while (e < 0.2f && !skipDiceAnim)
            {
                e += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 0.9f, e / 0.2f);
                dicePanelBackground.color = c;
                yield return null;
            }
            c.a = 0.9f; dicePanelBackground.color = c;
        }

        if (AudioManager.instance != null) AudioManager.instance.PlayDiceRoll();
        float rollTime = 0.4f; float rollElapsed = 0f;
        while (rollElapsed < rollTime && !skipDiceAnim)
        {
            rollElapsed += Time.deltaTime;
            float a = Mathf.Clamp01(rollElapsed / (rollTime * 0.5f));
            foreach (var cg in dieGroups) cg.alpha = a;
            yield return null;
        }
        foreach (var cg in dieGroups) cg.alpha = 1f;

        if (AudioManager.instance != null) AudioManager.instance.PlayDiceHit();
        for (int i = 0; i < rolls.Count; i++)
        {
            if (dieAnimators[i] != null) dieAnimators[i].enabled = false;
            int spriteIdx = Mathf.Clamp(rolls[i] - 1, 0, diceSprites.Length - 1);
            dieImages[i].sprite = diceSprites[spriteIdx];
            dieTexts[i].text = rolls[i].ToString();
            if (!skipDiceAnim) StartCoroutine(TextFadeInAndPopDelayed(dieTexts[i], i * 0.08f));
            else { Color tc = dieTexts[i].color; tc.a = 1f; dieTexts[i].color = tc; dieTexts[i].transform.localScale = Vector3.one; }
        }
        if (!skipDiceAnim) yield return new WaitForSeconds(0.2f + rolls.Count * 0.08f);
    }

    public void AnimateSpecificDie(int index, int newValue)
    {
        if (index >= 0 && index < spawnedDiceUI.Count)
        {
            TMP_Text dieText = spawnedDiceUI[index].GetComponentInChildren<TMP_Text>();
            Image dieImage = spawnedDiceUI[index].GetComponent<Image>();
            if (dieText != null) dieText.text = newValue.ToString();
            if (dieImage != null && diceSprites != null)
            {
                int spriteIndex = Mathf.Clamp(newValue - 1, 0, diceSprites.Length - 1);
                dieImage.sprite = diceSprites[spriteIndex];
            }
            StartCoroutine(DiePopAnimation(spawnedDiceUI[index].transform));
        }
    }

    private IEnumerator DiePopAnimation(Transform t)
    {
        Vector3 startScale = Vector3.one * 1.6f;
        float dur = 0.2f; float elapsed = 0f;
        while (elapsed < dur)
        {
            t.localScale = Vector3.Lerp(startScale, Vector3.one, elapsed / dur);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    public void UpdateTotalDamageDisplay(int val)
    {
        if (totalDamageText == null) return;
        totalDamageText.gameObject.SetActive(true);
        totalDamageText.text = val.ToString();
        StartCoroutine(TotalTextFadeIn(totalDamageText));
        if (textPopCoroutine != null) StopCoroutine(textPopCoroutine);
        textPopCoroutine = StartCoroutine(TextPopAnimation(totalDamageText));
    }

    private IEnumerator TotalTextFadeIn(TMP_Text txt)
    {
        if (txt == null) yield break;
        float duration = 0.2f; float elapsed = 0f;
        Color startColor = txt.color; startColor.a = 0f; txt.color = startColor;
        while (elapsed < duration)
        {
            Color c = txt.color; c.a = Mathf.Lerp(0f, 1f, elapsed / duration); txt.color = c;
            elapsed += Time.deltaTime; yield return null;
        }
        Color fc = txt.color; fc.a = 1f; txt.color = fc;
    }

    public void HideDiceResults()
    {
        StartCoroutine(FadeOutAndHideDice());
    }

    private IEnumerator FadeOutAndHideDice()
    {
        float duration = 0.25f; float elapsed = 0f;
        List<CanvasGroup> dieGroups = new List<CanvasGroup>();
        foreach (var die in spawnedDiceUI)
        {
            if (die == null) continue;
            CanvasGroup cg = die.GetComponent<CanvasGroup>();
            if (cg == null) cg = die.AddComponent<CanvasGroup>();
            dieGroups.Add(cg);
        }
        float totalStartA = totalDamageText != null ? totalDamageText.color.a : 0f;
        float critStartA = 0f;
        Image critImage = criticalText != null ? criticalText.GetComponent<Image>() : null;
        TMP_Text critTMP = criticalText != null ? criticalText.GetComponentInChildren<TMP_Text>() : null;
        if (critImage != null) critStartA = critImage.color.a;
        else if (critTMP != null) critStartA = critTMP.color.a;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            foreach (var cg in dieGroups) { if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, t); }
            if (totalDamageText != null) { Color c = totalDamageText.color; c.a = Mathf.Lerp(totalStartA, 0f, t); totalDamageText.color = c; }
            if (critImage != null) { Color c = critImage.color; c.a = Mathf.Lerp(critStartA, 0f, t); critImage.color = c; }
            if (critTMP != null) { Color c = critTMP.color; c.a = Mathf.Lerp(critStartA, 0f, t); critTMP.color = c; }
            elapsed += Time.deltaTime; yield return null;
        }

        foreach (var die in spawnedDiceUI) { if (die != null) Destroy(die); }
        spawnedDiceUI.Clear();
        if (totalDamageText != null) totalDamageText.gameObject.SetActive(false);
        if (criticalText != null) criticalText.gameObject.SetActive(false);
        if (dicePanelBackground != null) StartCoroutine(FadeDicePanel());
    }

    private IEnumerator FadeDicePanel()
    {
        if (dicePanelBackground == null) yield break;
        float e = 0f; Color c = dicePanelBackground.color;
        float startA = 0.9f;
        while (e < 0.2f)
        {
            e += Time.deltaTime;
            c.a = Mathf.Lerp(startA, 0f, e / 0.2f);
            dicePanelBackground.color = c;
            yield return null;
        }
        c.a = 0f; dicePanelBackground.color = c;
        dicePanelBackground.gameObject.SetActive(false);
    }

    public IEnumerator CriticalTextPopAnimation()
    {
        if (criticalText == null) yield break;
        criticalText.gameObject.SetActive(true);
        Transform t = criticalText.transform;
        Image critImg = criticalText.GetComponent<Image>();
        TMP_Text critTMP = criticalText.GetComponentInChildren<TMP_Text>();
        if (critImg != null) { Color c = critImg.color; c.a = 0f; critImg.color = c; }
        if (critTMP != null) { Color c = critTMP.color; c.a = 0f; critTMP.color = c; }

        Vector3 startScale = new Vector3(0.2f, 0.2f, 0.2f);
        Vector3 overshootScale = new Vector3(0.6f, 0.6f, 0.6f);
        Vector3 endScale = new Vector3(0.5f, 0.5f, 0.5f);
        float elapsed = 0f; float popDuration = 0.1f;
        while (elapsed < popDuration)
        {
            float prog = elapsed / popDuration;
            t.localScale = Vector3.Lerp(startScale, overshootScale, prog);
            if (critImg != null) { Color c = critImg.color; c.a = Mathf.Lerp(0f, 1f, prog); critImg.color = c; }
            if (critTMP != null) { Color c = critTMP.color; c.a = Mathf.Lerp(0f, 1f, prog); critTMP.color = c; }
            elapsed += Time.deltaTime; yield return null;
        }
        elapsed = 0f; float settleDuration = 0.1f;
        while (elapsed < settleDuration)
        {
            t.localScale = Vector3.Lerp(overshootScale, endScale, elapsed / settleDuration);
            elapsed += Time.deltaTime; yield return null;
        }
        t.localScale = endScale;
        if (critImg != null) { Color c = critImg.color; c.a = 1f; critImg.color = c; }
        if (critTMP != null) { Color c = critTMP.color; c.a = 1f; critTMP.color = c; }
    }

    public IEnumerator SkippableWait(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (skipDiceAnim) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator TextFadeInAndPopDelayed(TMP_Text txt, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return StartCoroutine(TextFadeInAndPop(txt));
    }

    private IEnumerator TextFadeInAndPop(TMP_Text txt)
    {
        if (txt == null) yield break;
        if (AudioManager.instance != null) AudioManager.instance.PlayTextEffect();
        CameraController.ShakeLight();
        float duration = 0.15f; float elapsed = 0f;
        Transform t = txt.transform;
        Vector3 startScale = new Vector3(2f, 2f, 2f);
        t.localScale = startScale;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float a = Mathf.Lerp(0f, 1f, progress);
            Color c = txt.color; c.a = a; txt.color = c;
            t.localScale = Vector3.Lerp(startScale, Vector3.one, 1f - (1f - progress) * (1f - progress));
            elapsed += Time.deltaTime; yield return null;
        }
        Color fc = txt.color; fc.a = 1f; txt.color = fc;
        t.localScale = Vector3.one;
    }

    private IEnumerator DiceFadeIn(Image img, TMP_Text txt)
    {
        float duration = 0.2f; float elapsed = 0f;
        while (elapsed < duration)
        {
            float a = Mathf.Lerp(0f, 1f, elapsed / duration);
            if (img != null) { Color c = img.color; c.a = a; img.color = c; }
            if (txt != null) { Color c = txt.color; c.a = a; txt.color = c; }
            elapsed += Time.deltaTime; yield return null;
        }
        if (img != null) { Color c = img.color; c.a = 1f; img.color = c; }
        if (txt != null) { Color c = txt.color; c.a = 1f; txt.color = c; }
    }

    private IEnumerator TextPopAnimation(TMP_Text textElement)
    {
        if (textElement == null) yield break;
        if (AudioManager.instance != null) AudioManager.instance.PlayTextEffect();
        CameraController.ShakeLight();
        Transform t = textElement.transform;
        Vector3 startScale = new Vector3(3f, 3f, 3f); Vector3 endScale = Vector3.one;
        float duration = 0.15f; float elapsed = 0f;
        while (elapsed < duration)
        {
            float tParam = elapsed / duration;
            tParam = 1f - (1f - tParam) * (1f - tParam);
            t.localScale = Vector3.Lerp(startScale, endScale, tParam);
            elapsed += Time.deltaTime; yield return null;
        }
        t.localScale = endScale;
    }

    // Combo sistemi
    public void RegisterComboHit()
    {
        comboCount++;
        if (comboCount >= 2) ShowCombo(comboCount);
    }

    public void ResetCombo()
    {
        comboCount = 0;
        if (comboTextObj != null) comboTextObj.SetActive(false);
        if (comboFadeCoroutine != null) { StopCoroutine(comboFadeCoroutine); comboFadeCoroutine = null; }
    }

    private void ShowCombo(int count)
    {
        if (comboTextObj == null) return;
        var tmp = comboTextObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = $"x{count} COMBO!";
        comboTextObj.SetActive(true);
        if (comboFadeCoroutine != null) StopCoroutine(comboFadeCoroutine);
        comboFadeCoroutine = StartCoroutine(ComboPopAndFade());
    }

    private IEnumerator ComboPopAndFade()
    {
        Transform t = comboTextObj.transform;
        Vector3 start = new Vector3(0.3f, 0.3f, 0.3f);
        Vector3 over = new Vector3(0.65f, 0.65f, 0.65f);
        Vector3 end = new Vector3(0.5f, 0.5f, 0.5f);
        float elapsed = 0f;
        while (elapsed < 0.1f) { t.localScale = Vector3.Lerp(start, over, elapsed / 0.1f); elapsed += Time.unscaledDeltaTime; yield return null; }
        elapsed = 0f;
        while (elapsed < 0.1f) { t.localScale = Vector3.Lerp(over, end, elapsed / 0.1f); elapsed += Time.unscaledDeltaTime; yield return null; }
        t.localScale = end;

        yield return new WaitForSecondsRealtime(1.2f);
        var tmp = comboTextObj.GetComponentInChildren<TMP_Text>();
        if (tmp == null) { comboTextObj.SetActive(false); yield break; }
        Color c = tmp.color; float fadeDur = 0.3f; elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDur);
            tmp.color = c;
            yield return null;
        }
        c.a = 1f; tmp.color = c;
        comboTextObj.SetActive(false);
    }
}
