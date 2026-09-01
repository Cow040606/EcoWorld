using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion;

public class ESC : MonoBehaviour 
{
    public static ESC instance;
    public GameObject khungESC; 
    public bool isESC_Open; 
    
    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (khungESC != null) khungESC.SetActive(false);
    }

    public void BatTatESC()
    {
        isESC_Open = !isESC_Open;
        
        if (khungESC != null) 
        {
            khungESC.SetActive(isESC_Open);
        }

        if (isESC_Open && Player_Controller.localPlayer != null)
        {
            Player_Controller.localPlayer.LuuGameHienTai();
        }
    }

    // --- 1. NÚT LƯU & THOÁT VỀ MENU CHÍNH (SCENE 0) ---
    public async void BamNut_LuuVaThoatGame()
    {
        await ThucHienThoat(traVeMenu: true);
    }

    public async void BamNut_LuuVaThoatVeMenu()
    {
        await ThucHienThoat(traVeMenu: true);
    }

    // --- 2. NÚT LƯU & THOÁT HẮN KHỎI GAME (QUIT APPLICATION) ---
    public async void BamNut_LuuVaThoatKhoiGame()
    {
        await ThucHienThoat(traVeMenu: false);
    }

    private async Task ThucHienThoat(bool traVeMenu)
    {
        // Debug.Log("[ESC]: Bắt đầu tiến trình Lưu & Thoát...");

        // 1. Lưu dữ liệu người chơi
        if (Player_Controller.localPlayer != null)
        {
            Player_Controller.localPlayer.LuuGameHienTai();
        }

        // 2. Tắt Photon Runner một cách an toàn
        if (NetworkRunner.Instances != null)
        {
            List<NetworkRunner> activeRunners = new List<NetworkRunner>(NetworkRunner.Instances);
            foreach (var runner in activeRunners)
            {
                if (runner != null && runner.IsRunning)
                {
                    await runner.Shutdown();
                }
            }
        }

        // 3. Thực hiện chuyển Scene hoặc Đóng game
        if (traVeMenu)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}