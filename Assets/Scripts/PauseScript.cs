using UnityEngine;
using UnityEngine.SceneManagement; // Sahne yönetimi için gerekli
using TMPro;
public class PauseManager : MonoBehaviour
{
    // Durdurma menüsü panelini (Canvas) buraya sürükleyeceğiz
    public GameObject pauseMenuUI;
    public GameObject deathMenuUI; // Müfettiş (Inspector) panelinden ölme ekranını buraya sürükle
    // Oyunun durup durmadığını takip eden değişken
    public static bool isPaused = false;

    [Header("Stats UI")]
    public TMP_Text pauseStatsText; // Duraklatma ekranındaki Text (eski, opsiyonel)
    public TMP_Text deathStatsText; // Ölme ekranındaki Text (eski, opsiyonel)
    public StatsPanelUI statsPanelUI;       // Pause menüsündeki stats paneli
    public StatsPanelUI deathStatsPanelUI;  // Ölüm ekranındaki stats paneli

    private bool deathStatsRefreshed = false;

    void Update()
    {
        // EĞER ÖLME EKRANI AÇIKSA, ESC TUŞUNU HİÇ DİNLEME!
        if (deathMenuUI != null && deathMenuUI.activeSelf)
        {
            if (!deathStatsRefreshed)
            {
                if (deathStatsPanelUI != null) deathStatsPanelUI.Refresh();
                else if (deathStatsText != null) deathStatsText.text = RunManager.instance.GetStatsSummary();
                deathStatsRefreshed = true;
            }
            return;
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
        pauseMenuUI.SetActive(true);
        if (statsPanelUI != null) statsPanelUI.Refresh();
        else if (pauseStatsText != null) pauseStatsText.text = RunManager.instance.GetStatsSummary();
        Time.timeScale = 0f;
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
