using UnityEngine;
using System.Collections;

public abstract class BasePerk : MonoBehaviour
{
    public string perkName;
    [TextArea] public string description;
    public int priority = 0;
    public int level = 1; // [YENİ] Perk seviyesi

    public virtual void OnAcquire() { }
    public virtual void ModifyCombat(CombatPayload payload) { }
    public virtual void OnSkip() { }
    public virtual void OnLevelStart() { }
    public virtual void OnEnemyKilled(EnemyAI enemy) { }

    // [YENİ] Ancient Blessing tarafından çağrılacak yükseltme metodu
    public virtual void Upgrade() 
    { 
        level++; 
        Debug.Log(perkName + " yükseltildi! Yeni Seviye: " + level);
    }

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
            tParam = 1f - (1f - tParam) * (1f - tParam);
            t.localScale = Vector3.Lerp(startScale, endScale, tParam);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = endScale;
    }
}
