
using Fusion;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using Fusion.Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Menu_Fusion : MonoBehaviour
{
    private NetworkRunner runner;

    [Header("UI References")]
    public GameObject nutTaoTenPhong;    // Kéo cái Nút "Tạo Tên Phòng" vào đây
    public TMP_InputField inputSessionName; // Kéo ô InputField vào đây
    public GameObject coopPlayer;
    public GameObject menu;

    [Header("UI Hover Settings")]
    public Color hoverColor = Color.lightYellow;
    private Color normalColor = Color.black;

    void Start()
    {
        // Ban đầu: Hiện nút, ẩn ô nhập liệu
        if (nutTaoTenPhong != null) nutTaoTenPhong.SetActive(true);
        if (inputSessionName != null) inputSessionName.gameObject.SetActive(false);
    }

    // Hàm này dùng để gọi khi bạn nhấn vào nút "Tạo Tên Phòng"
    public void BamNut_HienOInput()
    {
        if (nutTaoTenPhong != null) nutTaoTenPhong.SetActive(false); // Ẩn nút đi

        if (inputSessionName != null)
        {
            inputSessionName.gameObject.SetActive(true); // Hiện ô nhập liệu
            inputSessionName.ActivateInputField();      // Tự động cho phép gõ chữ luôn
        }
    }

    async void KetNoi(GameMode cheDo)
    {
        if (runner != null)
        {
            await runner.Shutdown();
            if (runner != null) Destroy(runner.gameObject);
        }

        if (string.IsNullOrEmpty(inputSessionName.text))
        {
            Debug.LogError("<color=red>Bà mụ:</color> Bò ơi, gõ cái tên phòng vào đã!");
            return;
        }

        var idRieng = new AuthenticationValues(System.Guid.NewGuid().ToString());
        GameObject runnerObject = new GameObject("TienTrinhFusion");
        runner = runnerObject.AddComponent<NetworkRunner>();

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = cheDo,
            SessionName = inputSessionName.text.Trim(),
            AuthValues = idRieng,
            Scene = SceneRef.FromIndex(1),
            SceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    // --- LOGIC CÁC NÚT BẤM ---
    public void BamNut_ChoiDon() { KetNoi(GameMode.Single); }
    public void BamNut_TaoPhong() { KetNoi(GameMode.Host); }
    public void BamNut_VaoPhong() { KetNoi(GameMode.Client); }

    public void BamNut_Coop()
    {
        coopPlayer.SetActive(true);
        menu.SetActive(false);
    }

    public void BamNut_Menu()
    {
        coopPlayer.SetActive(false);
        menu.SetActive(true);
        // Reset lại trạng thái: Hiện nút, ẩn ô nhập
        if (nutTaoTenPhong != null) nutTaoTenPhong.SetActive(true);
        if (inputSessionName != null) inputSessionName.gameObject.SetActive(false);
    }

    // --- LOGIC HIỆU ỨNG DI CHUỘT (HOVER) ---
    public void DiChuotVao(GameObject buttonObj)
    {
        Image img = buttonObj.GetComponent<Image>();
        if (img != null)
        {
            img.color = hoverColor;
        }
        buttonObj.transform.localScale = Vector3.one * 1.1f;
    }

    public void DiChuotRa(GameObject buttonObj)
    {
        Image img = buttonObj.GetComponent<Image>();
        if (img != null)
        {
            // Trả về màu trắng để giữ nguyên màu gốc của Sprite vẽ tay
            img.color = Color.black;
        }
        buttonObj.transform.localScale = Vector3.one;
    }
}

// using Fusion;
// using UnityEngine;
// using System.Threading.Tasks;
// using TMPro;
// using Fusion.Photon.Realtime; // Bắt buộc phải có dòng này!

// public class Menu_Fusion : MonoBehaviour
// {
//     private NetworkRunner runner;
//     public TMP_InputField inputSessionName;
//     public GameObject coopPlayer;
//     public GameObject menu;

//     async void KetNoi(GameMode cheDo)
//     {
//         // 1. ĐUỔI VIỆC ANH CŨ ĐÚNG QUY TRÌNH (QUAN TRỌNG NHẤT)
//         if (runner != null)
//         {
//             // Phải Shutdown để Server xóa tên mình ra khỏi phòng cũ
//             await runner.Shutdown(); 
//             Destroy(runner.gameObject);
//         }

//         // Kiểm tra xem Bò có quên nhập tên không
//         if (string.IsNullOrEmpty(inputSessionName.text))
//         {
//             Debug.LogError("<color=red>Bà mụ:</color> Bò ơi, gõ cái tên phòng vào đã!");
//             return;
//         }

//         // 2. CẤP "CHỨNG MINH NHÂN DÂN" GIẢ (GUID)
//         // Dòng này giúp Server phân biệt 2 cửa sổ trên cùng 1 máy Bò
//         var idRieng = new AuthenticationValues(System.Guid.NewGuid().ToString());

//         // 3. THUÊ ANH MỚI
//         GameObject runnerObject = new GameObject("TienTrinhFusion");
//         runner = runnerObject.AddComponent<NetworkRunner>();

//         Debug.Log("<color=green>Bà mụ:</color> Đang phi vào phòng: " + inputSessionName.text.Trim());

//         // 4. RA LỆNH KẾT NỐI
//         await runner.StartGame(new StartGameArgs()
//         {
//             GameMode = cheDo,
//             SessionName = inputSessionName.text.Trim(),
//             AuthValues = idRieng, // <--- CHIÊU CUỐI FIX LỖI 104
//             Scene = SceneRef.FromIndex(1),
//             SceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>()
//         });
//     }

//     public void BamNut_ChoiDon() { KetNoi(GameMode.Single); }
//     public void BamNut_TaoPhong() { KetNoi(GameMode.Host); }
//     public void BamNut_Coop() { coopPlayer.SetActive(true); 
//         menu.SetActive(false); } 
//         public void BamNut_Menu() { coopPlayer.SetActive(false); 
//         menu.SetActive(true); } 
//     public void BamNut_VaoPhong() { KetNoi(GameMode.Client); }
//     public void BamNut_VaoNhanh() { KetNoi(GameMode.AutoHostOrClient); }
// }

