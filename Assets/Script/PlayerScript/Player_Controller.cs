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
    public NetworkBool isUsingItem;
}

public struct O_VatPham : INetworkStruct
{
    public int ItemID;
    public int SoLuong;
    public int UpgradeLevel;
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
    public float thoiGianVoDich = 0.6f;
    [Networked] public NetworkBool isDashing { get; set; }
    [Networked] public NetworkBool isInvincible { get; set; }
    [Networked] public TickTimer dongHoDash { get; set; }
    [Networked] public TickTimer dongHoHoiDash { get; set; }
    [Networked] public TickTimer dongHoVoDich { get; set; }
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
    public float baseDamage = 0f; // Đã đổi về 0 để phụ thuộc vào vũ khí

    [Networked] public float CurrentHealth { get; set; }
    [Networked] public float MaxHealth { get; set; }
    [Networked] public float CurrentStamina { get; set; }
    [Networked] public float MaxStamina { get; set; }
    [Networked] public float CurrentArmor { get; set; }
    [Networked] public float MaxArmor { get; set; }
    public float tocDoTut = 20f;
    public float tocDoHoi = 15f;

    [Header("Hệ Thống Kinh Nghiệm & Cấp Độ")]
    [Networked] public float ExpCurrent { get; set; }
    [Networked] public int level { get; set; }
    [Networked] public float expToLevelUp { get; set; }

    [Header("Hệ Thống Phân Bổ Điểm")]
    [Networked] public int AvailablePoints { get; set; }
    [Networked] public int DiemSucManh { get; set; }
    [Networked] public int DiemTheLuc { get; set; }
    [Networked] public int DiemNhanhNhen { get; set; }
    [Networked] public int DiemMau { get; set; }

    [HideInInspector] public bool dangChayNhanh = false;
    public float thoiGianDelayHoi = 2f;
    private float dongHoDelayHoi = 0f;

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
    public float khoangCachMin = 1.5f;
    public float khoangCachMax = 10f;
    public float tocDoZoomChuot = 0.5f;
    private float khoangCachMucTieu;
    public LayerMask layerVaChamCamera;
    public float fovBinhThuong = 60f;
    public float fovChayNhanh = 75f;
    public float tocDoZoom = 5f;

    [Header("Camera Ngắm Bắn (Over-The-Shoulder)")]
    [Tooltip("Độ lệch vị trí camera khi ngắm bắn: X = Sang phải, Y = Nâng cao, Z = Tiến/lùi")]
    public Vector3 aimCameraOffset = new Vector3(0.65f, 0.1f, 0f);
    public float aimCameraDistance = 2.2f;
    public float tocDoChuyenGocAim = 8f;
    private Vector3 currentAimOffsetSmooth = Vector3.zero;

    [Header("Kinh tế & Túi đồ")]
    [Networked] public int Gold { get; set; }
    [Networked] public int Gem { get; set; }
    [Networked, OnChangedRender(nameof(OnTuiDoChanged)), Capacity(20)] public NetworkArray<O_VatPham> TuiDo { get; }

    private void OnTuiDoChanged()
    {
        if (HasInputAuthority && Player_QuestManager.localQuest != null)
        {
            Player_QuestManager.localQuest.KiemTraTienDo();
        }
    }
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
    public Transform viTriCamTayTrai;
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
    public float minAttackCooldown = 0.5f;
    public float slashLockDuration = 0.5f;
    public float comboResetTime = 0.7f;
    public float comboFinishCooldown = 1f;
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
    private float thoiDiemHetKhoaCucBo = 0f;

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

    [Header("Bắn Cung")]
    public GameObject ArrowPrefab;
    public float maxBowTension = 1.5f;
    public float maxShootForce = 40f;
    public float drawDuration = 0.4f;
    private float drawStartTime = 0f;

    public enum BowState { Idle, Drawing, Holding, Shooting }
    public BowState currentBowState = BowState.Idle;
    #endregion

    #region 2. KHỞI TẠO & VÒNG LẶP CHÍNH

