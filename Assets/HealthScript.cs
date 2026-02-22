using TMPro;
using UnityEngine;

/// <summary>
/// Oyuncu ve düþman için ortak saðlýk sistemi.
/// Hasar, iyileþtirme ve ölüm olaylarýný yönetir.
/// </summary>
public class HealthScript : MonoBehaviour
{
    [Header("HP Settings")]
    public int maxHP = 3;
    public int currentHP;

    public System.Action OnDeath;
    public System.Action<int> OnDamaged; // güncel HP

    public TMP_Text hptext;

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int dmg)
    {
        currentHP -= dmg;
        OnDamaged?.Invoke(currentHP);
        updateHealth();

        if (currentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        updateHealth();
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} öldü!");
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    public void updateHealth()
    {
        hptext.text = currentHP.ToString() + "/" + maxHP;
    }
}
