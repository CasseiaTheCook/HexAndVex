using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [Header("Mixer Referansı")]
    public AudioMixer mainMixer;

    [Header("Slider Referansları")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        // Slider değerlerini mevcut Mixer değerlerine göre eşitle (veya PlayerPrefs'ten çek)
        if(masterSlider != null) masterSlider.value = PlayerPrefs.GetFloat("MasterVol", 0.75f);
        if(musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        if(sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("SfxVol", 0.75f);
        
        // İlk açılışta sesleri uygula
        SetMasterVolume(masterSlider.value);
        SetMusicVolume(musicSlider.value);
        SetSfxVolume(sfxSlider.value);
    }

    public void SetMasterVolume(float value)
    {
        // Slider 0-1 arası gelir, Mixer -80 ile 20 dB arası çalışır. 
        // Logaritmik hesaplama en doğrusudur:
        mainMixer.SetFloat("MasterVol", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MasterVol", value);
    }

    public void SetMusicVolume(float value)
    {
        mainMixer.SetFloat("MusicVol", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSfxVolume(float value)
    {
        mainMixer.SetFloat("SfxVol", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("SfxVol", value);
    }
}
