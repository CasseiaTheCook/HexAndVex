using System.Collections;
using UnityEngine;

public class MysteryButtonScript : MonoBehaviour
{
    public AudioSource sesKaynagi; // Sesi çalacak olan hoparlör
    public AudioClip birinciSes;   // İlk çalacak ses dosyası
    public AudioClip ikinciSes;    // İkinci çalacak ses dosyası

    // Butona basıldığında bu fonksiyon çalışacak
    public void SesleriSiraylaCal()
    {
        StartCoroutine(SiraliCalis());
    }

    IEnumerator SiraliCalis()
    {
        // 1. Birinci sesi ayarla ve çal
        sesKaynagi.clip = birinciSes;
        sesKaynagi.Play();

        // 2. Birinci sesin uzunluğu kadar bekle
        yield return new WaitForSeconds(birinciSes.length);

        // 3. İkinci sesi ayarla ve çal
        sesKaynagi.clip = ikinciSes;
        sesKaynagi.Play();
    }
}
