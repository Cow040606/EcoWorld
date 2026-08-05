
using Fusion;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using Fusion.Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Menu_Fusion : MonoBehaviour
{
    private NetworkRunner runner;
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();

    [Header("UI References")]
    public GameObject nutTaoTenPhong;    // Kéo cái Nút "Tạo Tên Phòng" vào đây
    public TMP_InputField inputSessionName; // Kéo ô InputField vào đây
    public GameObject coopPlayer;
    public GameObject menu;

    [Header("UI Hover Settings")]
    public Color hoverColor = new Color(1f, 0.95f, 0.6f);
    private Color normalColor = Color.black;

    [Header("UI Animation Settings")]
    public bool enablePanelAnimation = true;
    [Tooltip("Thời gian chạy animation mở/đóng menu (giây)")]
    public float animDuration = 0.22f;
    [Tooltip("Tỷ lệ scale ban đầu khi bắt đầu mở pop-in")]
    public Vector3 animStartScale = new Vector3(0.85f, 0.85f, 0.85f);



    // Hàm này dùng để gọi khi bạn nhấn vào nút "Tạo Tên Phòng"
    public void BamNut_HienOInput()
    {
        if (nutTaoTenPhong != null) nutTaoTenPhong.SetActive(false); // Ẩn nút đi

        if (inputSessionName != null)
        {
            OpenPanelAnimated(inputSessionName.gameObject); // Animation mở ô nhập liệu
            inputSessionName.ActivateInputField();           // Tự động cho phép gõ chữ luôn
        }
    }

    // --- ANIMATION HELPER FUNCTIONS ---
    public void OpenPanelAnimated(GameObject targetPanel)
    {
        if (targetPanel == null) return;
        
        if (enablePanelAnimation && gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateOpenPanel(targetPanel));
        }
        else
        {
            targetPanel.SetActive(true);
        }
    }

    public void ClosePanelAnimated(GameObject targetPanel, System.Action onComplete = null)
    {
        if (targetPanel == null) return;

        if (enablePanelAnimation && gameObject.activeInHierarchy && targetPanel.activeSelf)
        {
            StartCoroutine(AnimateClosePanel(targetPanel, onComplete));
        }
        else
        {
            targetPanel.SetActive(false);
            onComplete?.Invoke();
        }
    }

    private System.Collections.IEnumerator AnimateOpenPanel(GameObject panel)
    {
        panel.SetActive(true);
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        panel.transform.localScale = animStartScale;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            // Smooth Ease Out Curve: 1 - (1-t)^3
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            cg.alpha = smoothT;
            panel.transform.localScale = Vector3.LerpUnclamped(animStartScale, Vector3.one, smoothT);
            yield return null;
        }

        cg.alpha = 1f;
        panel.transform.localScale = Vector3.one;
    }

    private System.Collections.IEnumerator AnimateClosePanel(GameObject panel, System.Action onComplete)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        Vector3 currentScale = panel.transform.localScale;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float smoothT = t * t; // Ease in

            cg.alpha = 1f - smoothT;
            panel.transform.localScale = Vector3.LerpUnclamped(currentScale, animStartScale, smoothT);
            yield return null;
        }

        cg.alpha = 0f;
        panel.SetActive(false);
        panel.transform.localScale = Vector3.one;
        onComplete?.Invoke();
    }

    [Header("UI Loading Screen")]
    public FusionLoadingScreen loadingScreen; // Kéo Panel Màn hình Loading vào đây

    async void KetNoi(GameMode cheDo)
    {
        // Lấy tên phòng hiện tại trong ô Input (nếu có)
        string tenPhong = "";
        if (inputSessionName != null)
        {
            tenPhong = inputSessionName.text.Trim();
        }

        // CHỐT CHẶN: Chỉ bắt lỗi nếu chơi Coop (Host/Client) mà quên nhập tên
        if (cheDo != GameMode.Single && string.IsNullOrEmpty(tenPhong))
        {
            Debug.LogError("<color=red>Hệ Thống:</color> Bò ơi, muốn chơi Coop thì gõ cái tên phòng vào đã!");
            return;
        }

        // ĐẶC CÁCH CHƠI ĐƠN: Tự tạo tên ảo nếu chưa nhập
        if (cheDo == GameMode.Single && string.IsNullOrEmpty(tenPhong))
        {
            tenPhong = "PhongChoiDon_" + System.Guid.NewGuid().ToString().Substring(0, 5);
        }

        // 1. KÍCH HOẠT MÀN HÌNH LOADING
        string modeName = cheDo == GameMode.Single ? "Chơi Đơn" : (cheDo == GameMode.Host ? "Tạo Phòng" : "Vào Phòng");
        var activeLoadingScreen = loadingScreen != null ? loadingScreen : FusionLoadingScreen.instance;
        
        if (activeLoadingScreen != null)
        {
            activeLoadingScreen.ShowLoading(tenPhong, modeName);
            activeLoadingScreen.UpdateStatus("Đang dọn dẹp tiến trình cũ...", 0.15f);
        }

        // 2. Dọn rác mạng cũ nếu có
        if (runner != null)
        {
            await runner.Shutdown();
            if (runner != null) Destroy(runner.gameObject);
        }

        if (activeLoadingScreen != null)
        {
            activeLoadingScreen.UpdateStatus("Đang kết nối Fusion Cloud Server...", 0.40f);
        }

        // 3. Khởi tạo NetworkRunner & ID riêng
        var idRieng = new AuthenticationValues(System.Guid.NewGuid().ToString());
        GameObject runnerObject = new GameObject("TienTrinhFusion");
        runner = runnerObject.AddComponent<NetworkRunner>();

        Debug.Log("<color=green>Hệ Thống:</color> Đang phi vào phòng: " + tenPhong);

        if (activeLoadingScreen != null)
        {
            activeLoadingScreen.UpdateStatus("Đang nạp dữ liệu bản đồ...", 0.75f);
        }

        // 4. Bắt đầu kết nối và Load Scene (có try-catch xử lý lỗi)
        try
        {
            var result = await runner.StartGame(new StartGameArgs()
            {
                GameMode = cheDo,
                SessionName = tenPhong,
                AuthValues = idRieng,
                Scene = SceneRef.FromIndex(1), // Đảm bảo Map 1 nằm ở vị trí 1 trong Build Settings
                SceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
            {
                if (activeLoadingScreen != null)
                {
                    activeLoadingScreen.UpdateStatus("Kết nối thành công! Đang vào game...", 1.0f);
                }
            }
            else
            {
                string errorMsg = result.ShutdownReason.ToString();
                Debug.LogError("<color=red>Hệ Thống:</color> Kết nối thất bại: " + errorMsg);
                if (activeLoadingScreen != null)
                {
                    activeLoadingScreen.ShowError(errorMsg);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("<color=red>Hệ Thống:</color> Lỗi ngoại lệ khi kết nối: " + ex.Message);
            if (activeLoadingScreen != null)
            {
                activeLoadingScreen.ShowError(ex.Message);
            }
        }
    }

    [Header("Unity Animator Settings")]
    public Animator menuAnimator; // Kéo Animator của MenuGame vào đây (nếu không kéo sẽ tự GetComponent)

    void Start()
    {
        if (menuAnimator == null) menuAnimator = GetComponent<Animator>();

        // Ban đầu: Hiện nút, ẩn ô nhập liệu
        if (nutTaoTenPhong != null) nutTaoTenPhong.SetActive(true);
        if (inputSessionName != null) inputSessionName.gameObject.SetActive(false);

        // Chạy animation MenuOpen ban đầu
        PlayMenuOpenAnim();
    }

    // --- ANIMATION HELPER FUNCTIONS ---
    public void PlayMenuOpenAnim()
    {
        if (menuAnimator == null) menuAnimator = GetComponent<Animator>();
        if (menuAnimator != null)
        {
            menuAnimator.Play("MenuOpen", 0, 0f);
        }
    }

    public void PlayMultipOpenAnim()
    {
        if (menuAnimator == null) menuAnimator = GetComponent<Animator>();
        if (menuAnimator != null)
        {
            menuAnimator.Play("MultipOpen", 0, 0f);
        }
    }

    // --- LOGIC CÁC NÚT BẤM ---
    public void BamNut_ChoiDon() { KetNoi(GameMode.Single); }
    public void BamNut_TaoPhong() { KetNoi(GameMode.Host); }
    public void BamNut_VaoPhong() { KetNoi(GameMode.Client); }

    public void BamNut_Coop()
    {
        PlayMultipOpenAnim(); // Chạy animation clip MultipOpen
        if (coopPlayer != null) coopPlayer.SetActive(true);
        if (menu != null) menu.SetActive(false);
    }

    public void BamNut_Menu()
    {
        PlayMenuOpenAnim(); // Chạy animation clip MenuOpen
        if (coopPlayer != null) coopPlayer.SetActive(false);
        if (menu != null) menu.SetActive(true);

        // Reset lại trạng thái: Hiện nút, ẩn ô nhập
        if (nutTaoTenPhong != null) nutTaoTenPhong.SetActive(true);
        if (inputSessionName != null) inputSessionName.gameObject.SetActive(false);
    }

    // --- LOGIC HIỆU ỨNG DI CHUỘT (HOVER) ---
    public void DiChuotVao(GameObject buttonObj)
    {
        // Lưu kích thước ban đầu của Button (chỉ lưu 1 lần)
        if (!originalScales.ContainsKey(buttonObj))
        {
            originalScales.Add(buttonObj, buttonObj.transform.localScale);
        }

        // Đổi màu
        Image img = buttonObj.GetComponent<Image>();
        if (img != null)
        {
            img.color = hoverColor;
        }

        // Phóng to 10%
        buttonObj.transform.localScale = originalScales[buttonObj] * 1.1f;
    }

    public void DiChuotRa(GameObject buttonObj)
    {
        Image img = buttonObj.GetComponent<Image>();
        if (img != null)
        {
            img.color = Color.white; // Hoặc màu gốc của Button
        }

        // Trả về kích thước ban đầu
        if (originalScales.ContainsKey(buttonObj))
        {
            buttonObj.transform.localScale = originalScales[buttonObj];
        }
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