    public override void Spawned()
    {
        animator = GetComponent<Animator>();
        CurrentHealth = 100;

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

            khoangCachMucTieu = khoangCachCamera;

            GameObject objHint = GameObject.Find("Text_Hint");
            if (objHint != null) hintText = objHint.GetComponent<TextMeshProUGUI>();

            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            if (cameraTransform != null) cameraTransform.SetParent(null);
            if (playerCamera == null) Debug.LogError("[Player_Controller] ❌ 'Player Camera' chưa được gán!");

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
            level = 0;
            expToLevelUp = 100f;
            ExpCurrent = 0f;

            MaxHealth = baseMaxHealth;
            MaxStamina = baseMaxStamina;
            speed = baseSpeed;
            attackDamageToAnimal = baseDamage;
            MaxArmor = baseMaxArmor;
            CurrentArmor = MaxArmor;

            if (CurrentHealth <= 0) CurrentHealth = MaxHealth;
            if (CurrentStamina <= 0) CurrentStamina = MaxStamina;

            if (expToLevelUp <= 0) expToLevelUp = 100f;
        }
    }

    private System.Collections.IEnumerator TienTrinhTuDongSave()
    {
        while (true)
        {
            yield return new WaitForSeconds(180f);
            LuuGameHienTai();
        }
    }

    public void LuuGameHienTai()
    {
        if (!HasInputAuthority || SaveManager.instance == null) return;
        string nameToSave = (Runner != null && Runner.SessionInfo.IsValid && !string.IsNullOrEmpty(Runner.SessionInfo.Name))
            ? Runner.SessionInfo.Name : "TheGioi_AutoSave";
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

            if (Input.GetKeyDown(KeyCode.L)) RPC_AddExp(20f);

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

            if (batKyUI_NaoDangMo && currentBowState != BowState.Idle)
            {
                currentBowState = BowState.Idle;
                RPC_AnimBowState(0);
            }

            if (batKyUI_NaoDangMo)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (HintUIManager.instance != null) HintUIManager.instance.HideHint();
                if (hintText != null) hintText.text = "";
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                yRotation += Mouse.current.delta.x.ReadValue() * mouseSensitivity;
                xRotation -= Mouse.current.delta.y.ReadValue() * mouseSensitivity;
                xRotation = Mathf.Clamp(xRotation, -60f, 60f);

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
                if (idDangCam != 999 && currentBowState != BowState.Idle)
                {
                    currentBowState = BowState.Idle;
                    RPC_AnimBowState(0);
                }
                HandleGameplayInteraction(idDangCam);
                CapNhatDayCung(idDangCam);
            }

            sprintPressedLocal = Keyboard.current.leftShiftKey.isPressed;
            XuLyTheLuc();

            if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressedLocal = true;
            if (Keyboard.current.leftCtrlKey.wasPressedThisFrame) dashPressedLocal = true;

            float trucX = Keyboard.current.dKey.isPressed ? 1f : (Keyboard.current.aKey.isPressed ? -1f : 0f);
            float trucY = Keyboard.current.wKey.isPressed ? 1f : (Keyboard.current.sKey.isPressed ? -1f : 0f);
            moveInputLocal = new Vector2(trucX, trucY).normalized;

            if (Time.time < thoiDiemHetKhoaCucBo || currentBowState == BowState.Drawing || currentBowState == BowState.Holding)
            {
                moveInputLocal = Vector2.zero;
                jumpPressedLocal = false;
                dashPressedLocal = false;
                sprintPressedLocal = false;
            }
        }
    }

    private float syncTimeTimer = 0f;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        if (HasStateAuthority && (Runner.IsServer || Runner.IsSharedModeMasterClient))
        {
            syncTimeTimer += Runner.DeltaTime;
            if (syncTimeTimer >= 5f)
            {
                syncTimeTimer = 0f;
                if (TimeManager.Instance != null)
                {
                    RPC_SyncGlobalTime(TimeManager.Instance.CurrentTimeInSeconds);
                }
            }
        }

        if (CurrentHealth <= 0 && !isDead)
        {
            if (HasStateAuthority)
            {
                isDead = true;
                dongHoHoiSinh = TickTimer.CreateFromSeconds(Runner, 3f);
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
            if (isInvincible)
            {
                if (dongHoVoDich.Expired(Runner))
                {
                    isInvincible = false;
                }
            }

            if (isDashing)
            {
                if (dongHoDash.Expired(Runner))
                {
                    isDashing = false;
                }
                else
                {
                    Vector3 huongLuoT = new Vector3(data.moveInput.x, 0f, data.moveInput.y);
                    if (huongLuoT.magnitude < 0.1f) huongLuoT = transform.forward;

                    character.maxSpeed = dashSpeed;
                    character.Move(huongLuoT.normalized);

                    Quaternion huongMucTieu = Quaternion.LookRotation(huongLuoT);
                    transform.rotation = Quaternion.Slerp(transform.rotation, huongMucTieu, Runner.DeltaTime * tocDoXoay * 2f);

                    return;
                }
            }

            if (!isDashing && data.isDashPressed && character.Grounded && dongHoHoiDash.ExpiredOrNotRunning(Runner) && CurrentStamina >= theLucTieuHaoDash)
            {
                CurrentStamina -= theLucTieuHaoDash;
                isDashing = true;
                isInvincible = true;
                dongHoDash = TickTimer.CreateFromSeconds(Runner, thoiGianDash);
                dongHoVoDich = TickTimer.CreateFromSeconds(Runner, thoiGianVoDich);
                dongHoHoiDash = TickTimer.CreateFromSeconds(Runner, thoiGianHoiDash);
                RPC_AnimDash();
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

            if (currentBowState == BowState.Drawing || currentBowState == BowState.Holding)
            {
                currentSpeedSmooth = 0f;
                character.maxSpeed = 0f;
                character.Move(Vector3.zero);
                isrun = false;
                isSprinting = false;
                isJumping = false;

                transform.rotation = Quaternion.Euler(0f, data.mouseX, 0f);
                return;
            }

            Vector3 huongDiChuyen = new Vector3(data.moveInput.x, 0f, data.moveInput.y);
            bool dangBampi = huongDiChuyen.magnitude > 0.1f;

            isrun = dangBampi;
            isSprinting = isrun && data.isRunfast;

            float targetSpeed = 0f;
            if (dangBampi) 
            {
                if (data.isUsingItem) targetSpeed = 2f;
                else targetSpeed = isSprinting ? runfast : speed;
            }

            if (dangBampi)
            {
                currentSpeedSmooth = Mathf.Lerp(currentSpeedSmooth, targetSpeed, Runner.DeltaTime * giaToc);
            }
            else
            {
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
                animator.SetBool("isDead", true);
                animator.SetFloat("Speed", 0f);
                animator.SetBool("isJump", false);
            }
            else
            {
                animator.SetBool("isDead", false);
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

                int idDangCam = (CurrentToolIndex >= 0 && CurrentToolIndex <= 5) ? HotbarIDs[CurrentToolIndex] : 0;
                bool isTool = (idDangCam == 4 || idDangCam == 5 || idDangCam == 6 || idDangCam == 8);
                bool isBow = (idDangCam == 999);

                if (isTool && !isJumping)
                {
                    animator.SetBool("isHoldingTool", true);
                    animator.SetBool("isEquipBow", false);
                }
                else if (isBow && !isJumping)
                {
                    animator.SetBool("isEquipBow", true);
                    animator.SetBool("isHoldingTool", false);
                }
                else
                {
                    animator.SetBool("isHoldingTool", false);
                    animator.SetBool("isEquipBow", false);
                }
            }
        }

        if (HasInputAuthority && cameraTransform != null)
        {
            bool isAimingBow = (currentBowState == BowState.Drawing || currentBowState == BowState.Holding);

            float targetDistance = isAimingBow ? Mathf.Min(khoangCachMucTieu, aimCameraDistance) : khoangCachMucTieu;
            khoangCachCamera = Mathf.Lerp(khoangCachCamera, targetDistance, Time.deltaTime * 10f);

            Quaternion camRotationMucTieu = Quaternion.Euler(xRotation, yRotation, 0f);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, camRotationMucTieu, Time.deltaTime * 25f);

            Vector3 targetAimOffset = isAimingBow ? (cameraTransform.right * aimCameraOffset.x + cameraTransform.up * aimCameraOffset.y + cameraTransform.forward * aimCameraOffset.z) : Vector3.zero;
            currentAimOffsetSmooth = Vector3.Lerp(currentAimOffsetSmooth, targetAimOffset, Time.deltaTime * tocDoChuyenGocAim);

            Vector3 diemNhin = transform.position + Vector3.up * 1.5f + currentAimOffsetSmooth;
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
                if (isAimingBow) fovMucTieu = fovBinhThuong - 15f; // Zoom in khi giương/giữ cung

                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fovMucTieu, Time.deltaTime * tocDoZoom);
            }

            if (isAimingBow)
            {
                transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            }
        }
    }

    #endregion

    #region 3. HỆ THỐNG NHẬP LIỆU

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
            data.mouseX = yRotation;
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

            if (currentBowState == BowState.Drawing || currentBowState == BowState.Holding)
            {
                data.moveInput = Vector2.zero;
                data.isJumpPressed = false;
                data.isDashPressed = false;
            }
            else
            {
                data.moveInput = new Vector2(huongChuanBiGui.x, huongChuanBiGui.z);
            }

            data.mouseX = yRotation;
            data.isRunfast = dangChayNhanh;
        }

        data.isUsingItem = dangSuDungVatPham;
        input.Set(data);
        jumpPressedLocal = false;
        dashPressedLocal = false;
    }

    public void OnMove(InputValue value)
    {
        if (!HasInputAuthority) return;
        moveInputLocal = value.Get<Vector2>();
    }

    #endregion

    #region 4. CƠ CHẾ TƯƠNG TÁC & VŨ KHÍ

    private void HandleGameplayInteraction(int idDangCam)
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            Collider[] cacVatTheGan = Physics.OverlapSphere(transform.position, banKinhNhat);
            bool daTuongTacXong = false;

            foreach (var col in cacVatTheGan)
            {
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

        if (idDangCam == 999)
        {
            HandleBowShooting();
        }
    }

    private void HandleBowShooting()
    {
        // 1. Kéo cung (Drawing): Bấm giữ chuột trái
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            currentBowState = BowState.Drawing;
            drawStartTime = Time.time;
            RPC_AnimBowState(1);
        }

        // 2. Giữ cung (Holding): Đang giữ chuột và đã kéo đủ thời gian drawDuration
        if (Mouse.current.leftButton.isPressed && currentBowState == BowState.Drawing)
        {
            if (Time.time - drawStartTime >= drawDuration)
            {
                currentBowState = BowState.Holding;
                RPC_AnimBowState(2);
            }
        }

        // 3. Bắn cung (Shooting): Nhả chuột trái khi đang kéo hoặc giữ cung
        if (Mouse.current.leftButton.wasReleasedThisFrame && (currentBowState == BowState.Drawing || currentBowState == BowState.Holding))
        {
            float holdTime = Time.time - drawStartTime;
            float tension = Mathf.Clamp01(holdTime / maxBowTension);
            float shootForce = Mathf.Lerp(12f, maxShootForce, tension);

            // 1. Tính toán sát thương tối đa (Dame gốc + Dame Sức mạnh + Dame Vũ khí + Cấp độ nâng cấp)
            int idDangCam = (CurrentToolIndex >= 0 && CurrentToolIndex <= 5) ? HotbarIDs[CurrentToolIndex] : 0;
            float satThuongVuKhi = 0f;
            if (idDangCam > 0 && InventoryManager.instance != null)
            {
                Item thongTinItem = InventoryManager.instance.TraCuuItem(idDangCam);
                if (thongTinItem != null)
                {
                    int upgradeLvl = 0;
                    for (int i = 0; i < TuiDo.Length; i++)
                    {
                        if (TuiDo[i].ItemID == idDangCam && TuiDo[i].SoLuong > 0)
                        {
                            upgradeLvl = TuiDo[i].UpgradeLevel;
                            break;
                        }
                    }
                    float multiplier = 1f + (0.1f * upgradeLvl);
                    satThuongVuKhi = thongTinItem.congThemSatThuong * multiplier;
                }
            }

            float satThuongGoc = DiemSucManh * 2f + baseDamage;
            float satThuongMax = satThuongGoc + satThuongVuKhi;
            if (satThuongMax < 1f) satThuongMax = 25f;

            // 2. Tụ lực sát thương từ 1 đến satThuongMax theo tỷ lệ căng dây cung (tension: 0 -> 1)
            float satThuongThucTe = Mathf.Lerp(1f, satThuongMax, tension);

            currentBowState = BowState.Idle;
            RPC_AnimBowState(3);

            // Xác định điểm xuất phát thực tế từ cây cung trên tay nhân vật (không bị trôi theo vị trí camera)
            Vector3 diemBatDau;
            if (vuKhiDangCamThucTe != null)
                diemBatDau = vuKhiDangCamThucTe.transform.position + transform.forward * 0.3f;
            else if (viTriCamTayTrai != null)
                diemBatDau = viTriCamTayTrai.position + transform.forward * 0.3f;
            else
                diemBatDau = transform.position + Vector3.up * 1.3f + transform.forward * 0.5f;

            // Xác định điểm mục tiêu từ tâm màn hình camera
            Vector3 targetPoint;
            Ray ray = (playerCamera != null)
                ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(cameraTransform.position, cameraTransform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, 150f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.gameObject == gameObject)
                    targetPoint = ray.origin + ray.direction * 100f;
                else
                    targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.origin + ray.direction * 100f;
            }

            Vector3 huongBay = (targetPoint - diemBatDau).normalized;
            Vector3 huongBan = huongBay * shootForce + Vector3.up * (tension * 1.5f);

            RPC_BanCungToanSever(diemBatDau, huongBan, satThuongThucTe);
        }

        // 4. Hủy ngắm bắn (Cancel): Nhấn chuột phải
        if (Mouse.current.rightButton.wasPressedThisFrame && (currentBowState == BowState.Drawing || currentBowState == BowState.Holding))
        {
            currentBowState = BowState.Idle;
            RPC_AnimBowState(0);
        }
    }

    private void HandleAttackAnimal()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (isDoingAction || Time.time < comboCooldownEndTime) return;
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

        thoiDiemHetKhoaCucBo = Time.time + slashLockDuration;
        RPC_BaoHieuBatDauAction(3, slashLockDuration, 0f);

        if (currentComboStep == 3)
        {
            comboCooldownEndTime = Time.time + comboFinishCooldown;
        }
    }

    public void PlayerDoDamage()
    {
        if (!HasInputAuthority) return; // Bảo mật: Chỉ máy Client đánh mới tự tính toán Raycast

        int idDangCam = (CurrentToolIndex >= 0 && CurrentToolIndex <= 5) ? HotbarIDs[CurrentToolIndex] : 0;
        float satThuongVuKhi = 0f;
        
        if (idDangCam > 0 && InventoryManager.instance != null)
        {
            Item thongTinItem = InventoryManager.instance.TraCuuItem(idDangCam);
            if (thongTinItem != null)
            {
                int upgradeLvl = 0;
                for (int i = 0; i < TuiDo.Length; i++)
                {
                    if (TuiDo[i].ItemID == idDangCam && TuiDo[i].SoLuong > 0)
                    {
                        upgradeLvl = TuiDo[i].UpgradeLevel;
                        break;
                    }
                }
                float multiplier = 1f + (0.1f * upgradeLvl);
                satThuongVuKhi = thongTinItem.congThemSatThuong * multiplier;
            }
        }

        // Tính tổng sát thương lúc chém
        float satThuongGoc = DiemSucManh * 2f + baseDamage;
        float satThuongTong = satThuongGoc + satThuongVuKhi;

        Vector3 tamQuet = transform.position + transform.forward * 1f;
        float banKinhChem = 3f;
        Collider[] hitColliders = Physics.OverlapSphere(tamQuet, banKinhChem, attackLayer);

        float heSoCombo = 1f;
        if (currentComboStep == 2) heSoCombo = 1.2f;
        else if (currentComboStep == 3) heSoCombo = 1.5f;

        float satThuongThucTe = satThuongTong * heSoCombo;

        foreach (var hitCollider in hitColliders)
        {
            var animalAI = hitCollider.GetComponentInParent<ithappy.Animals_FREE.AnimalAI_Controller>();
            if (animalAI != null)
            {
                animalAI.RPC_AnimalTakeDamage(satThuongThucTe, Runner.LocalPlayer);
                DamagePopup.Create(animalAI.transform.position + Vector3.up * 1.0f, (int)satThuongThucTe, currentComboStep == 3);
            }

            var enemyOrc = hitCollider.GetComponentInParent<EnemyAIOrc>();
            if (enemyOrc != null)
            {
                enemyOrc.RPC_TakeDamageFromPlayer((int)satThuongThucTe);
                DamagePopup.Create(enemyOrc.transform.position + Vector3.up * 1.5f, (int)satThuongThucTe, currentComboStep == 3);
            }

            var boss = hitCollider.GetComponentInParent<BossController>();
            if (boss != null)
            {
                boss.RPC_PlayerHitBoss(satThuongThucTe);
                DamagePopup.Create(boss.transform.position + Vector3.up * 2.5f, (int)satThuongThucTe, currentComboStep == 3);
            }
        }
    }

    private void HandleChopping()
    {
        if (playerCamera == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        thoiDiemHetKhoaCucBo = Time.time + 1.5f;
        RPC_BaoHieuBatDauAction(1, 1.5f, 0f);
    }

    private void HandleMining()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        RPC_AnimDapDa();
        thoiDiemHetKhoaCucBo = Time.time + 1.5f;
        RPC_BaoHieuBatDauAction(2, 1.5f, 0f);
    }

    public void ThucHienXetVaChamChop()
    {
        if (!HasInputAuthority) return;

        Vector3 hitboxCenter = transform.position + transform.forward * hitboxOffset;
        bool daChatCayPrefab = false;

        Collider[] hits = Physics.OverlapSphere(hitboxCenter, hitboxRadius, chopLayer);
        foreach (var col in hits)
        {
            TreeScript cay = col.GetComponentInParent<TreeScript>();
            if (cay != null)
            {
                cay.RPC_TakeDamage(20f);
                daChatCayPrefab = true;
            }
        }

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

    public void ThucHienXetVaChamMine()
    {
        if (!HasInputAuthority) return;

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
        RPC_AnimTuongTac(true);
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
                RPC_AnimTuongTac(false);
                yield break;
            }
            yield return null;
        }

        if (UI_TienTrinhDung.instance != null) UI_TienTrinhDung.instance.AnUI();
        dangSuDungVatPham = false;
        RPC_AnimTuongTac(false);
        RPC_HoanThanhDungVatPham(thongTinItem.itemID);
    }

    #endregion

    #region 5. NÔNG TRẠI & CÂU CÁ

    private void UpdateFarmingUI(int idDangCam)
    {
        string inputKey = "";
        string inputAction = "";

        // 1. Ưu tiên mục tiêu ngắm trúng (Đất trồng)
        if (BanTiaTuTamManHinh(interactRange, farmlandLayer, out RaycastHit hit))
        {
            currentLookedPlot = hit.collider.GetComponentInParent<FarmPlot>();
            if (currentLookedPlot != null)
            {
                if (currentLookedPlot.CurrentState == FarmPlot.PlotState.DatTrong)
                {
                    inputKey = (idDangCam == 10) ? "M2" : "";
                    inputAction = (idDangCam == 10) ? "Sow seeds" : "Need seeds";
                }
                else if (currentLookedPlot.CurrentState == FarmPlot.PlotState.CayCon)
                {
                    inputAction = "Trees are growing...";
                }
            }
        }
        else
        {
            currentLookedPlot = null;
        }

        // 2. Nếu không có ngắm vào đất, thì tìm mục tiêu gần nhất để tương tác
        if (string.IsNullOrEmpty(inputAction))
        {
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
                inputKey = "F";
                FarmPlot plot = mucTieuGanNhat.GetComponentInParent<FarmPlot>();
                if (plot != null && plot.CurrentState == FarmPlot.PlotState.CayLon)
                {
                    inputAction = "Harvest";
                }
                else if (mucTieuGanNhat.CompareTag("NPC"))
                {
                    inputAction = "Interact";
                }
                else if (mucTieuGanNhat.GetComponentInParent<DroppedBackpack>() != null)
                {
                    inputAction = "Pick up the backpack";
                }
                else if (mucTieuGanNhat.CompareTag("Items"))
                {
                    XuLyItem theCanCuoc = mucTieuGanNhat.GetComponent<XuLyItem>();
                    if (theCanCuoc != null && theCanCuoc.thongTinDoVat != null)
                        inputAction = $"Pick up {theCanCuoc.thongTinDoVat.itemName}";
                    else
                        inputAction = "Pick up";
                }
            }
            // 3. Nếu không có gì tương tác, kiểm tra xem có cầm vật phẩm dùng được không
            else if (idDangCam > 0 && InventoryManager.instance != null)
            {
                Item thongTinItem = InventoryManager.instance.TraCuuItem(idDangCam);
                if (thongTinItem != null && thongTinItem.loaiTieuHao != Item.LoaiTieuHao.KhongPhai)
                {
                    inputKey = "M2";
                    inputAction = $"Use {thongTinItem.itemName}";
                }
            }
        }

        // --- GỌI GIAO DIỆN HƯỚNG DẪN MỚI ---
        if (HintUIManager.instance != null)
        {
            if (!string.IsNullOrEmpty(inputAction))
            {
                HintUIManager.instance.ShowHint(inputKey, inputAction);
            }
            else
            {
                HintUIManager.instance.HideHint();
            }
        }
        else
        {
            // Tương thích ngược lỡ chưa kéo script
            if (hintText != null)
            {
                if (!string.IsNullOrEmpty(inputAction))
                    hintText.text = (string.IsNullOrEmpty(inputKey) ? "" : $"[{inputKey}] ") + inputAction;
                else
                    hintText.text = "";
            }
        }
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

        if (cauCaCoroutine != null) StopCoroutine(cauCaCoroutine);
        cauCaCoroutine = StartCoroutine(BaoHiemPhaoTrenCan());
    }

    private System.Collections.IEnumerator BaoHiemPhaoTrenCan()
    {
        yield return new WaitForSeconds(1.5f);
        if (currentState == FishState.Casting)
        {
            PhaoRotTrenCan();
        }
    }

    public void PhaoDaChamNuoc()
    {
        if (cauCaCoroutine != null) StopCoroutine(cauCaCoroutine);

        currentState = FishState.Waiting;
        if (animator != null) animator.SetTrigger("PhaoChamNuoc");

        cauCaCoroutine = StartCoroutine(TienTrinhCauCa());
    }

    public void PhaoRotTrenCan()
    {
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

    #region 6. TÚI ĐỒ & GIAO DIỆN

    private int _lastEquippedID = -1;

    private void OnToolChanged()
    {
        if (HasInputAuthority && UI_HotBar.Instance != null)
            UI_HotBar.Instance.HighlightSlot(CurrentToolIndex);

        int idDangCam = (CurrentToolIndex >= 0 && CurrentToolIndex <= 5) ? HotbarIDs[CurrentToolIndex] : 0;

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

            Transform viTriGhep = (idDangCam == 999 && viTriCamTayTrai != null) ? viTriCamTayTrai : viTriCamVuKhi;

            if (thongTinItem != null && thongTinItem.model3DPrefab != null && viTriGhep != null)
            {
                vuKhiDangCamThucTe = Instantiate(thongTinItem.model3DPrefab, viTriGhep);

                vuKhiDangCamThucTe.transform.localPosition = thongTinItem.viTriCamOffset;
                vuKhiDangCamThucTe.transform.localRotation = Quaternion.Euler(thongTinItem.gocXoayOffset);
                vuKhiDangCamThucTe.transform.localScale = thongTinItem.scaleTrenTay;
            }
        }
    }

    private void CapNhatDayCung(int idDangCam)
    {
        if (idDangCam == 999 && vuKhiDangCamThucTe != null)
        {
            Bow_StringController bowString = vuKhiDangCamThucTe.GetComponent<Bow_StringController>();
            if (bowString != null)
            {
                float tension = 0f;
                if (currentBowState == BowState.Drawing || currentBowState == BowState.Holding)
                {
                    float holdTime = Time.time - drawStartTime;
                    tension = Mathf.Clamp01(holdTime / maxBowTension);
                }
                bowString.SetStringTension(tension, viTriCamVuKhi);
            }
        }
    }

    public bool ThemDoVaoTui(int idCanThem, int soLuongCanThem, int upgradeLevel = 0)
    {
        bool isStackable = true;
        if (InventoryManager.instance != null)
        {
            Item thongTin = InventoryManager.instance.TraCuuItem(idCanThem);
            if (thongTin != null) isStackable = thongTin.stackable;
        }

        if (upgradeLevel > 0) isStackable = false;

        if (isStackable)
        {
            for (int i = 0; i < TuiDo.Length; i++)
            {
                if (TuiDo[i].ItemID == idCanThem && TuiDo[i].UpgradeLevel == upgradeLevel)
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
                TuiDo.Set(i, new O_VatPham { ItemID = idCanThem, SoLuong = soLuongCanThem, UpgradeLevel = upgradeLevel });
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
            ShopUIController.instance.CloseShop();
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

    #region 7. CHỈ SỐ & SÁT THƯƠNG

    private void XuLyTheLuc()
    {
        if (isDashing || currentBowState == BowState.Drawing || currentBowState == BowState.Holding)
        {
            dongHoDelayHoi = thoiGianDelayHoi;
            dangChayNhanh = false;
            return;
        }

        if (sprintPressedLocal && moveInputLocal.magnitude > 0.1f && CurrentStamina > 0)
        {
            dangChayNhanh = true;
            CurrentStamina -= tocDoTut * Time.deltaTime;
            dongHoDelayHoi = thoiGianDelayHoi;
        }
        else
        {
            dangChayNhanh = false;
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

    public void Server_TakeDamageFromBoss(float damage)
    {
        if (!Object.HasStateAuthority) return;
        if (isInvincible && damage > 0) return;

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

    public void Server_AddExp(float exp)
    {
        if (!HasStateAuthority) return;

        ExpCurrent += exp;
        bool daLenCap = false;

        while (ExpCurrent >= expToLevelUp)
        {
            ExpCurrent -= expToLevelUp;
            level++;
            AvailablePoints += 3;
            expToLevelUp *= 1.1f;
            daLenCap = true;
        }

        // Báo cho máy của Player biết là đã lên cấp để load lại UI Nhiệm Vụ
        if (daLenCap)
        {
            RPC_CapNhatNhiemVuLevel();
        }
    }

    // GỌI HÀM NÀY ĐỂ BÁO VỀ CLIENT UPDATE QUEST BẢNG UI
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_CapNhatNhiemVuLevel()
    {
        if (Player_QuestManager.localQuest != null)
        {
            Player_QuestManager.localQuest.KiemTraTienDo();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_AddExp(float exp)
    {
        Server_AddExp(exp);
    }

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

    public void ThemDoVatVaoTui(int idCanThem, int soLuongCanThem, int upgradeLevel = 0)
    {
        bool isStackable = true;
        if (InventoryManager.instance != null)
        {
            Item thongTin = InventoryManager.instance.TraCuuItem(idCanThem);
            if (thongTin != null) isStackable = thongTin.stackable;
        }

        if (upgradeLevel > 0) isStackable = false;

        if (isStackable)
        {
            for (int i = 0; i < TuiDo.Length; i++)
            {
                if (TuiDo[i].ItemID == idCanThem && TuiDo[i].UpgradeLevel == upgradeLevel)
                {
                    O_VatPham doVat = TuiDo[i];
                    doVat.SoLuong += soLuongCanThem;
                    TuiDo.Set(i, doVat);
                    if (Player_QuestManager.localQuest != null) Player_QuestManager.localQuest.KiemTraTienDo();
                    return;
                }
            }
        }
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == 0)
            {
                TuiDo.Set(i, new O_VatPham { ItemID = idCanThem, SoLuong = soLuongCanThem, UpgradeLevel = upgradeLevel });
                if (Player_QuestManager.localQuest != null) Player_QuestManager.localQuest.KiemTraTienDo();
                return;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
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
            RPC_HienDamagePopupKhach((int)Dame);
        }
        else
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth - Dame, 0, MaxHealth);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_HienDamagePopupKhach(int satThuong)
    {
        DamagePopup.Create(transform.position + Vector3.up * 1.8f, satThuong, false, true);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_CapNhatChiSoTrangBi(O_VatPham[] danhSachDoDangMac)
    {
        float bonusHealth = DiemMau * 10f;
        float bonusStamina = DiemTheLuc * 5f;
        float bonusDamage = DiemSucManh * 2f;
        float bonusSpeed = DiemNhanhNhen * 0.2f;
        float bonusArmor = 0f;

        foreach (O_VatPham monDo in danhSachDoDangMac)
        {
            if (monDo.ItemID > 0 && InventoryManager.instance != null)
            {
                Item thongTin = InventoryManager.instance.TraCuuItem(monDo.ItemID);
                if (thongTin != null)
                {
                    // Tăng thêm 10% chỉ số cho mỗi cấp độ nâng cấp
                    float multiplier = 1f + (0.1f * monDo.UpgradeLevel);
                    
                    bonusHealth += thongTin.congThemMau * multiplier;
                    bonusStamina += thongTin.congThemStamina * multiplier;
                    bonusSpeed += thongTin.congThemTocDo * multiplier; // Có thể không nhân speed để tránh lạm phát tốc độ
                    bonusArmor += thongTin.congThemGiap * multiplier;
                    bonusDamage += thongTin.congThemSatThuong * multiplier;
                    
                    // Note: Nếu vũ khí có sát thương, nhưng Item.cs không có trường sát thương riêng (có thể gộp vào congThemGiap hoặc máu tùy logic game, nhưng tạm thời cứ áp dụng hệ số).
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

    public void RPC_NangCapVatPham(int idVatPham, int levelHienTai)
    {
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == idVatPham && TuiDo[i].UpgradeLevel == levelHienTai && TuiDo[i].SoLuong > 0)
            {
                var doVat = TuiDo[i];
                doVat.UpgradeLevel += 1;
                TuiDo.Set(i, doVat);
                break;
            }
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
    public void RPC_BanVatPham(int idBan, int giaBanCua1Cai, int soLuongBan = 1)
    {
        int soLuongConPhaiBan = soLuongBan;
        int tongTienNhanDuoc = 0;

        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == idBan && TuiDo[i].SoLuong > 0)
            {
                var doVat = TuiDo[i];
                int soLuongTru = Mathf.Min(doVat.SoLuong, soLuongConPhaiBan);
                doVat.SoLuong -= soLuongTru;
                soLuongConPhaiBan -= soLuongTru;
                tongTienNhanDuoc += soLuongTru * giaBanCua1Cai;

                if (doVat.SoLuong <= 0) doVat.ItemID = 0;
                TuiDo.Set(i, doVat);

                if (soLuongConPhaiBan <= 0) break;
            }
        }

        if (tongTienNhanDuoc > 0)
        {
            Gold += tongTienNhanDuoc;
            KiemTraDonDepHotbar();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_MuaVatPham(int idMatHang, int giaTienCua1Cai, int soLuongMua = 1)
    {
        int tongTien = giaTienCua1Cai * soLuongMua;
        if (Gold < tongTien || InventoryManager.instance == null) return;
        Item thongTin = InventoryManager.instance.TraCuuItem(idMatHang);
        if (thongTin == null) return;

        int soLuongConPhaiNhet = soLuongMua;

        if (thongTin.stackable)
        {
            for (int i = 0; i < TuiDo.Length; i++)
            {
                if (TuiDo[i].ItemID == idMatHang)
                {
                    O_VatPham doVat = TuiDo[i];
                    doVat.SoLuong += soLuongConPhaiNhet;
                    TuiDo.Set(i, doVat);
                    soLuongConPhaiNhet = 0;
                    break;
                }
            }
        }

        while (soLuongConPhaiNhet > 0)
        {
            bool daTimThayChoTrang = false;
            for (int i = 0; i < TuiDo.Length; i++)
            {
                if (TuiDo[i].ItemID == 0)
                {
                    O_VatPham slotTrong = TuiDo[i];
                    slotTrong.ItemID = idMatHang;

                    if (thongTin.stackable)
                    {
                        slotTrong.SoLuong = soLuongConPhaiNhet;
                        soLuongConPhaiNhet = 0;
                    }
                    else
                    {
                        slotTrong.SoLuong = 1;
                        soLuongConPhaiNhet -= 1;
                    }

                    TuiDo.Set(i, slotTrong);
                    daTimThayChoTrang = true;
                    break;
                }
            }
            if (!daTimThayChoTrang) break; // Hết ô trống
        }

        int soLuongDaNhet = soLuongMua - soLuongConPhaiNhet;
        if (soLuongDaNhet > 0)
        {
            Gold -= (giaTienCua1Cai * soLuongDaNhet);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ThayDoiTien(int soTien) { Gold = Mathf.Max(0, Gold + soTien); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ThayDoiGem(int soGem) { Gem = Mathf.Max(0, Gem + soGem); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_HoanThanhQuest(int idVatPham, int soLuongCanTru, int tienThuong, int gemThuong = 0, int idVatPhamThuong = 0, int soLuongVatPhamThuong = 1, float expThuong = 0f)
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

        if (expThuong > 0)
        {
            Server_AddExp(expThuong);
        }

        KiemTraDonDepHotbar();
        RPC_BaoClientVeLaiUI();
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
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_AnimDash() { if (animator != null) animator.SetTrigger("dash"); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimTuongTac(NetworkBool isTuongTac) { if (animator != null) animator.SetBool("tuongtac", isTuongTac); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_QuangPhaoVatLy(Vector3 diemBatDau, Vector3 huongNem)
    {
        if (animator != null)
        {
            animator.ResetTrigger("GiatCan");
            animator.ResetTrigger("HuyCau");
            animator.ResetTrigger("PhaoChamNuoc");
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
            if (animator != null) animator.SetTrigger("HuyCau");
        }
        else
        {
            if (animator != null) animator.SetTrigger("GiatCan");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimBowState(int stateIndex)
    {
        if (animator == null) return;
        if (stateIndex == 1) // Kéo cung (Drawing)
        {
            animator.SetInteger("BowState", 1);
            animator.SetBool("isDrawingBow", true);
            animator.SetBool("isHoldingBow", false);
        }
        else if (stateIndex == 2) // Giữ cung (Holding)
        {
            animator.SetInteger("BowState", 2);
            animator.SetBool("isDrawingBow", false);
            animator.SetBool("isHoldingBow", true);
        }
        else if (stateIndex == 3) // Bắn cung (Shooting)
        {
            animator.SetInteger("BowState", 3);
            animator.SetBool("isDrawingBow", false);
            animator.SetBool("isHoldingBow", false);
            animator.SetTrigger("ShootBow");
        }
        else // 0: Reset / Cancel
        {
            animator.SetInteger("BowState", 0);
            animator.SetBool("isDrawingBow", false);
            animator.SetBool("isHoldingBow", false);
            animator.ResetTrigger("ShootBow");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_BanCungToanSever(Vector3 diemBatDau, Vector3 huongBan, float satThuongMuiTen)
    {
        if (ArrowPrefab != null)
        {
            GameObject arrow = Instantiate(ArrowPrefab, diemBatDau, Quaternion.LookRotation(huongBan));
            
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(huongBan, ForceMode.Impulse);

            Arrow_Logic logic = arrow.GetComponent<Arrow_Logic>();
            if (logic == null) logic = arrow.AddComponent<Arrow_Logic>();
            
            logic.chuSohuu = this;
            logic.damage = satThuongMuiTen;
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
#pragma warning disable UNT0006
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, Fusion.Sockets.NetDisconnectReason reason) { }
#pragma warning restore UNT0006
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

        for (int i = 0; i < HotbarIDs.Length; i++)
        {
            HotbarIDs.Set(i, 0);
            RPC_CapNhatUIHotbarKhach(i, 0);
        }

        CurrentToolIndex = -1;

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
                    TuiDo.Set(i, new O_VatPham { ItemID = 0, SoLuong = 0, UpgradeLevel = 0 });
                }
            }
        }
    }

    private void HoiSinhNhanVat()
    {
        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
        isDead = false;

        Player_Runner runner = FindFirstObjectByType<Player_Runner>();
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncGlobalTime(float hostTime)
    {
        if (Runner.IsServer || Runner.IsSharedModeMasterClient) return;

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SyncTimeFromHost(hostTime);
        }
    }
    // =========================================================
    //              TRẠNG THÁI DI CHUYỂN CHO AUDIO
    // =========================================================

    public bool DangDiChuyen
    {
        get
        {
            return moveInputLocal.sqrMagnitude > 0.01f;
        }
    }

    public bool DangChay
    {
        get
        {
            return dangChayNhanh && DangDiChuyen;
        }
    }
}
    