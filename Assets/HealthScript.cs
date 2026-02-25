using TMPro;
using UnityEngine;

/// <summary>
/// Oyuncu ve d��man i�in ortak sa�l�k sistemi.
/// Hasar, iyile�tirme ve �l�m olaylar�n� y�netir.
/// </summary>
public class HealthScript : MonoBehaviour
{
    [Header("HP Settings")]
    public int maxHP = 3;
    public int currentHP;

    public System.Action OnDeath;
    public System.Action<int> OnDamaged; // g�ncel HP

    public TMP_Text hptext;

    void Start()
    {
        if (gameObject.CompareTag("Player"))
        {
            // RunManager'daki kalıcı can verilerini çek
            if (RunManager.instance != null)
            {
                maxHP = RunManager.instance.playerMaxHealth;
                currentHP = RunManager.instance.playerCurrentHealth;
            }
        }
        else
        {
            // Düşmanlar için normal Inspector değeri geçerli olsun
            currentHP = maxHP;
        }
        updateHealth();
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
        Debug.Log($"{gameObject.name} �ld�!");
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    public void updateHealth()
    {
        hptext.text = currentHP.ToString() + "/" + maxHP;
    }
}
