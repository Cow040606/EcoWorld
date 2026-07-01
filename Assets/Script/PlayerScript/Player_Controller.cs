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
}

public struct O_VatPham : INetworkStruct
{
    public int ItemID;
    public int SoLuong;
}

public class Player_Controller : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static Player_Controller localPlayer;

    #region KHAI BÁO BIẾN (VARIABLES)

    [Header("Di chuyển")]
    public NetworkCharacterController character;
    public float speed = 5f;
    public float runfast = 15f;
    private Vector2 moveInputLocal;
    private bool sprintPressedLocal;

    [Header("Chỉ số nhân vật")]
    public float CurrentHealth { get; set; }
    public float MaxHealth = 100f;
    public float CurrentStamina { get; set; }
    public float MaxStamina = 100f;
    public float ExpCurrent { get; set; }
    public int level = 0;
    public float expToLevelUp = 100f;

    [Header("Hiệu ứng Câu cá")]
    public GameObject iconCamThan;

    [Header("Camera & Chuột")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.5f;
    private float xRotation = 0f;
    private float yRotation = 0f;
    public float khoangCachCamera = 4f;
    private float mouseXLocalAcc;
    public LayerMask layerVaChamCamera;

    public float fovBinhThuong = 60f;
    public float fovChayNhanh = 75f;
    public float tocDoZoom = 5f;

    [Header("Nhặt vật phẩm")]
    public float banKinhNhat = 5f;

    [Header("Trọng lực & Nhảy")]
    [Networked] public bool isJumping { get; set; }
    private bool jumpPressedLocal;
    public float thoiGianHoiNhay = 1f;
    [Networked] public TickTimer dongHoChoNhay { get; set; }

    [Header("Kinh tế & Túi đồ")]
    [Networked] public int Gold { get; set; }
    [Networked] public int Gem { get; set; }
    [Networked, Capacity(20)] public NetworkArray<O_VatPham> TuiDo { get; }
    [Networked, Capacity(4)] public NetworkArray<int> HotbarIDs { get; }

    [Header("Animation")]
    [Networked] private NetworkBool isrun { get; set; }
    [Networked] private NetworkBool isSprinting { get; set; }
    private Animator animator;

    [Header("Hệ Thống Hiển Thị Công Cụ Tự Động")]
    [Networked, OnChangedRender(nameof(OnToolChanged))] public int CurrentToolIndex { get; set; }
    public Transform viTriCamVuKhi;
    private GameObject vuKhiDangCamThucTe;

    [Header("Tương Tác - Chặt Cây & Nhặt Đồ")]
    public Camera playerCamera;
    public float interactRange = 10f;
    public LayerMask interactLayer;
    public LayerMask chopLayer;

    [Header("Tấn Công Thú")]
    public float attackDamageToAnimal = 25f;

    [Header("Debug - Chặt Cây")]
    public bool showChopDebug = true;
    private Vector3 _lastRayOrigin;
    private Vector3 _lastRayDir;
    private bool _lastRayHit;
    private Vector3 _lastRayHitPoint;

    [Header("Đào Khoáng Sản")]
    public LayerMask rockLayer;

    [Header("Câu cá")]
    public LayerMask waterLayer;
    public GameObject Phaocauca;
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
    public bool showDebugRay = true;
    private Vector3 debugRayOrigin;
    private Vector3 debugRayDirection;
    private float debugRayDistance;
    private bool didRayHit;
    private Vector3 rayHitPoint;

    [Header("Trạng thái Hành Động (Chặt/Đào)")]
    [Networked] public NetworkBool isDoingAction { get; set; }
    [Networked] public TickTimer actionTimer { get; set; }
    [Networked] public TickTimer hitTimer { get; set; }
    [Networked] public int pendingActionType { get; set; } // 1: Chặt cây, 2: Đập đá

    #endregion

    #region KHỞI TẠO (SPAWNED)

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

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (playerCamera == null)
                Debug.LogError("[Player_Controller] ❌ 'Player Camera' chưa được gán trong Inspector!");

            if (TreeManager.Instance == null)
                Debug.LogError("[Player_Controller] ❌ Không tìm thấy TreeManager trong scene!");
        }
        else
        {
            if (character != null) character.enabled = true;

            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = false;

            AudioListener listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    #endregion

    #region THU THẬP ĐẦU VÀO CỤC BỘ (UPDATE & ON INPUT)

    void Update()
    {
        if (HasInputAuthority && Keyboard.current != null && Mouse.current != null)
        {
            int idDangCam = (CurrentToolIndex >= 0) ? HotbarIDs[CurrentToolIndex] : 0;
            RPC_AddExp(0.1f);
            bool dangGoPhim = EventSystem.current != null &&
                              EventSystem.current.currentSelectedGameObject != null &&
                              EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;

            if (ChatSystem.IsChatting || dangGoPhim) return;

            bool baloDangMo = (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo);
            bool ishopopen = (ShopUIController.instance != null && ShopUIController.instance.isShopOpen);
            bool questDangMo = (QuestManager.instance != null && QuestManager.instance.isQuest_Open);
            bool IsChatAct = (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive);
            bool ESCDangMo = (ESC.instance != null && ESC.instance.isESC_Open);
            bool ismapOpen = (MapManager.Instance != null && MapManager.Instance.dangMoMap);

            if (baloDangMo || ESCDangMo || ishopopen || IsChatAct || questDangMo || ismapOpen)
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

                UpdateFarmingUI(idDangCam);
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TatToanBoUI();
                if (ESC.instance != null) ESC.instance.BatTatESC();
            }

            if (Keyboard.current.cKey.wasPressedThisFrame) RPC_TakeDame(10);
            if (Keyboard.current.vKey.wasPressedThisFrame) RPC_TakeDame(-10);

            if (!IsChatAct && !ishopopen)
            {
                if (Keyboard.current.bKey.wasPressedThisFrame)
                {
                    if (InventoryManager.instance != null)
                    {
                        InventoryManager.instance.BatTatBalo(TuiDo, this);
                    }
                }

                if (Keyboard.current.tabKey.wasPressedThisFrame && QuestManager.instance != null)
                    QuestManager.instance.Battatbangnhiemvu();
            }

            bool dangBam1 = Keyboard.current.digit1Key.wasPressedThisFrame;
            bool dangBam2 = Keyboard.current.digit2Key.wasPressedThisFrame;
            bool dangBam3 = Keyboard.current.digit3Key.wasPressedThisFrame;
            bool dangBam4 = Keyboard.current.digit4Key.wasPressedThisFrame;

            if (baloDangMo && ItemHover.itemID_DangDiChuot != 0)
            {
                if (dangBam1) RPC_GanVaoHotbar(0, ItemHover.itemID_DangDiChuot);
                if (dangBam2) RPC_GanVaoHotbar(1, ItemHover.itemID_DangDiChuot);
                if (dangBam3) RPC_GanVaoHotbar(2, ItemHover.itemID_DangDiChuot);
                if (dangBam4) RPC_GanVaoHotbar(3, ItemHover.itemID_DangDiChuot);
            }
            else if (!baloDangMo && !ishopopen && !IsChatAct && !ESCDangMo && !questDangMo)
            {
                if (dangBam1) RPC_EquipTool(0);
                if (dangBam2) RPC_EquipTool(1);
                if (dangBam3) RPC_EquipTool(2);
                if (dangBam4) RPC_EquipTool(3);
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (currentLookedPlot != null && currentLookedPlot.CurrentState == FarmPlot.PlotState.CayLon)
                {
                    currentLookedPlot.RPC_ThuHoach(Runner.LocalPlayer);
                }
                else
                {
                    bool daNhatBangTia = HandlePickup();
                    if (!daNhatBangTia)
                    {
                        RPC_YeuCauNhatRac();
                    }
                }
            }

            sprintPressedLocal = Keyboard.current.leftShiftKey.isPressed;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressedLocal = true;
            float trucX = Keyboard.current.dKey.isPressed ? 1f : (Keyboard.current.aKey.isPressed ? -1f : 0f);
            float trucY = Keyboard.current.wKey.isPressed ? 1f : (Keyboard.current.sKey.isPressed ? -1f : 0f);
            moveInputLocal = new Vector2(trucX, trucY).normalized;

            if (Keyboard.current.kKey.wasPressedThisFrame) RPC_ThayDoiTien(5);
            if (Keyboard.current.lKey.wasPressedThisFrame) RPC_ThayDoiTien(-5);

            if (!baloDangMo && !ESCDangMo && !ishopopen && !IsChatAct && !questDangMo)
            {
                if (currentState != FishState.Idle)
                {
                    if (idDangCam != 8)
                    {
                        ThuCanCau("<color=orange>Đã cất cần câu, tự động thu mồi!</color>");
                    }
                    else if (currentphaocauca != null)
                    {
                        float khoangCach = Vector3.Distance(transform.position, currentphaocauca.transform.position);
                        if (khoangCach > khoangCachDutDay)
                        {
                            ThuCanCau("<color=red>Đi xa quá đứt dây cước rồi!</color>");
                        }
                    }
                }

                switch (idDangCam)
                {
                    case 4:
                        HandleAttackAnimal();
                        break;
                    case 5:
                        HandleChopping();
                        break;
                    case 6:
                        HandleMining();
                        break;
                    case 8:
                        if (Mouse.current.rightButton.wasPressedThisFrame)
                        {
                            if (currentState == FishState.Idle) BatDauCauCa();
                            else if (currentState == FishState.Waiting) ThuCanCau("<color=orange>Kéo cần sớm quá, cá hoảng sợ chạy mất!</color>");
                            else if (currentState == FishState.Giatca) ThanhCongGiatCa();
                        }
                        break;
                    case 10:
                        HandleFarmingPlantLogic();
                        break;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (HasInputAuthority && cameraTransform != null)
        {
            Quaternion camRotation = Quaternion.Euler(xRotation, yRotation, 0f);
            Vector3 diemNhin = transform.position + Vector3.up * 1.5f;
            Vector3 huongCamera = -(camRotation * Vector3.forward);
            Vector3 viTriDuKien = diemNhin + huongCamera * khoangCachCamera;

            if (Physics.Raycast(diemNhin, huongCamera, out RaycastHit hit, khoangCachCamera, layerVaChamCamera))
                cameraTransform.position = hit.point + hit.normal * 0.1f;
            else
                cameraTransform.position = viTriDuKien;

            cameraTransform.rotation = camRotation;

            if (playerCamera != null)
            {
                float fovMucTieu = isSprinting ? fovChayNhanh : fovBinhThuong;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fovMucTieu, Runner.DeltaTime * tocDoZoom);
            }
        }
    }

    // =========================================================================
    // HÀM LOGIC NÔNG TRẠI GHÉP VÀO
    // =========================================================================
    private void UpdateFarmingUI(int idDangCam)
    {
        if (BanTiaTuTamManHinh(interactRange, farmlandLayer, out RaycastHit hit))
        {
            currentLookedPlot = hit.collider.GetComponentInParent<FarmPlot>();

            if (currentLookedPlot != null && hintText != null)
            {
                hintText.transform.position = currentLookedPlot.transform.position + (Vector3.up * 1.5f);
                if (cameraTransform != null)
                {
                    hintText.transform.rotation = Quaternion.LookRotation(hintText.transform.position - cameraTransform.position);
                }

                switch (currentLookedPlot.CurrentState)
                {
                    case FarmPlot.PlotState.DatTrong:
                        if (idDangCam == 10) hintText.text = "[Chuột Phải] Gieo hạt";
                        else hintText.text = "Cầm hạt giống (ID: 10) để trồng!";
                        break;
                    case FarmPlot.PlotState.CayCon: hintText.text = "Cây đang lớn..."; break;
                    case FarmPlot.PlotState.CayLon: hintText.text = "[E] Thu hoạch"; break;
                }
            }
        }
        else
        {
            currentLookedPlot = null;
            if (hintText != null) hintText.text = "";
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
    // =========================================================================

    private void HandleChopping()
    {
        if (playerCamera == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        // Báo hiệu khóa di chuyển, chạy anim 1.5s, tính va chạm ở giây 0.6
        RPC_BaoHieuBatDauAction(1, 1.5f, 0.6f);
    }

    private void HandleMining()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        // Báo hiệu khóa di chuyển, chạy anim 1.5s, tính va chạm ở giây 0.6
        RPC_BaoHieuBatDauAction(2, 1.5f, 0.6f);
    }

    private void ThucHienXetVaChamChop()
    {
        if (playerCamera == null) return;
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        _lastRayOrigin = ray.origin;
        _lastRayDir = ray.direction;

        LayerMask maskDung = (chopLayer.value != 0) ? chopLayer : Physics.DefaultRaycastLayers;

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, maskDung))
        {
            _lastRayHit = true;
            _lastRayHitPoint = hit.point;

            Terrain hitTerrain = hit.collider.GetComponent<Terrain>();
            bool trungTerrain = hitTerrain != null;

            if (trungTerrain)
            {
                if (TreeManager.Instance == null)
                {
                    Debug.LogError("[Player_Controller] ❌ TreeManager.Instance = NULL!");
                    return;
                }

                TreeManager.Instance.TryChopTree(hitTerrain, hit.point, Runner);
            }
        }
        else
        {
            _lastRayHit = false;
        }
    }

    private void ThucHienXetVaChamMine()
    {
        if (BanTiaTuTamManHinh(interactRange, rockLayer, out RaycastHit hit))
        {
            RockScript cucDa = hit.collider.GetComponent<RockScript>();
            if (cucDa != null) cucDa.RPC_NhanSatThuongCuoc(25f);
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

    private void HandleAttackAnimal()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (BanTiaTuTamManHinh(interactRange, 0, out RaycastHit hit))
        {
            var animalAI = hit.collider.GetComponent<ithappy.Animals_FREE.AnimalAI_Controller>();
            if (animalAI != null) animalAI.RPC_AnimalTakeDamage(attackDamageToAnimal, Runner.LocalPlayer);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new DuLieuInput();
        if (!HasInputAuthority) return;

        bool dangGoPhim = EventSystem.current != null &&
                          EventSystem.current.currentSelectedGameObject != null &&
                          EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;

        if (ChatSystem.IsChatting || dangGoPhim)
        {
            input.Set(data);
            return;
        }

        data.isJumpPressed = jumpPressedLocal;

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
            data.isRunfast = sprintPressedLocal;
        }

        input.Set(data);
        jumpPressedLocal = false;
        mouseXLocalAcc = 0f;
    }

    public void OnMove(InputValue value)
    {
        if (!HasInputAuthority) return;
        moveInputLocal = value.Get<Vector2>();
    }

    #endregion

    #region XỬ LÝ VẬT LÝ & ĐỒNG BỘ TRÊN MẠNG (FIXED UPDATE NETWORK)

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority && !HasInputAuthority) return;
        bool dangGoPhim = EventSystem.current != null &&
                          EventSystem.current.currentSelectedGameObject != null &&
                          EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;

        if (ChatSystem.IsChatting || dangGoPhim)
        {
            character.Move(Vector3.zero);
            isrun = false;
            isSprinting = false;
            isJumping = false;
            return;
        }

        // --- KHÓA DI CHUYỂN & XỬ LÝ VA CHẠM KHI CHẶT CÂY/ĐẬP ĐÁ ---
        if (isDoingAction)
        {
            character.Move(Vector3.zero); // Ép nhân vật đứng im
            isrun = false;
            isSprinting = false;
            isJumping = false;

            // Kiểm tra khoảnh khắc va chạm (lưỡi rìu đập vào cây)
            if (hitTimer.Expired(Runner))
            {
                hitTimer = TickTimer.None;
                if (pendingActionType == 1) ThucHienXetVaChamChop();
                else if (pendingActionType == 2) ThucHienXetVaChamMine();
            }

            // Kiểm tra kết thúc toàn bộ animation để mở khóa di chuyển
            if (actionTimer.Expired(Runner))
            {
                isDoingAction = false;
                pendingActionType = 0;
                actionTimer = TickTimer.None;
            }

            return; // Dừng lại ở đây, không nhận Input di chuyển nữa
        }
        // ------------------------------------------------------------

        if (GetInput(out DuLieuInput data))
        {
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
            float tocDoHienTai = data.isRunfast ? runfast : speed;

            isrun = data.moveInput.magnitude > 0.1f;
            isSprinting = isrun && data.isRunfast;

            if (huongDiChuyen.magnitude >= 0.1f)
            {
                character.maxSpeed = tocDoHienTai;
                character.Move(huongDiChuyen.normalized);
                Quaternion huongMucTieu = Quaternion.LookRotation(huongDiChuyen);
                transform.rotation = Quaternion.Slerp(transform.rotation, huongMucTieu, Runner.DeltaTime * 15f);
            }
            else
            {
                character.Move(Vector3.zero);
            }
        }
    }

    #endregion

    #region XỬ LÝ HÌNH ẢNH & HOẠT ẢNH (RENDER)

    public override void Render()
    {
        if (animator != null)
        {
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
            animator.SetBool("isRunning", isrun);
            animator.SetBool("isRunFast", isSprinting);
        }
    }

    private void OnToolChanged()
    {
        if (HasInputAuthority && UI_HotBar.Instance != null)
            UI_HotBar.Instance.HighlightSlot(CurrentToolIndex);

        if (vuKhiDangCamThucTe != null)
        {
            Destroy(vuKhiDangCamThucTe);
            vuKhiDangCamThucTe = null;
        }

        if (CurrentToolIndex < 0 || CurrentToolIndex > 3) return;

        int idDangCam = HotbarIDs[CurrentToolIndex];
        if (idDangCam > 0 && InventoryManager.instance != null)
        {
            Item thongTinItem = InventoryManager.instance.TraCuuItem(idDangCam);

            if (thongTinItem != null && thongTinItem.model3DPrefab != null && viTriCamVuKhi != null)
            {
                vuKhiDangCamThucTe = Instantiate(thongTinItem.model3DPrefab, viTriCamVuKhi);
                vuKhiDangCamThucTe.transform.localScale = thongTinItem.scaleTrenTay;
                Transform vitriCamModel = vuKhiDangCamThucTe.transform.Find("vitricam");

                if (vitriCamModel != null)
                {
                    vuKhiDangCamThucTe.transform.localPosition = -vitriCamModel.localPosition;
                    vuKhiDangCamThucTe.transform.localRotation = Quaternion.Inverse(vitriCamModel.localRotation);
                }
                else
                {
                    vuKhiDangCamThucTe.transform.localPosition = Vector3.zero;
                    vuKhiDangCamThucTe.transform.localRotation = Quaternion.identity;
                }
            }
        }
    }

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

    #endregion

    #region HỆ THỐNG CÂU CÁ (LOGIC & RPC)

    private void BatDauCauCa()
    {
        currentState = FishState.Casting;
        Debug.Log("<color=blue>Đã quăng cần! Chờ phao rơi xuống...</color>");

        Vector3 diemBatDau = cameraTransform.position + cameraTransform.forward * 1.5f;
        float lucNem = 12f;
        Vector3 huongNem = cameraTransform.forward * lucNem + Vector3.up * 2f;

        RPC_QuangPhaoVatLy(diemBatDau, huongNem);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_QuangPhaoVatLy(Vector3 diemBatDau, Vector3 huongNem)
    {
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

        if (animator != null)
        {
            animator.SetTrigger("QuangCan");
        }
    }

    public void PhaoDaChamNuoc()
    {
        Debug.Log("<color=cyan>Phao đã tiếp nước êm ái! Bắt đầu dụ cá...</color>");
        currentState = FishState.Waiting;
        cauCaCoroutine = StartCoroutine(TienTrinhCauCa());
    }

    public void PhaoRotTrenCan()
    {
        Debug.Log("<color=red>Trượt nước rồi! Phao rớt trên bờ, tự động thu cần!</color>");
        ThuCanCau("Rớt trên cạn");
    }

    private System.Collections.IEnumerator TienTrinhCauCa()
    {
        float thoiGianCho = Random.Range(3f, 6f);
        yield return new WaitForSeconds(thoiGianCho);

        currentState = FishState.Giatca;
        Debug.Log("<color=yellow>Cá cắn câu!!! BẤM CHUỘT PHẢI ĐỂ GIẬT NGAY!</color>");

        if (iconCamThan != null) iconCamThan.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        if (currentState == FishState.Giatca)
        {
            ThuCanCau("<color=red>Trễ quá, cá xơi mồi rồi bơi mất tiêu!</color>");
        }
    }
    private void ThanhCongGiatCa()
    {
        Debug.Log("<color=green>Giật thành công! Lên cá!!!</color>");
        if (cauCaCoroutine != null) StopCoroutine(cauCaCoroutine);

        ThemDoVaoTui(9, 1);
        ThuCanCau("Hoàn tất câu cá, cất cần vào túi!", false);
    }

    private void ThuCanCau(string lyDo, bool laHuy = true)
    {
        Debug.Log(lyDo);
        currentState = FishState.Idle;

        if (iconCamThan != null) iconCamThan.SetActive(false);

        RPC_ThuPhao(laHuy);

        if (cauCaCoroutine != null) StopCoroutine(cauCaCoroutine);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_ThuPhao(bool laHuy)
    {
        if (currentphaocauca != null) Destroy(currentphaocauca);

        if (laHuy)
        {
            animator.SetTrigger("HuyCau");
        }
        else
        {
            animator.SetTrigger("GiatCan");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_HienThiPhao(Vector3 viTriMatNuoc)
    {
        if (currentphaocauca != null) Destroy(currentphaocauca);
        if (Phaocauca != null)
        {
            currentphaocauca = Instantiate(Phaocauca, viTriMatNuoc, Quaternion.identity);
        }
    }

    #endregion

    #region HỆ THỐNG GỌI HÀM TỪ XA (RPC)

    // Hàm báo hiệu bắt đầu hành động
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_BaoHieuBatDauAction(int actionType, float totalAnimTime, float timeToHit)
    {
        isDoingAction = true;
        pendingActionType = actionType;

        // Cài đặt đồng hồ đếm ngược
        actionTimer = TickTimer.CreateFromSeconds(Runner, totalAnimTime);
        hitTimer = TickTimer.CreateFromSeconds(Runner, timeToHit);

        // Chạy Animation cho tất cả client
        if (actionType == 1) RPC_AnimChatCay();
        else if (actionType == 2) RPC_AnimDapDa();
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
    public void RPC_TakeDame(float Dame)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth - Dame, 0, MaxHealth);
        if (CurrentHealth <= 0) { /* Die */ }
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
                    bool daNhat = false;
                    bool isstack = true;
                    if (InventoryManager.instance != null)
                    {
                        Item thongTin = InventoryManager.instance.TraCuuItem(idThucTe);
                        if (thongTin != null) isstack = thongTin.stackable;
                    }

                    if (isstack)
                    {
                        for (int i = 0; i < TuiDo.Length; i++)
                        {
                            if (TuiDo[i].ItemID == idThucTe)
                            {
                                O_VatPham doVat = TuiDo[i];
                                doVat.SoLuong++;
                                TuiDo.Set(i, doVat);
                                daNhat = true; break;
                            }
                        }
                    }

                    if (!daNhat)
                    {
                        for (int i = 0; i < TuiDo.Length; i++)
                        {
                            if (TuiDo[i].ItemID == 0)
                            {
                                TuiDo.Set(i, new O_VatPham { ItemID = idThucTe, SoLuong = 1 });
                                daNhat = true; break;
                            }
                        }
                    }

                    if (daNhat)
                    {
                        RPC_XoaRacKhapBanDo(nObj);
                        Rpc_NotifyPickupClient(idThucTe, 1);
                        break;
                    }
                }
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

        Debug.LogWarning("[Player_Controller] Balo đã đầy, không thể nhặt thêm!");
        return false;
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
        if (InventoryManager.instance == null) return;
        Item thongTinItem = InventoryManager.instance.TraCuuItem(itemID_ServerGui);
        if (thongTinItem == null || ItemNotifyManager.Instance == null) return;

        ItemNotifyManager.Instance.ShowNotify(thongTinItem.itemName, soLuong_ServerGui, thongTinItem.icon);
        if (Player_QuestManager.localQuest != null) Player_QuestManager.localQuest.KiemTraTienDo();
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ThayDoiTien(int soTien)
    {
        Gold = Mathf.Max(0, Gold + soTien);
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
    public void RPC_HoanThanhQuest(int idVatPham, int soLuongCanTru, int tienThuong)
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
        Gold += tienThuong;
        KiemTraDonDepHotbar();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_EquipTool(int toolIndex)
    {
        CurrentToolIndex = (CurrentToolIndex == toolIndex) ? -1 : toolIndex;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_CapNhatUIHotbarKhach(int slotIndex, int itemID)
    {
        CapNhatUIHotbarLocal(slotIndex, itemID);
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_AddExp(float exp)
    {
        ExpCurrent += exp;
        while (ExpCurrent >= expToLevelUp)
        {
            ExpCurrent = 0;
            level++;
            expToLevelUp *= 1.1f;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimChatCay()
    {
        if (animator != null)
        {
            animator.SetTrigger("Chatcay");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimDapDa()
    {
        if (animator != null)
        {
            animator.SetTrigger("dapda");
        }
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

    #endregion

    #region CÁC HÀM TIỆN ÍCH & XỬ LÝ GIAO DIỆN CỤC BỘ


    public void ThucHienDichChuyen(Vector3 toaDoMoi)
    {
        if (Object.HasStateAuthority)
        {
            if (character != null)
            {
                character.Teleport(toaDoMoi);
            }
        }
        else
        {
            RPC_XinPhepDichChuyen(toaDoMoi);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_XinPhepDichChuyen(Vector3 toaDoMoi)
    {
        if (character != null)
        {
            character.Teleport(toaDoMoi);
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
    }

    public void CapNhatUIHotbarLocal(int slotIndex, int itemID)
    {
        if (UI_HotBar.Instance == null) return;
        if (itemID == 0) { UI_HotBar.Instance.CapNhatHinhAnhSlot(slotIndex, null); return; }

        if (InventoryManager.instance != null)
        {
            Item thongTinItem = InventoryManager.instance.TraCuuItem(itemID);
            if (thongTinItem != null)
                UI_HotBar.Instance.CapNhatHinhAnhSlot(slotIndex, thongTinItem.icon);
        }
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

    public void Click_NutBanGo()
    {
        Player_Controller myPlayer = NetworkRunner.Instances[0]
            .GetPlayerObject(NetworkRunner.Instances[0].LocalPlayer)
            .GetComponent<Player_Controller>();
        if (myPlayer != null) myPlayer.RPC_BanVatPham(1, 10);
    }

    #endregion

    #region HÀM TRỐNG BẮT BUỘC CỦA INTERFACE

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
}