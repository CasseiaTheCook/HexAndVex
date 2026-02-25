using UnityEngine;
using System.Collections;

public abstract class BasePerk : MonoBehaviour
{
    public string perkName;
    [TextArea] public string description;

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

    // Görsel geri bildirim: Perk çalıştığında ekranda zıplar
    public void TriggerVisualPop()
    {
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