
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
    public GameObject panelSettings;     // Kéo Panel Settings vào đây
    public GameObject panelCredits;      // Kéo Panel Credits vào đây

    [Header("UI Danh Sách File Save")]
    public GameObject panelDanhSachSave;   // Panel chứa bảng danh sách Save
    public Transform saveSlotContainer;    // Content container chứa các ô Save
    public GameObject saveSlotPrefab;      // Prefab thẻ thông tin Save (chứa script SaveSlotItemUI)

    [Header("UI Hover Settings")]
    public Color hoverColor = new Color(1f, 0.95f, 0.6f);
    public Color hoverColortext = Color.yellow; // Chỉnh màu chữ khi di chuột vào trực tiếp ở Inspector
    private Color normalColor = Color.black;

    [Header("UI Animation Settings")]
    public bool enablePanelAnimation = true;
    [Tooltip("Thời gian chạy animation mở/đóng menu (giây)")]
    public float animDuration = 0.22f;
    [Tooltip("Tỷ lệ scale ban đầu khi bắt đầu mở pop-in")]
    public Vector3 animStartScale = new Vector3(0.85f, 0.85f, 0.85f);



    // --- CÁC HÀM TÙY CHỈNH DỄ DÀNG CHO BÒ ---

    // 1. Hàm đổi màu Nền & màu Chữ khi di chuột vào nút
    public void DatMauHover(Color mauNenHover, Color mauChuHover)
    {
        hoverColor = mauNenHover;
        hoverColortext = mauChuHover;
    }

    // 2. Hàm đổi dòng chữ mờ hướng dẫn trong ô nhập tên phòng
    public void DatTextHuongDanOInput(string textHuongDan)
    {
        if (inputSessionName != null && inputSessionName.placeholder != null)
        {
            var txtPlaceholder = inputSessionName.placeholder.GetComponent<TextMeshProUGUI>();
            if (txtPlaceholder != null) txtPlaceholder.text = textHuongDan;
        }
    }

    // 3. Hàm đổi đường dẫn lưu Save
    public void DatDuongDanSave(string duongDanMoi)
    {
        if (SaveManager.instance != null)
        {
            SaveManager.instance.customSavePath = duongDanMoi;
        }
    }

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
        ResetTatCaMauHover();
        
        CanvasGroup cg = targetPanel.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;
        targetPanel.transform.localScale = Vector3.one;

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
            CanvasGroup cg = targetPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            targetPanel.transform.localScale = Vector3.one;
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

        panel.SetActive(false);
        cg.alpha = 1f;
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

        // Nếu quên gõ tên phòng ➔ Tự động tạo tên phòng mặc định
        if (string.IsNullOrEmpty(tenPhong))
        {
            tenPhong = "TheGioi_" + System.DateTime.Now.ToString("ddHHmm");
            if (inputSessionName != null) inputSessionName.text = tenPhong;
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

        // Ban đầu: Hiển thị cả Nút Tạo Phòng lẫn Ô Nhập Tên
        if (nutTaoTenPhong != null) nutTaoTenPhong.SetActive(true);
        if (inputSessionName != null)
        {
            inputSessionName.gameObject.SetActive(true);
            if (inputSessionName.placeholder != null)
            {
                var txtPlaceholder = inputSessionName.placeholder.GetComponent<TextMeshProUGUI>();
                if (txtPlaceholder != null) txtPlaceholder.text = "Room's Name";
            }
        }
        
        // Chạy animation MenuOpen ban đầu
        PlayMenuOpenAnim();
        HienThiDanhSachSave();
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
    public void BamNut_Single() { KetNoi(GameMode.Single); }
    public void BamNut_TaoPhong() { KetNoi(GameMode.Host); }
    public void BamNut_VaoPhong() { KetNoi(GameMode.Client); }

    public void BamNut_Mul() { BamNut_Coop(); }
    public void BamNut_Coop()
    {
        ResetTatCaMauHover();
        PlayMultipOpenAnim(); // Chạy animation clip MultipOpen từ Unity Animator

        if (coopPlayer != null)
        {
            coopPlayer.SetActive(true);
            CanvasGroup cg = coopPlayer.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            coopPlayer.transform.localScale = Vector3.one; // Đảm bảo luôn giữ kích thước 100%
        }

        if (menu != null) menu.SetActive(false);
        HienThiDanhSachSave(); // Tự động load danh sách file save đã có lên giao diện!
    }

    public void BamNut_Settings()
    {
        if (panelSettings != null) OpenPanelAnimated(panelSettings);
        if (panelCredits != null) panelCredits.SetActive(false);
        if (menu != null) menu.SetActive(false);
        if (coopPlayer != null) coopPlayer.SetActive(false);
    }

    public void BamNut_Credits()
    {
        if (panelCredits != null) OpenPanelAnimated(panelCredits);
        if (panelSettings != null) panelSettings.SetActive(false);
        if (menu != null) menu.SetActive(false);
        if (coopPlayer != null) coopPlayer.SetActive(false);
    }

    public void BamNut_Exit()
    {
        ThucHienThoatGame();
    }

    public void ThucHienThoatGame()
    {
        Debug.Log("<color=yellow>[Menu_Fusion]:</color> Bắt đầu thoát Game...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // Nút Back / Quay lại Menu Chính
    public void BamNut_Back()
    {
        BamNut_Menu();
    }

    public void BamNut_Menu()
    {
        ResetTatCaMauHover();
        PlayMenuOpenAnim(); // Chạy animation clip MenuOpen từ Unity Animator
        if (panelSettings != null) ClosePanelAnimated(panelSettings);
        if (panelCredits != null) ClosePanelAnimated(panelCredits);
        if (panelDanhSachSave != null) ClosePanelAnimated(panelDanhSachSave);

        if (coopPlayer != null)
        {
            coopPlayer.SetActive(false);
            CanvasGroup cg = coopPlayer.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            coopPlayer.transform.localScale = Vector3.one;
        }

        if (menu != null) menu.SetActive(true);

        // Reset lại trạng thái: Hiển thị cả Nút Tạo Phòng lẫn Ô Nhập Tên
        if (nutTaoTenPhong != null) nutTaoTenPhong.SetActive(true);
        if (inputSessionName != null) inputSessionName.gameObject.SetActive(true);
    }

    // --- HIỂN THỊ DANH SÁCH FILE SAVE VÀ CHỌN FILE SAVE ---
    public void BamNut_MoDanhSachSave()
    {
        if (panelDanhSachSave != null)
        {
            OpenPanelAnimated(panelDanhSachSave);
            HienThiDanhSachSave();
        }
    }

    public void BamNut_DongDanhSachSave()
    {
        if (panelDanhSachSave != null)
        {
            ClosePanelAnimated(panelDanhSachSave);
        }
    }

    public void HienThiDanhSachSave()
    {
        if (saveSlotContainer == null)
        {
            GameObject obj = GameObject.Find("saveSlotContainer");
            if (obj != null) saveSlotContainer = obj.transform;
        }

        if (saveSlotContainer == null) return;

        Transform targetContainer = saveSlotContainer;
        ScrollRect sr = saveSlotContainer.GetComponent<ScrollRect>();
        if (sr != null && sr.content != null)
        {
            targetContainer = sr.content;
        }

        foreach (Transform child in targetContainer)
        {
            Destroy(child.gameObject);
        }

        List<WorldSaveData> saves = SaveManager.instance != null ? SaveManager.instance.GetAllSaves() : new List<WorldSaveData>();

        if (saves.Count == 0)
        {
            GameObject emptyTextObj = new GameObject("TxtEmptySave", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyTextObj.transform.SetParent(targetContainer, false);
            TextMeshProUGUI txtEmpty = emptyTextObj.GetComponent<TextMeshProUGUI>();
            txtEmpty.text = "Chưa có file save nào.\nHãy bấm 'Tạo Phòng' để bắt đầu thế giới mới!";
            txtEmpty.fontSize = 20;
            txtEmpty.alignment = TextAlignmentOptions.Center;
            txtEmpty.color = Color.yellow;
            return;
        }

        foreach (var saveData in saves)
        {
            GameObject slotObj = (saveSlotPrefab != null) ? Instantiate(saveSlotPrefab, targetContainer) : TaoTheSaveMacDinh(targetContainer);
            if (slotObj != null)
            {
                SaveSlotItemUI itemUI = slotObj.GetComponent<SaveSlotItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(saveData, ChonFileSave, XoaFileSave);
                }
            }
        }
    }

    private GameObject TaoTheSaveMacDinh(Transform parent)
    {
        GameObject slotObj = new GameObject("SaveSlotItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup));
        slotObj.transform.SetParent(parent, false);

        RectTransform rect = slotObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(450, 65);

        Image img = slotObj.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

        HorizontalLayoutGroup hlg = slotObj.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(12, 12, 6, 6);
        hlg.spacing = 10;

        GameObject txtObj = new GameObject("TxtInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(slotObj.transform, false);
        TextMeshProUGUI txtInfo = txtObj.GetComponent<TextMeshProUGUI>();
        txtInfo.fontSize = 18;
        txtInfo.color = Color.white;
        txtInfo.alignment = TextAlignmentOptions.Left;

        GameObject btnDelObj = new GameObject("BtnDelete", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnDelObj.transform.SetParent(slotObj.transform, false);
        btnDelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 45);
        btnDelObj.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 0.9f);

        GameObject txtDelObj = new GameObject("TxtDel", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtDelObj.transform.SetParent(btnDelObj.transform, false);
        TextMeshProUGUI txtDel = txtDelObj.GetComponent<TextMeshProUGUI>();
        txtDel.text = "Xóa";
        txtDel.fontSize = 16;
        txtDel.color = Color.white;
        txtDel.alignment = TextAlignmentOptions.Center;

        SaveSlotItemUI itemUI = slotObj.AddComponent<SaveSlotItemUI>();
        itemUI.txtSessionName = txtInfo;
        itemUI.txtSaveTime = null;
        itemUI.txtGameTime = null;
        itemUI.btnSelect = slotObj.GetComponent<Button>();
        itemUI.btnDelete = btnDelObj.GetComponent<Button>();

        return slotObj;
    }

    public void ChonFileSave(string sessionName)
    {
        if (inputSessionName != null)
        {
            inputSessionName.text = sessionName;
        }
        KetNoi(GameMode.Host);
    }

    public void XoaFileSave(string sessionName)
    {
        if (SaveManager.instance != null)
        {
            SaveManager.instance.DeleteSave(sessionName);
            HienThiDanhSachSave();
        }
    }

    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();
    private Dictionary<GameObject, Color> originalTextColors = new Dictionary<GameObject, Color>();

    // --- LOGIC HIỆU ỨNG DI CHUỘT (HOVER) 3 TRƯỜNG HỢP ---
    
    // 🟩 TRƯỜNG HỢP 1: CHỈ ĐỔI MÀU NỀN + PHÓNG TO (Không đổi màu chữ)
    public void DiChuotVaoNen(GameObject buttonObj)
    {
        if (buttonObj == null) return;

        if (!originalScales.ContainsKey(buttonObj))
        {
            originalScales.Add(buttonObj, buttonObj.transform.localScale);
        }

        Image img = buttonObj.GetComponent<Image>();
        if (img != null)
        {
            if (!originalColors.ContainsKey(buttonObj))
            {
                originalColors.Add(buttonObj, img.color);
            }
            img.color = hoverColor;
        }

        buttonObj.transform.localScale = originalScales[buttonObj] * 1.1f;
    }

    public void DiChuotRaNen(GameObject buttonObj)
    {
        if (buttonObj == null) return;

        Image img = buttonObj.GetComponent<Image>();
        if (img != null && originalColors.ContainsKey(buttonObj))
        {
            img.color = originalColors[buttonObj];
        }

        if (originalScales.ContainsKey(buttonObj))
        {
            buttonObj.transform.localScale = originalScales[buttonObj];
        }
    }

    // 🟨 TRƯỜNG HỢP 2: CHỈ ĐỔI MÀU CHỮ + PHÓNG TO (Không đổi màu nền Image)
    public void DiChuotVaoChu(GameObject buttonObj)
    {
        if (buttonObj == null) return;

        if (!originalScales.ContainsKey(buttonObj))
        {
            originalScales.Add(buttonObj, buttonObj.transform.localScale);
        }

        DoiMauChuHover(buttonObj, true);
        buttonObj.transform.localScale = originalScales[buttonObj] * 1.1f;
    }

    public void DiChuotRaChu(GameObject buttonObj)
    {
        if (buttonObj == null) return;

        DoiMauChuHover(buttonObj, false);

        if (originalScales.ContainsKey(buttonObj))
        {
            buttonObj.transform.localScale = originalScales[buttonObj];
        }
    }

    // 🟧 TRƯỜNG HỢP 3: VỪA ĐỔI MÀU NỀN + VỪA ĐỔI MÀU CHỮ + PHÓNG TO
    public void DiChuotVao(GameObject buttonObj)
    {
        if (buttonObj == null) return;

        if (!originalScales.ContainsKey(buttonObj))
        {
            originalScales.Add(buttonObj, buttonObj.transform.localScale);
        }

        Image img = buttonObj.GetComponent<Image>();
        if (img != null)
        {
            if (!originalColors.ContainsKey(buttonObj))
            {
                originalColors.Add(buttonObj, img.color);
            }
            img.color = hoverColor;
        }

        DoiMauChuHover(buttonObj, true);
        buttonObj.transform.localScale = originalScales[buttonObj] * 1.1f;
    }

    public void DiChuotRa(GameObject buttonObj)
    {
        if (buttonObj == null) return;

        Image img = buttonObj.GetComponent<Image>();
        if (img != null && originalColors.ContainsKey(buttonObj))
        {
            img.color = originalColors[buttonObj];
        }

        DoiMauChuHover(buttonObj, false);

        if (originalScales.ContainsKey(buttonObj))
        {
            buttonObj.transform.localScale = originalScales[buttonObj];
        }
    }

    private void DoiMauChuHover(GameObject buttonObj, bool dangHover)
    {
        TextMeshProUGUI tmpText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            if (!originalTextColors.ContainsKey(tmpText.gameObject))
            {
                originalTextColors.Add(tmpText.gameObject, tmpText.color);
            }
            tmpText.color = dangHover ? hoverColortext : originalTextColors[tmpText.gameObject];
        }
        else
        {
            Text uiText = buttonObj.GetComponentInChildren<Text>();
            if (uiText != null)
            {
                if (!originalTextColors.ContainsKey(uiText.gameObject))
                {
                    originalTextColors.Add(uiText.gameObject, uiText.color);
                }
                uiText.color = dangHover ? hoverColortext : originalTextColors[uiText.gameObject];
            }
        }
    }

    public void ResetTatCaMauHover()
    {
        foreach (var kvp in originalColors)
        {
            if (kvp.Key != null)
            {
                Image img = kvp.Key.GetComponent<Image>();
                if (img != null) img.color = kvp.Value;
            }
        }

        foreach (var kvp in originalTextColors)
        {
            if (kvp.Key != null)
            {
                TextMeshProUGUI tmpText = kvp.Key.GetComponent<TextMeshProUGUI>();
                if (tmpText != null) tmpText.color = kvp.Value;
                else
                {
                    Text uiText = kvp.Key.GetComponent<Text>();
                    if (uiText != null) uiText.color = kvp.Value;
                }
            }
        }

        foreach (var kvp in originalScales)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.localScale = kvp.Value;
            }
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

