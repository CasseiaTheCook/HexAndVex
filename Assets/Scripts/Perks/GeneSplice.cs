using UnityEngine;
using System.Collections.Generic;

public class GeneSplicePerk : BasePerk
{
    // İlk alındığında çalışır
    public override void OnAcquire()
    {
        ApplyBlessing();
    }

    // YENİ: Oyuncu bu kartı 2. kez seçerse (Upgrade ederse) yine tetiklenir!
    public override void Upgrade()
    {
        base.Upgrade(); // Kendi seviyesini (currentLevel) 1 artırır
        ApplyBlessing(); // Etkiyi tekrar yapıştırır!
    }

    // İşin bütün mutfağı (Hem ilk alımda hem upgrade'de aynı mantık çalışsın diye ayırdık)
    private void ApplyBlessing()
    {
        List<BasePerk> upgradablePerks = new List<BasePerk>();

        foreach (var perk in RunManager.instance.activePerks)
        {
            // 1. KENDİSİ HARİÇ olsun.
            // 2. YENİ DÜZELTME: MAX SEVİYEYE ULAŞMAMIŞ yetenekleri filtrele. Max olanı boşuna seçmesin!
            if (perk != this && perk.currentLevel < perk.maxLevel) 
            {
                upgradablePerks.Add(perk);
            }
        }

        if (upgradablePerks.Count > 0)
        {
            int randomIndex = Random.Range(0, upgradablePerks.Count);
            BasePerk selectedPerk = upgradablePerks[randomIndex];
            
            // YENİ SİSTEMİMİZ: O yeteneğin kendi özel Upgrade'ini çalıştır!
            selectedPerk.Upgrade();
            
            Debug.Log($"✨ Kadim Kutsama: {selectedPerk.perkName} güçlendirildi! (Yeni Seviye: {selectedPerk.currentLevel})");
            TriggerVisualPop();
        }
        else
        {
            // B PLANI: Gelişecek yetenek yoksa veya hepsi MAX olduysa YENİ kart ver.
            Debug.Log("⚠️ Güçlendirilecek yetenek yok, Kadim Kutsama yeni bir yetenek hediye ediyor!");
            
            GameObject randomPerk = LevelUpManager.instance.GetRandomPerkByRarity(false);
            if (randomPerk != null)
            {
                // Bizim yeni AddPerk fonksiyonu zaten çok akıllı.
                // Eğer verdiği random kart da adamda varsa onu yükseltir, yoksa yeni ekler.
                RunManager.instance.AddPerk(randomPerk);
            }
            TriggerVisualPop();
        }
    }
}