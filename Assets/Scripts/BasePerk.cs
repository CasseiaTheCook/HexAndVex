using UnityEngine;
using System.Collections;

public abstract class BasePerk : MonoBehaviour
{
    [Header("Seviye Sistemi")]
    public int currentLevel = 1;
    public int maxLevel = 3; // Unity'den her yetenek için bunu değiştirebilirsin (Örn: SwiftAction max 2 olsun gibi)
    public string perkName;
    [TextArea] public string description;
    public int priority = 0;
    public bool isRerollPerk = false; // true ise zar değişiminde "!" re-roll animasyonu oynar

    // 1. Perk satın alındığında / seçildiğinde 1 kez çalışır
    public virtual void OnAcquire() { }

    // 2. Her saldırı yapıldığında, hasar hesaplanırken çalışır
    public virtual void ModifyCombat(CombatPayload payload) { }

    // 3. Tur geçildiğinde (Skip) çalışır
    public virtual void OnSkip() { }

    // Her yeni levele/odaya geçildiğinde çalışır
    public virtual void OnLevelStart() { }

    // Düşman öldüğünde çalışır
    public virtual void OnEnemyKilled(EnemyAI enemy) { }

    // ======================================================
    // İŞTE YENİ EKLENEN KISIM BURASI KANKA:
    // Ancient Blessing bu komutu çağıracak. Diğer perkler de bu komutu alınca ne yapacaklarını bilecek.
    public virtual void UpgradePerk() { }

    public virtual void Upgrade()
    {
        currentLevel++;
        Debug.Log($"{perkName} seviye atladı! Yeni Seviye: {currentLevel}");
        
        // İleride yeteneklerine özel Upgrade işlemleri yapmak istersen, 
        // SwiftActionPerk gibi alt kodlarda "public override void Upgrade()" diyerek ezeceksin.
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