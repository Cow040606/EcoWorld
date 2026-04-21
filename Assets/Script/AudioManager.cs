using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion; // Bắt buộc phải có để gọi lệnh ngắt kết nối mạng

public class GameManager : MonoBehaviour
{
    [Header("=== CÀI ĐẶT ÂM THANH ===")]
    public AudioMixer mainMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("=== CÀI ĐẶT CHUYỂN SCENE ===")]
    [Tooltip("Gõ chính xác tên Scene Menu của Bò vào đây")]
    public string tenSceneMenu = "Menu";

    private void Start()
    {
        // ---------------------------------------------------
        // KHỞI TẠO ÂM THANH: Tải lại mức âm lượng đã lưu từ lần chơi trước
        // ---------------------------------------------------
        float savedMaster = PlayerPrefs.GetFloat("MasterVol", 0.75f);
        float savedMusic = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVol", 0.75f);

        // Kéo các thanh trượt trên màn hình về đúng vị trí đã lưu
        if (masterSlider != null) masterSlider.value = savedMaster;
        if (musicSlider != null) musicSlider.value = savedMusic;
        if (sfxSlider != null) sfxSlider.value = savedSFX;

        // Áp dụng âm lượng vào hệ thống ngay lập tức
        SetMasterVolume(savedMaster);
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);
    }

    // =======================================================
    // KHU VỰC 1: CÁC HÀM XỬ LÝ ÂM THANH (GẮN VÀO SLIDERS)
    // =======================================================
    public void SetMasterVolume(float sliderValue)
    {
        mainMixer.SetFloat("MasterVol", Mathf.Log10(sliderValue) * 20);
        PlayerPrefs.SetFloat("MasterVol", sliderValue); // Lưu lại
    }

    public void SetMusicVolume(float sliderValue)
    {
        mainMixer.SetFloat("MusicVol", Mathf.Log10(sliderValue) * 20);
        PlayerPrefs.SetFloat("MusicVol", sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        mainMixer.SetFloat("SFXVol", Mathf.Log10(sliderValue) * 20);
        PlayerPrefs.SetFloat("SFXVol", sliderValue);
    }

    // =======================================================
    // KHU VỰC 2: CÁC HÀM XỬ LÝ THOÁT GAME (GẮN VÀO BUTTONS)
    // =======================================================
    
    // Nút "Thoát Phòng & Về Menu"
    public void BamNutThoatRaMenu()
    {
        // 1. Tìm hệ thống mạng đang chạy ngầm
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        
        // 2. Nếu đang kết nối mạng thì ngắt kết nối
        if (runner != null)
        {
            runner.Shutdown();
        }

        // 3. Chuyển cảnh về Menu
        SceneManager.LoadScene(tenSceneMenu);
    }

    // Nút "Thoát Game (Quit)"
    public void BamNutThoatHanGame()
    {
        Application.Quit();
    }
}