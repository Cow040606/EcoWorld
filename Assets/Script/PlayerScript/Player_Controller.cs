using UnityEngine;
using Fusion;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

public struct DuLieuInput : INetworkInput
{
    public Vector2 moveInput;
    public NetworkBool isJumpPressed;
    public float mouseX;
    public NetworkBool isRunfast;
    public NetworkBool isDashPressed;
}

public struct O_VatPham : INetworkStruct
{
    public int ItemID;
    public int SoLuong;
}

public class Player_Controller : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static Player_Controller localPlayer;

    #region 1. KHAI BÁO BIẾN (VARIABLES)
    [Header("Quán tính")]
    [Networked] public float currentSpeedSmooth { get; set; }
    public float giaToc = 8f;    
    public float giamToc = 12f;  
    public float tocDoXoay = 15f;

    [Header("Di chuyển")]
    public NetworkCharacterController character;
    public float speed = 5f;
    public float runfast = 15f;
    private Vector2 moveInputLocal;
    private bool sprintPressedLocal;

    [Header("Trọng lực & Nhảy")]
    [Networked] public bool isJumping { get; set; }
    private bool jumpPressedLocal;
    public float thoiGianHoiNhay = 1f;
    [Networked] public TickTimer dongHoChoNhay { get; set; }
    [Header("Cơ chế Lướt (Dash) & I-Frames")]
    public float dashSpeed = 18f;           
    public float thoiGianDash = 0.25f;     
    public float thoiGianHoiDash = 1.2f;    
    public float theLucTieuHaoDash = 15f;   
    [Networked] public NetworkBool isDashing { get; set; }
    [Networked] public NetworkBool isInvincible { get; set; } 
    [Networked] public TickTimer dongHoDash { get; set; }
    [Networked] public TickTimer dongHoHoiDash { get; set; }
    private bool dashPressedLocal;

    [Header("Trạng Thái Sinh Mệnh")]
    [Networked] public NetworkBool isDead { get; set; }
    [Networked] public TickTimer dongHoHoiSinh { get; set; }

    [Header("Balo Rơi Khi Chết")]
    public GameObject droppedBackpackPrefab;

    [Header("Chỉ số nhân vật (GỐC & TỔNG)")]
    public float baseMaxHealth = 100f;
    public float baseMaxStamina = 100f;
    public float baseMaxArmor = 0f;
    public float baseSpeed = 5f;
    public float baseDamage = 25f;

    [Networked] public float CurrentHealth { get; set; }
    [Networked] public float MaxHealth { get; set; }
    [Networked] public float CurrentStamina { get; set; }
    [Networked] public float MaxStamina { get; set; }
    [Networked] public float CurrentArmor { get; set; }
    [Networked] public float MaxArmor { get; set; }
    public float tocDoTut = 20f;
    public float tocDoHoi = 15f;

    [Header("Hệ Thống Phân Bổ Điểm")]
    [Networked] public int AvailablePoints { get; set; } 
    [Networked] public int DiemSucManh { get; set; }     
    [Networked] public int DiemTheLuc { get; set; }     
    [Networked] public int DiemNhanhNhen { get; set; }
    [Networked] public int DiemMau { get; set; }

    [HideInInspector] public bool dangChayNhanh = false;
    public float thoiGianDelayHoi = 2f;
    private float dongHoDelayHoi = 0f;

    public float ExpCurrent { get; set; }
    public int level = 0;
    public float expToLevelUp = 100f;

    [Header("Trạng Thái Tiêu Hao")]
    private bool dangSuDungVatPham = false;
    private Coroutine tienTrinhDungItem;

    [Header("Camera & Chuột")]
    public Transform cameraTransform;
    public Camera playerCamera;
    public float mouseSensitivity = 0.5f;
    private float xRotation = 0f;
    private float yRotation = 0f;
    public float khoangCachCamera = 4f;
    // --- BỔ SUNG BIẾN ZOOM CAMERA ---
    public float khoangCachMin = 1.5f; 
    public float khoangCachMax = 10f;  
    public float tocDoZoomChuot = 0.5f; 
    private float khoangCachMucTieu;    
    // --------------------------------
    private float mouseXLocalAcc;
    public LayerMask layerVaChamCamera;
    public float fovBinhThuong = 60f;
    public float fovChayNhanh = 75f;
    public float tocDoZoom = 5f;

    [Header("Kinh tế & Túi đồ")]
    [Networked] public int Gold { get; set; }
    [Networked] public int Gem { get; set; }
    [Networked, Capacity(20)] public NetworkArray<O_VatPham> TuiDo { get; }
    [Networked, OnChangedRender(nameof(OnHotbarChanged)), Capacity(6)] public NetworkArray<int> HotbarIDs { get; }

    private void OnHotbarChanged()
    {
        OnToolChanged();
    }

    [Header("Animation & Vũ Khí")]
    [Networked] private NetworkBool isrun { get; set; }
    [Networked] private NetworkBool isSprinting { get; set; }
    private Animator animator;
    [Networked, OnChangedRender(nameof(OnToolChanged))] public int CurrentToolIndex { get; set; }
    public Transform viTriCamVuKhi;
    private GameObject vuKhiDangCamThucTe;

    [Header("Tương Tác & Thu Thập")]
    public float banKinhNhat = 5f;
    public float interactRange = 10f;
    public LayerMask interactLayer;
    public LayerMask chopLayer;
    public LayerMask rockLayer;
    public float attackDamageToAnimal = 25f;
    public LayerMask attackLayer;

    [Header("Hệ thống chém Combo")]
    public float minAttackCooldown = 0.5f; // Khoảng thời gian nhỏ nhất giữa 2 lần chém (chống spam)
    public float slashLockDuration = 0.5f; // Thời gian đứng yên (không di chuyển/nhảy) khi chém
    public float comboResetTime = 0.7f;
    public float comboFinishCooldown = 1f; // Thời gian delay (nghỉ) sau khi tung xong combo 3 hit
    private int currentComboStep = 0;
    private float lastAttackTime = 0f;
    private float comboCooldownEndTime = 0f;

    [Header("Trạng thái Hành Động (Chặt/Đào)")]
    public float actionDelay = 0.8f;
    private float lastActionTime = 0f;
    public float hitboxOffset = 1.5f;
    public float hitboxRadius = 1.5f;
    [Networked] public NetworkBool isDoingAction { get; set; }
    [Networked] public TickTimer actionTimer { get; set; }
    [Networked] public TickTimer hitTimer { get; set; }
    [Networked] public int pendingActionType { get; set; }
    private float thoiDiemHetKhoaCucBo = 0f; // Biến này giúp khóa di chuyển ngay lập tức trên máy Client

    [Header("Hệ Thống Câu Cá")]
    public LayerMask waterLayer;
    public GameObject Phaocauca;
    public GameObject iconCamThan;
    private GameObject currentphaocauca;
    private Coroutine cauCaCoroutine;
    public float khoangCachDutDay = 10f;
    public enum FishState { Idle, Casting, Waiting, Giatca }
    public FishState currentState = FishState.Idle;

    [Header("Chức năng Nông Trại")]
    public LayerMask farmlandLayer;
    public TextMeshProUGUI hintText;
    private FarmPlot currentLookedPlot;

    [Header("Debug")]
    public bool showChopDebug = true;
    public bool showDebugRay = true;
    private Vector3 debugRayOrigin;
    private Vector3 debugRayDirection;
    private float debugRayDistance;
    private bool didRayHit;
    private Vector3 rayHitPoint;
    private Vector3 _lastRayOrigin, _lastRayDir, _lastRayHitPoint;
    private bool _lastRayHit;

    #endregion

    #region 2. KHỞI TẠO & VÒNG LẶP CHÍNH (SPAWN, UPDATE, FIXED UPDATE, RENDER)

    public override void Spawned()
    {
        animator = GetComponent<Animator>();
        CurrentHealth = 100;
        ExpCurrent = 0;

        if (hintText != null) hintText.text = "";

        if (!HasStateAuthority && !HasInputAuthority)
        {
            if (character != null) character.enabled = false;
        }

        if (HasInputAuthority)
        {
            localPlayer = this;
            Runner.AddCallbacks(this);
            Runner.SetPlayerObject(Runner.LocalPlayer, Object);
            
            // --- KHỞI TẠO ĐỒNG BỘ ZOOM LÚC MỚI VÀO GAME ---
            khoangCachMucTieu = khoangCachCamera; 

            GameObject objHint = GameObject.Find("Text_Hint");
            if (objHint != null) hintText = objHint.GetComponent<TextMeshProUGUI>();

            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            
            if (cameraTransform != null) cameraTransform.SetParent(null); 
            
            if (playerCamera == null) Debug.LogError("[Player_Controller] ❌ 'Player Camera' chưa được gán!");

            // --- NẠP & LƯU TỰ ĐỘNG KHI VỪA VÀO PHÒNG ---
            if (SaveManager.instance != null && Runner != null && Runner.SessionInfo.IsValid)
            {
                SaveManager.instance.LoadGame(Runner.SessionInfo.Name, this);
                LuuGameHienTai();
            }

            StartCoroutine(TienTrinhTuDongSave());
        }
        else
        {
            if (character != null) character.enabled = true;
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = false;
            AudioListener listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        if (HasStateAuthority)
        {
            MaxHealth = baseMaxHealth;
            MaxStamina = baseMaxStamina;
            speed = baseSpeed;
            attackDamageToAnimal = baseDamage;
            MaxArmor = baseMaxArmor;
            CurrentArmor = MaxArmor;
            if (CurrentHealth <= 0) CurrentHealth = MaxHealth;
            if (CurrentStamina <= 0) CurrentStamina = MaxStamina;
        }
    }

    private System.Collections.IEnumerator TienTrinhTuDongSave()
    {
        while (true)
        {
            yield return new WaitForSeconds(180f); // Tự động lưu mỗi 3 phút (180s)
            LuuGameHienTai();
        }
    }

    public void LuuGameHienTai()
    {
        if (!HasInputAuthority || SaveManager.instance == null) return;

        string nameToSave = (Runner != null && Runner.SessionInfo.IsValid && !string.IsNullOrEmpty(Runner.SessionInfo.Name)) 
            ? Runner.SessionInfo.Name 
            : "TheGioi_AutoSave";

        SaveManager.instance.SaveGame(nameToSave, this);
    }

    private void OnApplicationQuit()
    {
        LuuGameHienTai();
    }

    void Update()
    {
        if (HasInputAuthority && Keyboard.current != null && Mouse.current != null)
        {
            if (isDead)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    TatToanBoUI();
                    if (ESC.instance != null) ESC.instance.BatTatESC();
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                moveInputLocal = Vector2.zero;
                jumpPressedLocal = false;
                dashPressedLocal = false;
                sprintPressedLocal = false;
                return;
            }

            RPC_AddExp(0.1f);

            if (KiemTraDangGoPhimChat()) return;

            bool isBaloOpen = (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo);
            bool isShopOpen = (ShopUIController.instance != null && ShopUIController.instance.isShopOpen);
            bool isQuestOpen = (QuestManager.instance != null && QuestManager.instance.isQuest_Open);
            bool isChatActive = (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive);
            bool isEscOpen = (ESC.instance != null && ESC.instance.isESC_Open);
            bool isMapOpen = (MapManager.Instance != null && MapManager.Instance.dangMoMap);
            bool isCraftOpen = (ShopUIController.instance != null && ShopUIController.instance.dangMoCraft);
            bool isCutsceneActive = NPC_DialogueTrigger.isCutsceneActive;

            bool batKyUI_NaoDangMo = isBaloOpen || isEscOpen || isShopOpen || isChatActive || isQuestOpen || isMapOpen || isCraftOpen || isCutsceneActive;

            if (batKyUI_NaoDangMo)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (hintText != null) hintText.text = "";
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                yRotation += Mouse.current.delta.x.ReadValue() * mouseSensitivity;
                xRotation -= Mouse.current.delta.y.ReadValue() * mouseSensitivity;
                xRotation = Mathf.Clamp(xRotation, -60f, 60f);

                // --- BẮT SỰ KIỆN LĂN CHUỘT ---
                float scrollValue = Mouse.current.scroll.y.ReadValue();
                if (scrollValue != 0)
                {
                    khoangCachMucTieu -= Mathf.Sign(scrollValue) * tocDoZoomChuot;
                    khoangCachMucTieu = Mathf.Clamp(khoangCachMucTieu, khoangCachMin, khoangCachMax);
                }

                int idDangCam = (CurrentToolIndex >= 0) ? HotbarIDs[CurrentToolIndex] : 0;
                UpdateFarmingUI(idDangCam);
            }

            HandleUIGlobalInput(isChatActive, isShopOpen);
            HandleHotbarInput(isBaloOpen, isShopOpen, isChatActive, isEscOpen, isQuestOpen);

            if (Keyboard.current.cKey.wasPressedThisFrame) RPC_TakeDame(10);
            if (Keyboard.current.vKey.wasPressedThisFrame) RPC_TakeDame(-10);
            if (Keyboard.current.kKey.wasPressedThisFrame) RPC_ThayDoiTien(5);
            if (Keyboard.current.lKey.wasPressedThisFrame) RPC_ThayDoiTien(-5);

            if (!batKyUI_NaoDangMo)
            {
                int idDangCam = (CurrentToolIndex >= 0) ? HotbarIDs[CurrentToolIndex] : 0;
                HandleGameplayInteraction(idDangCam);
            }

            sprintPressedLocal = Keyboard.current.leftShiftKey.isPressed;
            XuLyTheLuc();
            if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressedLocal = true;
            if (Keyboard.current.leftCtrlKey.wasPressedThisFrame) dashPressedLocal = true;

            float trucX = Keyboard.current.dKey.isPressed ? 1f : (Keyboard.current.aKey.isPressed ? -1f : 0f);
            float trucY = Keyboard.current.wKey.isPressed ? 1f : (Keyboard.current.sKey.isPressed ? -1f : 0f);
            moveInputLocal = new Vector2(trucX, trucY).normalized;

            // FIX: Khóa di chuyển ngay lập tức trên máy của người chơi khi đang chém/chặt
            if (Time.time < thoiDiemHetKhoaCucBo)
            {
                moveInputLocal = Vector2.zero;
                jumpPressedLocal = false;
                dashPressedLocal = false;
                sprintPressedLocal = false;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        // Kiểm tra chết trên server để spawn balo và đặt isDead = true
        if (CurrentHealth <= 0 && !isDead)
        {
            if (HasStateAuthority)
            {
                isDead = true;
                dongHoHoiSinh = TickTimer.CreateFromSeconds(Runner, 3f); // Chờ 3s rồi hồi sinh
                DropBackpackOnDeath();
            }
        }
        else if (CurrentHealth > 0 && isDead)
        {
            if (HasStateAuthority)
            {
                isDead = false;
            }
        }

        if (isDead)
        {
            character.Move(Vector3.zero);
            isrun = isSprinting = isJumping = false;
            
            // Nếu đã hết thời gian đếm ngược thì hồi sinh
            if (HasStateAuthority && dongHoHoiSinh.Expired(Runner))
            {
                dongHoHoiSinh = TickTimer.None;
                HoiSinhNhanVat();
            }

            return;
        }

        if (KiemTraDangGoPhimChat())
        {
            character.Move(Vector3.zero);
            isrun = isSprinting = isJumping = false;
            return;
        }

        if (isDoingAction)
        {
            character.Move(Vector3.zero);
            isrun = isSprinting = isJumping = false;

            if (hitTimer.Expired(Runner))
            {
                hitTimer = TickTimer.None;
                if (pendingActionType == 1) ThucHienXetVaChamChop();
                else if (pendingActionType == 2) ThucHienXetVaChamMine();
            }

            if (actionTimer.Expired(Runner))
            {
                isDoingAction = false;
                pendingActionType = 0;
                actionTimer = TickTimer.None;
            }
            return;
        }

        if (GetInput(out DuLieuInput data))
        {
            if (isDashing)
            {
                if (dongHoDash.Expired(Runner))
                {
                    // Hết thời gian Dash -> Tắt trạng thái và mất I-frames
                    isDashing = false;
                    isInvincible = false; 
                }
                else
                {
                    // Đang trong lúc Dash -> Ép tốc độ bàn thờ và khóa các nút di chuyển khác
                    Vector3 huongLuoT = new Vector3(data.moveInput.x, 0f, data.moveInput.y);
                    if (huongLuoT.magnitude < 0.1f) huongLuoT = transform.forward; // Nếu không bấm phím hướng thì lướt tới trước

                    character.maxSpeed = dashSpeed;
                    character.Move(huongLuoT.normalized);
                    
                    Quaternion huongMucTieu = Quaternion.LookRotation(huongLuoT);
                    transform.rotation = Quaternion.Slerp(transform.rotation, huongMucTieu, Runner.DeltaTime * tocDoXoay * 2f);
                    
                    return; // KHÓA CỨNG: Bỏ qua đoạn code nhảy và chạy bình thường ở dưới
                }
            }

            // Kích hoạt Dash nếu bấm Ctrl, chạm đất, hồi chiêu xong và đủ thể lực
            if (!isDashing && data.isDashPressed && character.Grounded && dongHoHoiDash.ExpiredOrNotRunning(Runner) && CurrentStamina >= theLucTieuHaoDash)
            {
                CurrentStamina -= theLucTieuHaoDash; // Trừ thể lực
                isDashing = true;
                isInvincible = true; // KÍCH HOẠT VÔ ĐỊCH
                dongHoDash = TickTimer.CreateFromSeconds(Runner, thoiGianDash);
                dongHoHoiDash = TickTimer.CreateFromSeconds(Runner, thoiGianHoiDash);
                RPC_AnimDash(); // Kích hoạt Animation
                return;
            }
            if (data.isJumpPressed && character.Grounded)
            {
                if (dongHoChoNhay.ExpiredOrNotRunning(Runner))
                {
                    character.Jump();
                    isJumping = true;
                    dongHoChoNhay = TickTimer.CreateFromSeconds(Runner, thoiGianHoiNhay);
                }
            }
            else if (character.Grounded)
            {
                isJumping = false;
            }

            Vector3 huongDiChuyen = new Vector3(data.moveInput.x, 0f, data.moveInput.y);
            bool dangBampi = huongDiChuyen.magnitude > 0.1f;

            isrun = dangBampi;
            isSprinting = isrun && data.isRunfast;

            float targetSpeed = 0f;
            if (dangBampi) targetSpeed = isSprinting ? runfast : speed;

            if (dangBampi) {
                currentSpeedSmooth = Mathf.Lerp(currentSpeedSmooth, targetSpeed, Runner.DeltaTime * giaToc);
            } else {
                currentSpeedSmooth = Mathf.Lerp(currentSpeedSmooth, 0f, Runner.DeltaTime * giamToc);
            }

            character.maxSpeed = currentSpeedSmooth;

            if (currentSpeedSmooth > 0.1f)
            {
                if (dangBampi)
                {
                    character.Move(huongDiChuyen.normalized);
                    Quaternion huongMucTieu = Quaternion.LookRotation(huongDiChuyen);
                    transform.rotation = Quaternion.Slerp(transform.rotation, huongMucTieu, Runner.DeltaTime * tocDoXoay);
                }
                else
                {
                    character.Move(transform.forward);
                }
            }
            else
            {
                character.Move(Vector3.zero);
            }
        }
    }

    public override void Render()
    {
        if (animator != null)
        {
            if (isDead)
            {
                //animator.SetBool("isDead", true);
                animator.SetFloat("Speed", 0f);
                animator.SetBool("isJump", false);
            }
            else
            {
                //animator.SetBool("isDead", false);
                if (isJumping)
                {
                    isSprinting = false;
                    isrun = false;
                    animator.SetBool("isJump", isJumping);
                }
                else
                {
                    animator.SetBool("isJump", false);
                }
                animator.SetFloat("Speed", currentSpeedSmooth);

                // --- XỬ LÝ ANIMATION CẦM CÔNG CỤ (KIẾM, RÌU, CÚP, CẦN CÂU) ---
                int idDangCam = (CurrentToolIndex >= 0 && CurrentToolIndex <= 5) ? HotbarIDs[CurrentToolIndex] : 0;
                bool isTool = (idDangCam == 4 || idDangCam == 5 || idDangCam == 6 || idDangCam == 8);

                if (isTool && !isJumping)
                {
                    animator.SetBool("isHoldingTool", true);
                }
                else
                {
                    animator.SetBool("isHoldingTool", false);
                }
            }
        }

        if (HasInputAuthority && cameraTransform != null)
        {
            // --- KÉO DÂY THUN KHOẢNG CÁCH CAMERA (LÀM MƯỢT ZOOM) ---
            khoangCachCamera = Mathf.Lerp(khoangCachCamera, khoangCachMucTieu, Time.deltaTime * 10f);

            Quaternion camRotationMucTieu = Quaternion.Euler(xRotation, yRotation, 0f);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, camRotationMucTieu, Time.deltaTime * 25f);

            Vector3 diemNhin = transform.position + Vector3.up * 1.5f;
            Vector3 huongCamera = -(cameraTransform.rotation * Vector3.forward); 
            Vector3 viTriDuKien = diemNhin + huongCamera * khoangCachCamera;

            Vector3 viTriCuoiCung;
            if (Physics.Raycast(diemNhin, huongCamera, out RaycastHit hit, khoangCachCamera, layerVaChamCamera))
                viTriCuoiCung = hit.point + hit.normal * 0.4f;
            else
                viTriCuoiCung = viTriDuKien;

            cameraTransform.position = viTriCuoiCung;

            if (playerCamera != null)
            {
                float fovMucTieu = isSprinting ? fovChayNhanh : fovBinhThuong;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fovMucTieu, Time.deltaTime * tocDoZoom);
            }
        }
    }

    #endregion

    #region 3. HỆ THỐNG NHẬP LIỆU (INPUT METHODS)

    private bool KiemTraDangGoPhimChat()
    {
        bool dangGoPhim = EventSystem.current != null &&
                          EventSystem.current.currentSelectedGameObject != null &&
                          EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;
        return ChatSystem.IsChatting || dangGoPhim;
    }

    private void HandleUIGlobalInput(bool isChatActive, bool isShopOpen)
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TatToanBoUI();
            if (ESC.instance != null) ESC.instance.BatTatESC();
        }

        if (!isChatActive && !isShopOpen)
        {
            if (Keyboard.current.bKey.wasPressedThisFrame && InventoryManager.instance != null)
            {
                InventoryManager.instance.BatTatBalo(TuiDo, this);
            }

            if (Keyboard.current.tabKey.wasPressedThisFrame && QuestManager.instance != null)
            {
                QuestManager.instance.Battatbangnhiemvu();
            }

            if (Keyboard.current.eKey.wasPressedThisFrame && UI_TramCheTao.instance != null)
            {
                ShopUIController.instance.BatTatCraft();
            }
        }
    }

    private void HandleHotbarInput(bool baloDangMo, bool ishopopen, bool IsChatAct, bool ESCDangMo, bool questDangMo)
    {
        bool dangBam1 = Keyboard.current.digit1Key.wasPressedThisFrame;
        bool dangBam2 = Keyboard.current.digit2Key.wasPressedThisFrame;
        bool dangBam3 = Keyboard.current.digit3Key.wasPressedThisFrame;
        bool dangBam4 = Keyboard.current.digit4Key.wasPressedThisFrame;
        bool dangBam5 = Keyboard.current.digit5Key.wasPressedThisFrame;
        bool dangBam6 = Keyboard.current.digit6Key.wasPressedThisFrame;

        if (baloDangMo && ItemHover.itemID_DangDiChuot != 0)
        {
            if (dangBam1) RPC_GanVaoHotbar(0, ItemHover.itemID_DangDiChuot);
            if (dangBam2) RPC_GanVaoHotbar(1, ItemHover.itemID_DangDiChuot);
            if (dangBam3) RPC_GanVaoHotbar(2, ItemHover.itemID_DangDiChuot);
            if (dangBam4) RPC_GanVaoHotbar(3, ItemHover.itemID_DangDiChuot);
            if (dangBam5) RPC_GanVaoHotbar(4, ItemHover.itemID_DangDiChuot);
            if (dangBam6) RPC_GanVaoHotbar(5, ItemHover.itemID_DangDiChuot);
        }
        else if (!baloDangMo && !ishopopen && !IsChatAct && !ESCDangMo && !questDangMo)
        {
            if (dangBam1) RPC_EquipTool(0);
            if (dangBam2) RPC_EquipTool(1);
            if (dangBam3) RPC_EquipTool(2);
            if (dangBam4) RPC_EquipTool(3);
            if (dangBam5) RPC_EquipTool(4);
            if (dangBam6) RPC_EquipTool(5);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new DuLieuInput();
        if (!HasInputAuthority) return;

        if (isDead)
        {
            input.Set(data);
            return;
        }

        if (KiemTraDangGoPhimChat())
        {
            input.Set(data);
            return;
        }

        data.isJumpPressed = jumpPressedLocal;
        data.isDashPressed = dashPressedLocal;

        bool baloDangMo = (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo);
        bool ESCDangMo = (ESC.instance != null && ESC.instance.isESC_Open);
        bool ishopopen = (ShopUIController.instance != null && ShopUIController.instance.isShopOpen);
        bool IsChat = (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive);
        bool isMapOpen = (MapManager.Instance != null && MapManager.Instance.dangMoMap);
        bool dangCauCa = (currentState != FishState.Idle);

        if (baloDangMo || ESCDangMo || ishopopen || IsChat || isMapOpen || dangCauCa)
        {
            data.moveInput = Vector2.zero;
            data.isJumpPressed = false;
            data.mouseX = 0f;
        }
        else
        {
            Vector3 huongChuanBiGui = Vector3.zero;
            if (cameraTransform != null)
            {
                Vector3 camForward = cameraTransform.forward;
                Vector3 camRight = cameraTransform.right;
                camForward.y = 0; camRight.y = 0;
                camForward.Normalize(); camRight.Normalize();
                huongChuanBiGui = camForward * moveInputLocal.y + camRight * moveInputLocal.x;
            }
            data.moveInput = new Vector2(huongChuanBiGui.x, huongChuanBiGui.z);
            data.isRunfast = dangChayNhanh;
        }

        input.Set(data);
        jumpPressedLocal = false;
        dashPressedLocal = false;
        mouseXLocalAcc = 0f;
    }

    public void OnMove(InputValue value)
    {
        if (!HasInputAuthority) return;
        moveInputLocal = value.Get<Vector2>();
    }

    #endregion

    #region 4. CƠ CHẾ TƯƠNG TÁC & VŨ KHÍ (INTERACTION & TOOLS)

    private void HandleGameplayInteraction(int idDangCam)
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            // Bỏ qua layer mask để quét toàn bộ giống hệt hàm nhặt rác (item)
            Collider[] cacVatTheGan = Physics.OverlapSphere(transform.position, banKinhNhat);
            bool daTuongTacXong = false;

            foreach (var col in cacVatTheGan)
            {
                // Kiểm tra balo rơi trước
                DroppedBackpack db = col.GetComponentInParent<DroppedBackpack>();
                if (db != null)
                {
                    db.RPC_YeuCauNhatLaiBalo(this);
                    daTuongTacXong = true;
                    break;
                }

                FarmPlot plot = col.GetComponentInParent<FarmPlot>();
                if (plot != null && plot.CurrentState == FarmPlot.PlotState.CayLon)
                {
                    plot.RPC_ThuHoach(Runner.LocalPlayer);
                    daTuongTacXong = true;
                    break;
                }

                if (col.CompareTag("NPC"))
                {
                    daTuongTacXong = true;
                    break;
                }
            }

            if (!daTuongTacXong)
            {
                RPC_YeuCauNhatRac();
            }
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Bỏ thanh kiếm (id 4) ra khỏi actionDelay chung để hệ thống combo hoạt động mượt mà
            if (idDangCam == 5 || idDangCam == 6)
            {
                if (Time.time - lastActionTime < actionDelay) return;
                lastActionTime = Time.time;
            }

            switch (idDangCam)
            {
                case 4: HandleAttackAnimal(); break;
                case 5: HandleChopping(); break;
                case 6: HandleMining(); break;
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (idDangCam == 8)
            {
                if (currentState == FishState.Idle) 
                    BatDauCauCa();
                else if (currentState == FishState.Waiting || currentState == FishState.Casting) 
                    ThuCanCau("Hủy câu!");
                else if (currentState == FishState.Giatca) 
                    ThanhCongGiatCa();
            }
            else if (idDangCam == 10)
            {
                HandleFarmingPlantLogic();
            }
            else if (idDangCam > 0 && !dangSuDungVatPham)
            {
                Item thongTinItem = InventoryManager.instance.TraCuuItem(idDangCam);
                if (thongTinItem != null && thongTinItem.loaiTieuHao != Item.LoaiTieuHao.KhongPhai)
                {
                    tienTrinhDungItem = StartCoroutine(TienTrinhSuDungItem(thongTinItem));
                }
            }
        }
    }

    private void HandleAttackAnimal()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        // Bị khóa không cho chém vì đang làm hành động khác hoặc đang trong thời gian nghỉ 1s sau combo 3 hit
        if (isDoingAction || Time.time < comboCooldownEndTime) return;

        // Bỏ qua nếu người chơi bấm quá nhanh (chưa hết thời gian hồi đòn)
        if (lastAttackTime != 0f && Time.time - lastAttackTime < minAttackCooldown) return;

        if (Time.time - lastAttackTime > comboResetTime)
        {
            currentComboStep = 0;
        }

        currentComboStep++;

        if (currentComboStep > 3)
        {
            currentComboStep = 1;
        }

        lastAttackTime = Time.time;
        RPC_AnimSlash(currentComboStep);

        // Kích hoạt trạng thái khóa di chuyển (cục bộ ngay lập tức + báo lên server)
        thoiDiemHetKhoaCucBo = Time.time + slashLockDuration;
        RPC_BaoHieuBatDauAction(3, slashLockDuration, 0f);

        // Gọi hàm gây sát thương NGAY LẬP TỨC để fix lỗi không có dame (bỏ qua Animation Event)
        PlayerDoDamage();

        // Thiết lập thời gian nghỉ 1 giây sau khi chém hit 3
        if (currentComboStep == 3)
        {
            comboCooldownEndTime = Time.time + comboFinishCooldown;
        }
    }

    public void PlayerDoDamage()
    {
        if (!HasInputAuthority) return;

        Vector3 tamQuet = transform.position + transform.forward * 1f;
        float banKinhChem = 3f;
        Collider[] hitColliders = Physics.OverlapSphere(tamQuet, banKinhChem, attackLayer);

        // Tính toán sát thương theo combo (Hit 1: x1, Hit 2: x1.2, Hit 3: x1.5)
        float heSoCombo = 1f;
        if (currentComboStep == 2) heSoCombo = 1.2f;
        else if (currentComboStep == 3) heSoCombo = 1.5f;

        float satThuongThucTe = attackDamageToAnimal * heSoCombo;

        foreach (var hitCollider in hitColliders)
        {
            var animalAI = hitCollider.GetComponentInParent<ithappy.Animals_FREE.AnimalAI_Controller>();
            if (animalAI != null)
            {
                animalAI.RPC_AnimalTakeDamage(satThuongThucTe, Runner.LocalPlayer);
            }

            var enemyOrc = hitCollider.GetComponentInParent<EnemyAIOrc>();
            if (enemyOrc != null)
            {
                enemyOrc.RPC_TakeDamageFromPlayer((int)satThuongThucTe);
            }

            // --- ĐOẠN CODE MỚI THÊM ĐỂ ĐÁNH TRÚNG BOSS ---
            var boss = hitCollider.GetComponentInParent<BossController>();
            if (boss != null)
            {
                boss.RPC_PlayerHitBoss(satThuongThucTe);
            }
        }
    }

    private void HandleChopping()
    {
        if (playerCamera == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        thoiDiemHetKhoaCucBo = Time.time + 1.5f;
        RPC_BaoHieuBatDauAction(1, 1.5f, 0.6f);
    }

    private void HandleMining()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        RPC_AnimDapDa(); 
        thoiDiemHetKhoaCucBo = Time.time + 1.5f;
        RPC_BaoHieuBatDauAction(2, 1.5f, 0.6f);
    }

    private void ThucHienXetVaChamChop()
    {
        Vector3 hitboxCenter = transform.position + transform.forward * hitboxOffset;
        bool daChatCayPrefab = false;

        // 1. KIỂM TRA CHẶT CÂY GAMEOBJECT (PREFAB CÓ TREESCRIPT) TRƯỚC
        Collider[] hits = Physics.OverlapSphere(hitboxCenter, hitboxRadius, chopLayer);
        foreach (var col in hits)
        {
            // Dùng GetComponentInParent đề phòng va chạm trúng cái Visual con bên trong
            TreeScript cay = col.GetComponentInParent<TreeScript>();
            if (cay != null)
            {
                // Trừ máu cây bằng lực chém (ở đây lấy tạm attackDamageToAnimal, bạn có thể tạo biến riêng)
                cay.RPC_TakeDamage(attackDamageToAnimal);
                daChatCayPrefab = true;
            }
        }

        // 2. NẾU KHÔNG TRÚNG CÂY PREFAB NÀO, MỚI THỬ CHẶT CÂY TRÊN TERRAIN (Giữ nguyên code cũ của bạn)
        if (!daChatCayPrefab)
        {
            LayerMask maskDung = (chopLayer.value != 0) ? chopLayer : Physics.DefaultRaycastLayers;
            Terrain hitTerrain = null;

            if (Physics.Raycast(hitboxCenter + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, maskDung))
            {
                hitTerrain = hit.collider.GetComponent<Terrain>();
            }

            if (hitTerrain == null) hitTerrain = Terrain.activeTerrain;

            if (hitTerrain != null && TreeManager.Instance != null)
            {
                TreeManager.Instance.TryChopTree(hitTerrain, hitboxCenter, Runner);
            }
        }
    }

    private void ThucHienXetVaChamMine()
    {
        Vector3 hitboxCenter = transform.position + transform.forward * hitboxOffset + Vector3.up * 1f;
        Collider[] hits = Physics.OverlapSphere(hitboxCenter, hitboxRadius, rockLayer);
        
        foreach (var col in hits)
        {
            RockScript cucDa = col.GetComponent<RockScript>();
            if (cucDa != null)
            {
                cucDa.RPC_NhanSatThuongCuoc(25f);
            }
        }
    }

    private bool HandlePickup()
    {
        if (BanTiaTuTamManHinh(interactRange, interactLayer, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Items") && hit.collider.GetComponent<NetworkObject>() is NetworkObject itemNetObj)
            {
                RPC_YeuCauNhatRacTheoID(itemNetObj.Id);
                return true;
            }
        }
        return false;
    }

    private bool HandleNPCInteract()
    {
        if (BanTiaTuTamManHinh(interactRange, interactLayer, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("NPC"))
            {
                Debug.Log("<color=cyan>Đã bấm F! Đang nói chuyện với NPC!</color>");
                return true;
            }
        }
        return false;
    }

    private System.Collections.IEnumerator TienTrinhSuDungItem(Item thongTinItem)
    {
        dangSuDungVatPham = true;
        float thoiGianConLai = thongTinItem.thoiGianDung;

        while (thoiGianConLai > 0)
        {
            thoiGianConLai -= Time.deltaTime;

            if (UI_TienTrinhDung.instance != null)
                UI_TienTrinhDung.instance.CapNhatUI(thoiGianConLai, thongTinItem.thoiGianDung);

            if (Mouse.current.leftButton.wasPressedThisFrame || isSprinting)
            {
                if (UI_TienTrinhDung.instance != null) UI_TienTrinhDung.instance.AnUI();
                dangSuDungVatPham = false;
                yield break;
            }
            yield return null;
        }

        if (UI_TienTrinhDung.instance != null) UI_TienTrinhDung.instance.AnUI();
        dangSuDungVatPham = false;
        RPC_HoanThanhDungVatPham(thongTinItem.itemID);
    }

    #endregion

    #region 5. NÔNG TRẠI & CÂU CÁ (FARMING & FISHING)

    private void UpdateFarmingUI(int idDangCam)
    {
        if (hintText == null) return;
        string chuChuoiUI = "";

        if (BanTiaTuTamManHinh(interactRange, farmlandLayer, out RaycastHit hit))
        {
            currentLookedPlot = hit.collider.GetComponentInParent<FarmPlot>();
            if (currentLookedPlot != null)
            {
                if (currentLookedPlot.CurrentState == FarmPlot.PlotState.DatTrong)
                    chuChuoiUI += (idDangCam == 10) ? "[Chuột Phải] Gieo hạt\n" : "Cần hạt giống\n";
                else if (currentLookedPlot.CurrentState == FarmPlot.PlotState.CayCon)
                    chuChuoiUI += "Cây đang lớn...\n";
            }
        }
        else
        {
            currentLookedPlot = null;
        }

        // Bỏ qua layer mask để quét toàn bộ giống hệt nhặt item
        Collider[] cacVatTheGan = Physics.OverlapSphere(transform.position, banKinhNhat);
        float khoangCachNganNhat = float.MaxValue;
        Collider mucTieuGanNhat = null;

        foreach (var col in cacVatTheGan)
        {
            FarmPlot plot = col.GetComponentInParent<FarmPlot>();
            bool laCayLon = (plot != null && plot.CurrentState == FarmPlot.PlotState.CayLon);
            bool laNPC = col.CompareTag("NPC");
            bool laItem = col.CompareTag("Items");
            bool laBalo = (col.GetComponentInParent<DroppedBackpack>() != null);

            if (laCayLon || laNPC || laItem || laBalo)
            {
                float khoangCach = Vector3.Distance(transform.position, col.transform.position);
                if (khoangCach < khoangCachNganNhat)
                {
                    khoangCachNganNhat = khoangCach;
                    mucTieuGanNhat = col;
                }
            }
        }

        if (mucTieuGanNhat != null)
        {
            FarmPlot plot = mucTieuGanNhat.GetComponentInParent<FarmPlot>();
            if (plot != null && plot.CurrentState == FarmPlot.PlotState.CayLon)
            {
                chuChuoiUI += "[F] Thu hoạch\n";
            }
            else if (mucTieuGanNhat.CompareTag("NPC"))
            {
                chuChuoiUI += "[F] Trò chuyện\n";
            }
            else if (mucTieuGanNhat.GetComponentInParent<DroppedBackpack>() != null)
            {
                chuChuoiUI += "[F] Nhặt lại Balo\n";
            }
            else if (mucTieuGanNhat.CompareTag("Items"))
            {
                XuLyItem theCanCuoc = mucTieuGanNhat.GetComponent<XuLyItem>();
                if (theCanCuoc != null && theCanCuoc.thongTinDoVat != null)
                    chuChuoiUI += $"[F] Nhặt {theCanCuoc.thongTinDoVat.itemName}\n";
                else
                    chuChuoiUI += "[F] Nhặt đồ\n";
            }
        }

        if (idDangCam > 0 && InventoryManager.instance != null)
        {
            Item thongTinItem = InventoryManager.instance.TraCuuItem(idDangCam);
            if (thongTinItem != null && thongTinItem.loaiTieuHao != Item.LoaiTieuHao.KhongPhai)
            {
                chuChuoiUI += $"[Chuột Phải] Dùng {thongTinItem.itemName}";
            }
        }

        hintText.text = chuChuoiUI.TrimEnd('\n');
    }

    private void HandleFarmingPlantLogic()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (currentLookedPlot != null && currentLookedPlot.CurrentState == FarmPlot.PlotState.DatTrong)
            {
                currentLookedPlot.RPC_GieoHat();
                RPC_TruVatPham(10, 1);
            }
        }
    }

    private void BatDauCauCa()
    {
        currentState = FishState.Casting;
        Vector3 diemBatDau = cameraTransform.position + cameraTransform.forward * 1.5f;
        float lucNem = 12f;
        Vector3 huongNem = cameraTransform.forward * lucNem + Vector3.up * 2f;
        RPC_QuangPhaoVatLy(diemBatDau, huongNem);

        // BẢO HIỂM CHỐNG KẸT (Rất quan trọng):
        if (cauCaCoroutine != null) StopCoroutine(cauCaCoroutine);
        cauCaCoroutine = StartCoroutine(BaoHiemPhaoTrenCan());
    }
    private System.Collections.IEnumerator BaoHiemPhaoTrenCan()
    {
        // Đếm ngược 1.5 giây, nếu phao không đụng nước thì tự động ép hủy câu!
        yield return new WaitForSeconds(1.5f);
        if (currentState == FishState.Casting) 
        {
            //Debug.Log("<color=red>🚨 Bảo hiểm kích hoạt: Phao kẹt, ép hủy câu!</color>");
            PhaoRotTrenCan(); 
        }
    }

    public void PhaoDaChamNuoc()
    {
        if (cauCaCoroutine != null) StopCoroutine(cauCaCoroutine); 
        
        currentState = FishState.Waiting;

        // Báo cho Animator biết phao đã chạm nước
        if (animator != null) animator.SetTrigger("PhaoChamNuoc");

        cauCaCoroutine = StartCoroutine(TienTrinhCauCa());
    }

    public void PhaoRotTrenCan()
    {
        //Debug.Log("<color=orange>⚠️ Phao rớt trên cạn! Gọi lệnh ThuCanCau!</color>");
        ThuCanCau("Rớt trên cạn");
    }
    private System.Collections.IEnumerator TienTrinhCauCa()
    {
        float thoiGianCho = Random.Range(3f, 6f);
        yield return new WaitForSeconds(thoiGianCho);

        currentState = FishState.Giatca;
        if (iconCamThan != null) iconCamThan.SetActive(true);

        yield return new WaitForSeconds(1.5f);
        if (currentState == FishState.Giatca) ThuCanCau("<color=red>Trễ quá, cá xơi mồi rồi bơi mất tiêu!</color>");
    }

    private void ThanhCongGiatCa()
    {
        if (cauCaCoroutine != null) StopCoroutine(cauCaCoroutine);
        ThemDoVaoTui(9, 1);
        ThuCanCau("Hoàn tất câu cá, cất cần vào túi!", false);
    }

    private void ThuCanCau(string lyDo, bool laHuy = true)
    {
        currentState = FishState.Idle;
        if (iconCamThan != null) iconCamThan.SetActive(false);
        RPC_ThuPhao(laHuy);
        if (cauCaCoroutine != null) StopCoroutine(cauCaCoroutine);
    }

    #endregion

    #region 6. TÚI ĐỒ & GIAO DIỆN (INVENTORY & UI)

    private int _lastEquippedID = -1;

    private void OnToolChanged()
    {
        if (HasInputAuthority && UI_HotBar.Instance != null)
            UI_HotBar.Instance.HighlightSlot(CurrentToolIndex);

        int idDangCam = (CurrentToolIndex >= 0 && CurrentToolIndex <= 5) ? HotbarIDs[CurrentToolIndex] : 0;
        
        // Tránh load lại model nếu ID giống hệt nhau (chống nhấp nháy)
        if (idDangCam == _lastEquippedID) return;
        _lastEquippedID = idDangCam;

        if (vuKhiDangCamThucTe != null)
        {
            Destroy(vuKhiDangCamThucTe);
            vuKhiDangCamThucTe = null;
        }

        if (CurrentToolIndex < 0 || CurrentToolIndex > 5) return;

        if (idDangCam > 0 && InventoryManager.instance != null)
        {
            Item thongTinItem = InventoryManager.instance.TraCuuItem(idDangCam);

            if (thongTinItem != null && thongTinItem.model3DPrefab != null && viTriCamVuKhi != null)
            {
                // 1. Sinh ra vũ khí và làm con trực tiếp của viTriCamVuKhi[cite: 2]
                vuKhiDangCamThucTe = Instantiate(thongTinItem.model3DPrefab, viTriCamVuKhi);
                
                vuKhiDangCamThucTe.transform.localPosition = thongTinItem.viTriCamOffset;
                vuKhiDangCamThucTe.transform.localRotation = Quaternion.Euler(thongTinItem.gocXoayOffset);
                vuKhiDangCamThucTe.transform.localScale = thongTinItem.scaleTrenTay;
            }
        }
    }

    public bool ThemDoVaoTui(int idCanThem, int soLuongCanThem)
    {
        bool isStackable = true;
        if (InventoryManager.instance != null)
        {
            Item thongTin = InventoryManager.instance.TraCuuItem(idCanThem);
            if (thongTin != null) isStackable = thongTin.stackable;
        }

        if (isStackable)
        {
            for (int i = 0; i < TuiDo.Length; i++)
            {
                if (TuiDo[i].ItemID == idCanThem)
                {
                    O_VatPham doVat = TuiDo[i];
                    doVat.SoLuong += soLuongCanThem;
                    TuiDo.Set(i, doVat);
                    Rpc_NotifyPickupClient(idCanThem, soLuongCanThem);
                    return true;
                }
            }
        }

        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == 0)
            {
                TuiDo.Set(i, new O_VatPham { ItemID = idCanThem, SoLuong = soLuongCanThem });
                Rpc_NotifyPickupClient(idCanThem, soLuongCanThem);
                return true;
            }
        }

        return false;
    }

    public void KiemTraDonDepHotbar()
    {
        for (int i = 0; i < HotbarIDs.Length; i++)
        {
            int idDangGan = HotbarIDs[i];
            if (idDangGan <= 0) continue;

            bool conHangTrongBalo = false;
            for (int j = 0; j < TuiDo.Length; j++)
            {
                if (TuiDo[j].ItemID == idDangGan && TuiDo[j].SoLuong > 0)
                { conHangTrongBalo = true; break; }
            }

            if (!conHangTrongBalo)
            {
                HotbarIDs.Set(i, 0);
                RPC_CapNhatUIHotbarKhach(i, 0);
                if (CurrentToolIndex == i) CurrentToolIndex = -1;
            }
        }
    }

    private void TatToanBoUI()
    {
        if (MapManager.Instance != null && MapManager.Instance.dangMoMap) MapManager.Instance.DongMap();
        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo)
            InventoryManager.instance.BatTatBalo(TuiDo, this);

        if (QuestManager.instance != null && QuestManager.instance.isQuest_Open)
            QuestManager.instance.Battatbangnhiemvu();

        if (ShopUIController.instance != null && ShopUIController.instance.isShopOpen)
        {
            ShopUIController.instance.isShopOpen = false;
            ShopUIController.instance.dangmoshop = false;
            ShopUIController.instance.khungShop.SetActive(false);
        }

        if (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive)
            DialogueEditor.ConversationManager.Instance.EndConversation();
        if (ShopUIController.instance != null && ShopUIController.instance.dangMoCraft)
        {
            ShopUIController.instance.BatTatCraft();
        }
    }

    public void CapNhatUIHotbarLocal(int slotIndex, int itemID)
    {
        if (UI_HotBar.Instance == null) return;
        if (itemID == 0) { UI_HotBar.Instance.CapNhatHinhAnhSlot(slotIndex, null); }
        else if (InventoryManager.instance != null)
        {
            Item thongTinItem = InventoryManager.instance.TraCuuItem(itemID);
            if (thongTinItem != null)
                UI_HotBar.Instance.CapNhatHinhAnhSlot(slotIndex, thongTinItem.icon);
        }

        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo)
        {
            // Delay 0.1s chờ State Sync của HotbarIDs về tới Client rồi mới vẽ lại Balo
            Invoke(nameof(DelayVeBalo), 0.1f);
        }
    }

    private void DelayVeBalo()
    {
        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo)
        {
            InventoryManager.instance.VeBaloRaManHinh(TuiDo);
        }
    }

    #endregion

    #region 7. CHỈ SỐ & SÁT THƯƠNG (STATS & DAMAGE)

    private void XuLyTheLuc()
    {
        // 1. KHÓA HỒI THỂ LỰC KHI ĐANG DASH
        if (isDashing)
        {
            dongHoDelayHoi = thoiGianDelayHoi; // Ép hệ thống phải chờ 2 giây sau khi Dash mới được hồi
            dangChayNhanh = false;
            return; // Thoát luôn, cấm mọi hành động cộng/trừ thể lực khác
        }

        // 2. XỬ LÝ CHẠY NHANH BÌNH THƯỜNG (Giữ nguyên)
        if (sprintPressedLocal && moveInputLocal.magnitude > 0.1f && CurrentStamina > 0)
        {
            dangChayNhanh = true;
            CurrentStamina -= tocDoTut * Time.deltaTime;
            dongHoDelayHoi = thoiGianDelayHoi; // Đang chạy thì tiếp tục làm mới thời gian chờ
        }
        else
        {
            dangChayNhanh = false;
            // Nếu không chạy nhanh, không lướt -> Đếm ngược thời gian delay để hồi
            if (CurrentStamina < MaxStamina)
            {
                if (dongHoDelayHoi > 0) 
                    dongHoDelayHoi -= Time.deltaTime;
                else 
                    CurrentStamina += tocDoHoi * Time.deltaTime;
            }
        }
        
        CurrentStamina = Mathf.Clamp(CurrentStamina, 0, MaxStamina);
    }

    // --- ĐOẠN CODE MỚI THÊM: SERVER NHẬN SÁT THƯƠNG TỪ BOSS ---
    public void Server_TakeDamageFromBoss(float damage)
    {
        if (!Object.HasStateAuthority) return;

        if (damage > 0)
        {
            if (CurrentArmor >= damage) CurrentArmor -= damage;
            else
            {
                float satThuongDu = damage - CurrentArmor;
                CurrentArmor = 0;
                CurrentHealth = Mathf.Clamp(CurrentHealth - satThuongDu, 0, MaxHealth);
            }
        }
    }

    #endregion

    #region 8. HỆ THỐNG GỌI HÀM TỪ XA (RPC)

    public int DemSoLuongVatPham(int itemID)
    {
        int tong = 0;
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == itemID) tong += TuiDo[i].SoLuong;
        }
        return tong;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ThucHienCheTao(int idTaoRa, int slTaoRa, int id1, int sl1, int id2, int sl2, int id3, int sl3, int giaTien)
    {
        if (Gold < giaTien) return;
        if (id1 > 0 && DemSoLuongVatPham(id1) < sl1) return;
        if (id2 > 0 && DemSoLuongVatPham(id2) < sl2) return;
        if (id3 > 0 && DemSoLuongVatPham(id3) < sl3) return;

        Gold -= giaTien;
        if (id1 > 0) TruNguyenLieuCheTao(id1, sl1);
        if (id2 > 0) TruNguyenLieuCheTao(id2, sl2);
        if (id3 > 0) TruNguyenLieuCheTao(id3, sl3);

        KiemTraDonDepHotbar();
        ThemDoVatVaoTui(idTaoRa, slTaoRa);

        RPC_BaoClientVeLaiUI();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_BaoClientVeLaiUI()
    {
        LoCamDo.CapNhatToanBoSoLuongTrenTramCheTao();

        if (InventoryManager.instance != null)
        {
            if (InventoryManager.instance.trangThaiBalo)
            {
                InventoryManager.instance.VeBaloRaManHinh(this.TuiDo);
            }
        }
    }

    private void TruNguyenLieuCheTao(int idVatPham, int soLuongCanTru)
    {
        int soLuongDaTru = 0;
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == idVatPham && TuiDo[i].SoLuong > 0)
            {
                var doVat = TuiDo[i];
                int soLuongCoTheTru = Mathf.Min(doVat.SoLuong, soLuongCanTru - soLuongDaTru);
                doVat.SoLuong -= soLuongCoTheTru;
                soLuongDaTru += soLuongCoTheTru;
                if (doVat.SoLuong <= 0) doVat.ItemID = 0;
                TuiDo.Set(i, doVat);
                if (soLuongDaTru >= soLuongCanTru) break;
            }
        }
    }

    public void ThemDoVatVaoTui(int idCanThem, int soLuongCanThem)
    {
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == idCanThem)
            {
                O_VatPham doVat = TuiDo[i];
                doVat.SoLuong += soLuongCanThem;
                TuiDo.Set(i, doVat);
                if (Player_QuestManager.localQuest != null) Player_QuestManager.localQuest.KiemTraTienDo();
                return;
            }
        }
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == 0)
            {
                TuiDo.Set(i, new O_VatPham { ItemID = idCanThem, SoLuong = soLuongCanThem });
                if (Player_QuestManager.localQuest != null) Player_QuestManager.localQuest.KiemTraTienDo();
                return;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_TakeDame(float Dame)
    {
        if (isInvincible && Dame > 0) return;
        if (Dame > 0)
        {
            if (CurrentArmor >= Dame) CurrentArmor -= Dame;
            else
            {
                float satThuongDu = Dame - CurrentArmor;
                CurrentArmor = 0;
                CurrentHealth = Mathf.Clamp(CurrentHealth - satThuongDu, 0, MaxHealth);
            }
        }
        else
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth - Dame, 0, MaxHealth);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_CapNhatChiSoTrangBi(int[] idNhungMonDangMac)
    {
        float bonusHealth = DiemMau * 10f;       
        float bonusStamina = DiemTheLuc * 5f;    
        float bonusDamage = DiemSucManh * 2f;    
        float bonusSpeed = DiemNhanhNhen * 0.2f; 
        float bonusArmor = 0f;

        foreach (int id in idNhungMonDangMac)
        {
            if (id > 0 && InventoryManager.instance != null)
            {
                Item thongTin = InventoryManager.instance.TraCuuItem(id);
                if (thongTin != null)
                {
                    bonusHealth += thongTin.congThemMau;
                    bonusStamina += thongTin.congThemStamina;
                    bonusSpeed += thongTin.congThemTocDo;
                    bonusArmor += thongTin.congThemGiap;
                }
            }
        }

        MaxHealth = baseMaxHealth + bonusHealth;
        MaxStamina = baseMaxStamina + bonusStamina;
        MaxArmor = baseMaxArmor + bonusArmor;
        speed = baseSpeed + bonusSpeed;
        attackDamageToAnimal = baseDamage + bonusDamage;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        CurrentStamina = Mathf.Clamp(CurrentStamina, 0, MaxStamina);
        CurrentArmor = Mathf.Clamp(CurrentArmor, 0, MaxArmor);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_CongDiemTiemNang(int loaiChiSo)
    {
        if (AvailablePoints <= 0) return; 
        
        AvailablePoints--;
        
        if (loaiChiSo == 1) DiemSucManh++;
        else if (loaiChiSo == 2) DiemTheLuc++;
        else if (loaiChiSo == 3) DiemNhanhNhen++;
        else if (loaiChiSo == 4) DiemMau++;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_HoanThanhDungVatPham(int idVatPhamXai)
    {
        bool daTruThanhCong = false;
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == idVatPhamXai && TuiDo[i].SoLuong > 0)
            {
                var doVat = TuiDo[i];
                doVat.SoLuong -= 1;
                if (doVat.SoLuong <= 0) doVat.ItemID = 0;
                TuiDo.Set(i, doVat);
                daTruThanhCong = true;
                break;
            }
        }

        if (!daTruThanhCong) return;

        KiemTraDonDepHotbar();

        Item thongTin = InventoryManager.instance.TraCuuItem(idVatPhamXai);
        if (thongTin != null)
        {
            if (thongTin.loaiTieuHao == Item.LoaiTieuHao.SuaGiap)
                CurrentArmor = Mathf.Clamp(CurrentArmor + thongTin.luongHoiPhuc, 0, MaxArmor);
            else if (thongTin.loaiTieuHao == Item.LoaiTieuHao.HoiMau)
                CurrentHealth = Mathf.Clamp(CurrentHealth + thongTin.luongHoiPhuc, 0, MaxHealth);
            else if (thongTin.loaiTieuHao == Item.LoaiTieuHao.HoiTheLuc)
                CurrentStamina = Mathf.Clamp(CurrentStamina + thongTin.luongHoiPhuc, 0, MaxStamina);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_AddExp(float exp)
    {
        ExpCurrent += exp;
        while (ExpCurrent >= expToLevelUp)
        {
            ExpCurrent -= expToLevelUp; 
            level++;
            AvailablePoints += 3;       
            expToLevelUp *= 1.1f;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_YeuCauNhatRac()
    {
        Collider[] ketQuaQuet = Physics.OverlapSphere(transform.position, banKinhNhat);
        foreach (var Obj in ketQuaQuet)
        {
            if (Obj.CompareTag("Items"))
            {
                NetworkObject nObj = Obj.GetComponent<NetworkObject>();
                XuLyItem theCanCuoc = Obj.GetComponent<XuLyItem>();

                if (nObj != null && nObj.IsValid && theCanCuoc != null && theCanCuoc.thongTinDoVat != null)
                {
                    int idThucTe = theCanCuoc.thongTinDoVat.itemID;
                    bool daNhat = ThemDoVaoTui(idThucTe, 1);
                    if (daNhat)
                    {
                        RPC_XoaRacKhapBanDo(nObj);
                        break;
                    }
                }
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_YeuCauNhatRacTheoID(NetworkId itemId)
    {
        NetworkObject nObj = Runner.FindObject(itemId);
        if (nObj == null || !nObj.IsValid) return;

        XuLyItem theCanCuoc = nObj.GetComponent<XuLyItem>();
        if (theCanCuoc == null || theCanCuoc.thongTinDoVat == null) return;

        int idThucTe = theCanCuoc.thongTinDoVat.itemID;
        bool daNhat = ThemDoVaoTui(idThucTe, 1);
        if (daNhat) RPC_XoaRacKhapBanDo(nObj);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_NotifyPickupClient(int itemID_ServerGui, int soLuong_ServerGui)
    {
        if (Player_QuestManager.localQuest != null) Player_QuestManager.localQuest.KiemTraTienDo();

        if (InventoryManager.instance == null) return;
        Item thongTinItem = InventoryManager.instance.TraCuuItem(itemID_ServerGui);
        if (thongTinItem == null || ItemNotifyManager.Instance == null) return;

        ItemNotifyManager.Instance.ShowNotify(thongTinItem.itemName, soLuong_ServerGui, thongTinItem.icon);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_XoaRacKhapBanDo(NetworkObject rac)
    {
        if (rac != null && rac.IsValid)
        {
            rac.gameObject.SetActive(false);
            if (rac.HasStateAuthority) Runner.Despawn(rac);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AnRacTrenMoiMay(NetworkObject rac)
    {
        if (rac != null)
        {
            rac.gameObject.SetActive(false);
            if (HasStateAuthority) Runner.Despawn(rac);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_TruVatPham(int idVatPham, int soLuongCanTru)
    {
        int soLuongDaTru = 0;
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == idVatPham && TuiDo[i].SoLuong > 0)
            {
                var doVat = TuiDo[i];
                int soLuongCoTheTru = Mathf.Min(doVat.SoLuong, soLuongCanTru - soLuongDaTru);
                doVat.SoLuong -= soLuongCoTheTru;
                soLuongDaTru += soLuongCoTheTru;

                if (doVat.SoLuong <= 0) doVat.ItemID = 0;
                TuiDo.Set(i, doVat);
                if (soLuongDaTru >= soLuongCanTru) break;
            }
        }
        KiemTraDonDepHotbar();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_BanVatPham(int idBan, int giaBan)
    {
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == idBan && TuiDo[i].SoLuong > 0)
            {
                var doVat = TuiDo[i];
                doVat.SoLuong--;
                if (doVat.SoLuong <= 0) doVat.ItemID = 0;
                TuiDo.Set(i, doVat);
                Gold += giaBan;
                KiemTraDonDepHotbar();
                return;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_MuaVatPham(int idMatHang, int giaTien)
    {
        if (Gold < giaTien || InventoryManager.instance == null) return;
        Item thongTin = InventoryManager.instance.TraCuuItem(idMatHang);
        if (thongTin == null) return;

        bool daNhetVaoTui = false;
        if (thongTin.stackable)
        {
            for (int i = 0; i < TuiDo.Length; i++)
            {
                if (TuiDo[i].ItemID == idMatHang)
                {
                    O_VatPham doVat = TuiDo[i];
                    doVat.SoLuong++;
                    TuiDo.Set(i, doVat);
                    daNhetVaoTui = true; break;
                }
            }
        }

        if (!daNhetVaoTui)
        {
            for (int i = 0; i < TuiDo.Length; i++)
            {
                if (TuiDo[i].ItemID == 0)
                {
                    TuiDo.Set(i, new O_VatPham { ItemID = idMatHang, SoLuong = 1 });
                    daNhetVaoTui = true; break;
                }
            }
        }

        if (daNhetVaoTui) Gold -= giaTien;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ThayDoiTien(int soTien) { Gold = Mathf.Max(0, Gold + soTien); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ThayDoiGem(int soGem) { Gem = Mathf.Max(0, Gem + soGem); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_HoanThanhQuest(int idVatPham, int soLuongCanTru, int tienThuong, int gemThuong = 0, int idVatPhamThuong = 0, int soLuongVatPhamThuong = 1)
    {
        if (idVatPham > 0 && soLuongCanTru > 0)
        {
            int soLuongDaTru = 0;
            for (int i = 0; i < TuiDo.Length; i++)
            {
                if (TuiDo[i].ItemID == idVatPham && TuiDo[i].SoLuong > 0)
                {
                    var doVat = TuiDo[i];
                    int soLuongCoTheTru = Mathf.Min(doVat.SoLuong, soLuongCanTru - soLuongDaTru);
                    doVat.SoLuong -= soLuongCoTheTru;
                    soLuongDaTru += soLuongCoTheTru;
                    if (doVat.SoLuong <= 0) doVat.ItemID = 0;
                    TuiDo.Set(i, doVat);
                    if (soLuongDaTru >= soLuongCanTru) break;
                }
            }
        }

        if (tienThuong > 0) Gold += tienThuong;
        if (gemThuong > 0) Gem += gemThuong;
        if (idVatPhamThuong > 0 && soLuongVatPhamThuong > 0)
        {
            ThemDoVatVaoTui(idVatPhamThuong, soLuongVatPhamThuong);
        }

        KiemTraDonDepHotbar();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_EquipTool(int toolIndex)
    {
        CurrentToolIndex = (CurrentToolIndex == toolIndex) ? -1 : toolIndex;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_GanVaoHotbar(int slotIndex, int itemID)
    {
        for (int i = 0; i < HotbarIDs.Length; i++)
        {
            if (HotbarIDs[i] == itemID && i != slotIndex)
            {
                HotbarIDs.Set(i, 0);
                RPC_CapNhatUIHotbarKhach(i, 0);
            }
        }
        HotbarIDs.Set(slotIndex, itemID);
        RPC_CapNhatUIHotbarKhach(slotIndex, itemID);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_CapNhatUIHotbarKhach(int slotIndex, int itemID)
    {
        CapNhatUIHotbarLocal(slotIndex, itemID);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_BaoHieuBatDauAction(int actionType, float totalAnimTime, float timeToHit)
    {
        isDoingAction = true;
        pendingActionType = actionType;
        actionTimer = TickTimer.CreateFromSeconds(Runner, totalAnimTime);
        if (timeToHit > 0)
        {
            hitTimer = TickTimer.CreateFromSeconds(Runner, timeToHit);
        }
        else
        {
            hitTimer = TickTimer.None;
        }

        if (actionType == 1) RPC_AnimChatCay();
        else if (actionType == 2) RPC_AnimDapDa();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimChatCay() { if (animator != null) animator.SetTrigger("Chatcay"); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimDapDa() { if (animator != null) animator.SetTrigger("dapda"); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimSlash(int comboStep) 
    { 
        if (animator != null) 
        {
            animator.SetInteger("ComboStep", comboStep);
            animator.SetTrigger("slash"); 
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimDash() { if (animator != null) animator.SetTrigger("dash"); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_QuangPhaoVatLy(Vector3 diemBatDau, Vector3 huongNem)
    {
        if (animator != null) 
        {
            animator.ResetTrigger("GiatCan"); 
            animator.ResetTrigger("HuyCau");  
            animator.ResetTrigger("PhaoChamNuoc"); // Dọn dẹp trigger cũ
            animator.SetTrigger("QuangCan"); 
        }
        
        if (currentphaocauca != null) Destroy(currentphaocauca);
        if (Phaocauca != null)
        {
            currentphaocauca = Instantiate(Phaocauca, diemBatDau, Quaternion.identity);
            Rigidbody rb = currentphaocauca.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(huongNem, ForceMode.Impulse);

            PhaoCauCa_Logic logic = currentphaocauca.GetComponent<PhaoCauCa_Logic>();
            if (logic == null) logic = currentphaocauca.AddComponent<PhaoCauCa_Logic>();
            logic.chuSohuu = this;
            logic.isLocal = HasInputAuthority;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_ThuPhao(bool laHuy)
    {
        if (animator != null)
        {
            animator.ResetTrigger("QuangCan"); 
            animator.ResetTrigger("PhaoChamNuoc"); 
        }

        if (currentphaocauca != null) Destroy(currentphaocauca);
        
        if (laHuy) 
        {
            //Debug.Log("<color=yellow>⚡ Đã bắn Trigger HuyCau vào Animator!</color>");
            if (animator != null) animator.SetTrigger("HuyCau");
        }
        else 
        {
            if (animator != null) animator.SetTrigger("GiatCan");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_HienThiPhao(Vector3 viTriMatNuoc)
    {
        if (currentphaocauca != null) Destroy(currentphaocauca);
        if (Phaocauca != null) currentphaocauca = Instantiate(Phaocauca, viTriMatNuoc, Quaternion.identity);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_XinPhepDichChuyen(Vector3 toaDoMoi)
    {
        if (character != null) character.Teleport(toaDoMoi);
    }

    public void ThucHienDichChuyen(Vector3 toaDoMoi)
    {
        if (Object.HasStateAuthority) { if (character != null) character.Teleport(toaDoMoi); }
        else RPC_XinPhepDichChuyen(toaDoMoi);
    }

    public void Click_NutBanGo()
    {
        Player_Controller myPlayer = NetworkRunner.Instances[0].GetPlayerObject(NetworkRunner.Instances[0].LocalPlayer).GetComponent<Player_Controller>();
        if (myPlayer != null) myPlayer.RPC_BanVatPham(1, 10);
    }

    #endregion

    #region 9. TIỆN ÍCH CHUNG & INTERFACE RỖNG (DEBUG & CALLBACKS)

    private bool BanTiaTuTamManHinh(float khoangCach, LayerMask layerDich, out RaycastHit hit)
    {
        hit = default;
        if (playerCamera == null) return false;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        debugRayOrigin = ray.origin;
        debugRayDirection = ray.direction;
        debugRayDistance = khoangCach;

        LayerMask maskCuoi = (layerDich.value != 0) ? layerDich : Physics.DefaultRaycastLayers;
        didRayHit = Physics.Raycast(ray, out hit, khoangCach, maskCuoi);
        if (didRayHit) rayHitPoint = hit.point;

        return didRayHit;
    }

    void OnDrawGizmos()
    {
        if (!showDebugRay) return;
        Gizmos.color = didRayHit ? Color.green : Color.blue;
        Gizmos.DrawRay(debugRayOrigin, debugRayDirection * debugRayDistance);
        if (didRayHit)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(rayHitPoint, 0.2f);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, Fusion.Sockets.NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, Fusion.Sockets.NetAddress remoteAddress, Fusion.Sockets.NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, Fusion.Sockets.ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, Fusion.Sockets.ReliableKey key, float progress) { }

    #endregion

    private void DropBackpackOnDeath()
    {
        if (!Object.HasStateAuthority) return;

        if (droppedBackpackPrefab == null)
        {
            Debug.LogWarning("Chưa gán droppedBackpackPrefab trong Player_Controller!");
            return;
        }

        bool coDo = false;
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID != 0 && TuiDo[i].SoLuong > 0)
            {
                coDo = true;
                break;
            }
        }

        if (!coDo) return;

        // Xóa sạch Hotbar
        for (int i = 0; i < HotbarIDs.Length; i++)
        {
            HotbarIDs.Set(i, 0);
            RPC_CapNhatUIHotbarKhach(i, 0);
        }

        // Cất vũ khí trên tay
        CurrentToolIndex = -1;

        // Yêu cầu Client xóa đồ trong các ô cắm (Equipment Slots)
        RPC_XoaToanBoTrangBiLocal();

        var spawnedBackpack = Runner.Spawn(droppedBackpackPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity, PlayerRef.None);
        if (spawnedBackpack != null)
        {
            DroppedBackpack dbScript = spawnedBackpack.GetComponent<DroppedBackpack>();
            if (dbScript != null)
            {
                for (int i = 0; i < TuiDo.Length; i++)
                {
                    dbScript.VatPhamDaRoi.Set(i, TuiDo[i]);
                }

                for (int i = 0; i < TuiDo.Length; i++)
                {
                    TuiDo.Set(i, new O_VatPham { ItemID = 0, SoLuong = 0 });
                }
            }
        }
    }

    private void HoiSinhNhanVat()
    {
        // Phục hồi chỉ số
        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
        isDead = false;

        // Tìm vị trí SpawnPoint
        Player_Runner runner = FindObjectOfType<Player_Runner>();
        if (runner != null && runner.spawn != null)
        {
            Vector3 diemHoiSinh = runner.spawn.transform.position;
            if (character != null)
            {
                character.Teleport(diemHoiSinh);
            }
            else
            {
                transform.position = diemHoiSinh;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_XoaToanBoTrangBiLocal()
    {
        if (InventoryManager.instance != null)
        {
            if (InventoryManager.instance.slotMu != null) InventoryManager.instance.slotMu.XoaDoKhoiO();
            if (InventoryManager.instance.slotAo != null) InventoryManager.instance.slotAo.XoaDoKhoiO();
            if (InventoryManager.instance.slotQuan != null) InventoryManager.instance.slotQuan.XoaDoKhoiO();
            if (InventoryManager.instance.slotVuKhi != null) InventoryManager.instance.slotVuKhi.XoaDoKhoiO();
            if (InventoryManager.instance.slotDayChuyen != null) InventoryManager.instance.slotDayChuyen.XoaDoKhoiO();
            if (InventoryManager.instance.slotGiay != null) InventoryManager.instance.slotGiay.XoaDoKhoiO();
            if (InventoryManager.instance.slotNhan != null) InventoryManager.instance.slotNhan.XoaDoKhoiO();
            
            InventoryManager.instance.CapNhatLaiToanBoChiSo();
        }
    }
}