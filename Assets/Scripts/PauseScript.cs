using UnityEngine;
using UnityEngine.SceneManagement; // Sahne yönetimi için gerekli

public class PauseManager : MonoBehaviour
{
    // Durdurma menüsü panelini (Canvas) buraya sürükleyeceğiz
    public GameObject pauseMenuUI;
    public GameObject deathMenuUI; // Müfettiş (Inspector) panelinden ölme ekranını buraya sürükle
    // Oyunun durup durmadığını takip eden değişken
    public static bool isPaused = false;

    void Update()
    {
        // EĞER ÖLME EKRANI AÇIKSA, ESC TUŞUNU HİÇ DİNLEME!
        if (deathMenuUI != null && deathMenuUI.activeSelf)
        {
            return; // Fonksiyondan çık, aşağıdaki ESC kodlarına hiç bakma
        }

        // Normal ESC basma kodun...
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    // Oyuna devam etme fonksiyonu
    public void Resume()
    {
        pauseMenuUI.SetActive(false); // Menüyü kapat
        Time.timeScale = 1f;          // Zamanı normal hızına döndür
        isPaused = false;
    }

    // Oyunu durdurma fonksiyonu
    public void Pause()
    {
        pauseMenuUI.SetActive(true);  // Menüyü aç
        Time.timeScale = 0f;          // Zamanı tamamen durdur (Fizik ve hareketler durur)
        isPaused = true;
    }

    // Bölümü baştan başlatma fonksiyonu
    public void Restart()
    {
        Time.timeScale = 1f; // Önemli: Sahne yüklenmeden zamanı açmalıyız!
        // Mevcut sahnenin indeksini alıp tekrar yüklüyoruz
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Ana menüye veya başka bir sahneye gitme (Index ile çalışır)
    public void LoadSceneByIndex(int sceneIndex)
    {
        Time.timeScale = 1f; // Zamanı açmayı unutma!
        SceneManager.LoadScene(sceneIndex);
    }

    // Oyundan tamamen çıkma (Sadece gerçek oyunda çalışır, Unity Editor'da çalışmaz)
    public void QuitGame()
    {
        Debug.Log("Oyundan çıkılıyor...");

        // Bu kısım sadece Unity Editor içindeyken çalışır
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Bu kısım ise oyun gerçekten yüklendiğinde (Build alındığında) çalışır
        Application.Quit();
#endif
    }
}
