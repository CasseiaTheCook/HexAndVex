using UnityEngine;
using System.Collections;

public enum PerkRarity { Common, Rare, Epic, Legendary, Secret }

public abstract class BasePerk : MonoBehaviour
{
    [Header("Seviye Sistemi")]
    public int currentLevel = 1;
    public int maxLevel = 3;
    public string perkName;
    [TextArea] public string description;
    public Sprite icon;
    public int priority = 0;
    public bool isRerollPerk = false;

    [Header("Rarity")]
    public PerkRarity rarity = PerkRarity.Common;

    [Header("Durum")]
    public bool isDisabled = false;

    public void SetDisabled(bool disabled)
    {
        isDisabled = disabled;
    }

    // Perk havuzdan çekilirken gösterilebilir mi? (GeneSplice gibi koşullu perkler override eder)
    public virtual bool CanBeOffered() { return true; }

    // 1. Perk satın alındığında / seçildiğinde 1 kez çalışır
    public virtual void OnAcquire() { }

    // 2. Her saldırı yapıldığında, hasar hesaplanırken çalışır
    public virtual void ModifyCombat(CombatPayload payload) { if (isDisabled) return; }

    // 3. Tur geçildiğinde (Skip) çalışır
    public virtual void OnSkip() { if (isDisabled) return; }

    // Her yeni levele/odaya geçildiğinde çalışır
    public virtual void OnLevelStart() { if (isDisabled) return; }

    // Düşman öldüğünde çalışır
    public virtual void OnEnemyKilled(EnemyAI enemy) { if (isDisabled) return; }

    // ======================================================
    // İŞTE YENİ EKLENEN KISIM BURASI KANKA:
    // Ancient Blessing bu komutu çağıracak. Diğer perkler de bu komutu alınca ne yapacaklarını bilecek.
    public virtual void UpgradePerk() { }

    public virtual void Upgrade()
    {
        if (currentLevel >= maxLevel) return;
        currentLevel++;
        Debug.Log($"{perkName} seviye atladı! Yeni Seviye: {currentLevel}");
    }
    // ======================================================

    // Görsel geri bildirim: Perk çalıştığında ekranda zıplar
    public void TriggerVisualPop()
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(PopAnimation());
    }

    private IEnumerator PopAnimation()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayTextEffect();
        Transform t = transform;
        Vector3 startScale = new Vector3(1.5f, 1.5f, 1.5f);
        Vector3 endScale = Vector3.one;

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float tParam = elapsed / duration;
            tParam = 1f - (1f - tParam) * (1f - tParam); // Ease-out efekti
            t.localScale = Vector3.Lerp(startScale, endScale, tParam);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = endScale;
    }
}