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

    #region 1. KHAI BÁO BIẾN (VARIABLES)

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
    private float mouseXLocalAcc;
    public LayerMask layerVaChamCamera;
    public float fovBinhThuong = 60f;
    public float fovChayNhanh = 75f;
    public float tocDoZoom = 5f;

    [Header("Kinh tế & Túi đồ")]
    [Networked] public int Gold { get; set; }
    [Networked] public int Gem { get; set; }
    [Networked, Capacity(20)] public NetworkArray<O_VatPham> TuiDo { get; }
    [Networked, Capacity(6)]  public NetworkArray<int> HotbarIDs { get; }

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

    [Header("Trạng thái Hành Động (Chặt/Đào)")]
    [Networked] public NetworkBool isDoingAction { get; set; }
    [Networked] public TickTimer actionTimer { get; set; }
    [Networked] public TickTimer hitTimer { get; set; }
    [Networked] public int pendingActionType { get; set; }

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

            // --- THÊM 2 DÒNG NÀY ĐỂ TỰ ĐỘNG TÌM TEXT HINT NGOÀI GIAO DIỆN ---
            GameObject objHint = GameObject.Find("Text_Hint");
            if (objHint != null) hintText = objHint.GetComponent<TextMeshProUGUI>();

            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            if (playerCamera == null) Debug.LogError("[Player_Controller] ❌ 'Player Camera' chưa được gán!");
            if (TreeManager.Instance == null) Debug.LogError("[Player_Controller] ❌ Không tìm thấy TreeManager!");
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
            CurrentHealth = MaxHealth;
            CurrentStamina = MaxStamina;
        }
    }

    void Update()
    {
        if (HasInputAuthority && Keyboard.current != null && Mouse.current != null)
        {
            RPC_AddExp(0.1f);
            
            // 1. NGĂN CHẶN THAO TÁC KHI ĐANG CHAT
            if (KiemTraDangGoPhimChat()) return;

            // 2. KIỂM TRA TRẠNG THÁI CÁC BẢNG UI
            bool isBaloOpen   = (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo);
            bool isShopOpen   = (ShopUIController.instance != null && ShopUIController.instance.isShopOpen);
            bool isQuestOpen  = (QuestManager.instance != null && QuestManager.instance.isQuest_Open);
            bool isChatActive = (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive);
            bool isEscOpen    = (ESC.instance != null && ESC.instance.isESC_Open);
            bool isMapOpen    = (MapManager.Instance != null && MapManager.Instance.dangMoMap);

            bool batKyUI_NaoDangMo = isBaloOpen || isEscOpen || isShopOpen || isChatActive || isQuestOpen || isMapOpen;

            // 3. QUẢN LÝ CON TRỎ CHUỘT VÀ TIA NHÌN
            if (batKyUI_NaoDangMo)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
                if (hintText != null) hintText.text = ""; 
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
                yRotation += Mouse.current.delta.x.ReadValue() * mouseSensitivity;
                xRotation -= Mouse.current.delta.y.ReadValue() * mouseSensitivity;
                xRotation  = Mathf.Clamp(xRotation, -60f, 60f);
                
                int idDangCam = (CurrentToolIndex >= 0) ? HotbarIDs[CurrentToolIndex] : 0;
                UpdateFarmingUI(idDangCam);
            }

            // 4. XỬ LÝ NHẬP LIỆU BÀN PHÍM CHUNG
            HandleUIGlobalInput(isChatActive, isShopOpen);
            HandleHotbarInput(isBaloOpen, isShopOpen, isChatActive, isEscOpen, isQuestOpen);
            
            if (Keyboard.current.cKey.wasPressedThisFrame) RPC_TakeDame(10);
            if (Keyboard.current.vKey.wasPressedThisFrame) RPC_TakeDame(-10);
            if (Keyboard.current.kKey.wasPressedThisFrame) RPC_ThayDoiTien(5);
            if (Keyboard.current.lKey.wasPressedThisFrame) RPC_ThayDoiTien(-5);

            // 5. XỬ LÝ TƯƠNG TÁC (Khi không mở UI)
            if (!batKyUI_NaoDangMo)
            {
                int idDangCam = (CurrentToolIndex >= 0) ? HotbarIDs[CurrentToolIndex] : 0;
                HandleGameplayInteraction(idDangCam);
            }

            // 6. XỬ LÝ DI CHUYỂN
            sprintPressedLocal = Keyboard.current.leftShiftKey.isPressed;
            XuLyTheLuc();
            if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressedLocal = true;
            
            float trucX = Keyboard.current.dKey.isPressed ? 1f : (Keyboard.current.aKey.isPressed ? -1f : 0f);
            float trucY = Keyboard.current.wKey.isPressed ? 1f : (Keyboard.current.sKey.isPressed ? -1f : 0f);
            moveInputLocal = new Vector2(trucX, trucY).normalized;
        }
    }

    void LateUpdate()
    {
        if (HasInputAuthority && cameraTransform != null)
        {
            Quaternion camRotation  = Quaternion.Euler(xRotation, yRotation, 0f);
            Vector3 diemNhin        = transform.position + Vector3.up * 1.5f;
            Vector3 huongCamera     = -(camRotation * Vector3.forward);
            Vector3 viTriDuKien     = diemNhin + huongCamera * khoangCachCamera;

            if (Physics.Raycast(diemNhin, huongCamera, out RaycastHit hit, khoangCachCamera, layerVaChamCamera))
                cameraTransform.position = hit.point + hit.normal * 0.4f;
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

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

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
            if (data.isJumpPressed && character.Grounded)
            {
                if (dongHoChoNhay.ExpiredOrNotRunning(Runner))
                {
                    character.Jump();
                    isJumping     = true;
                    dongHoChoNhay = TickTimer.CreateFromSeconds(Runner, thoiGianHoiNhay);
                }
            }
            else if (character.Grounded)
            {
                isJumping = false;
            }

            Vector3 huongDiChuyen = new Vector3(data.moveInput.x, 0f, data.moveInput.y);
            float tocDoHienTai    = data.isRunfast ? runfast : speed;

            isrun       = data.moveInput.magnitude > 0.1f;
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

    public override void Render()
    {
        if (animator != null)
        {
            if (isJumping)
            {
                isSprinting = false;
                isrun       = false;
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

        if (KiemTraDangGoPhimChat())
        {
            input.Set(data);
            return;
        }

        data.isJumpPressed = jumpPressedLocal;
        
        bool baloDangMo = (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo);
        bool ESCDangMo  = (ESC.instance != null && ESC.instance.isESC_Open);
        bool ishopopen  = (ShopUIController.instance != null && ShopUIController.instance.isShopOpen);
        bool IsChat     = (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive);
        bool isMapOpen  = (MapManager.Instance != null && MapManager.Instance.dangMoMap);
        bool dangCauCa  = (currentState != FishState.Idle); 

        if (baloDangMo || ESCDangMo || ishopopen || IsChat || isMapOpen || dangCauCa)
        {
            data.moveInput     = Vector2.zero;
            data.isJumpPressed = false;
            data.mouseX        = 0f;
        }
        else
        {
            Vector3 huongChuanBiGui = Vector3.zero;
            if (cameraTransform != null)
            {
                Vector3 camForward = cameraTransform.forward;
                Vector3 camRight   = cameraTransform.right;
                camForward.y = 0; camRight.y = 0;
                camForward.Normalize(); camRight.Normalize();
                huongChuanBiGui = camForward * moveInputLocal.y + camRight * moveInputLocal.x;
            }
            data.moveInput  = new Vector2(huongChuanBiGui.x, huongChuanBiGui.z);
            data.isRunfast  = dangChayNhanh;
        }

        input.Set(data);
        jumpPressedLocal = false;
        mouseXLocalAcc   = 0f;
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
        // 1. NÚT F: TƯƠNG TÁC THEO PHẠM VI 
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            Collider[] cacVatTheGan = Physics.OverlapSphere(transform.position, banKinhNhat, farmlandLayer | interactLayer);
            bool daTuongTacXong = false;

            foreach (var col in cacVatTheGan)
            {
                // Ưu tiên 1: Cây trồng đã chín -> Nhổ
                FarmPlot plot = col.GetComponentInParent<FarmPlot>();
                if (plot != null && plot.CurrentState == FarmPlot.PlotState.CayLon)
                {
                    plot.RPC_ThuHoach(Runner.LocalPlayer);
                    daTuongTacXong = true;
                    break; 
                }

                // Ưu tiên 2: Báo cho hệ thống biết là ĐANG Ở GẦN NPC
                if (col.CompareTag("NPC"))
                {
                    // Mình đánh dấu 'daTuongTacXong = true' để chặn không cho nó chạy xuống hàm nhặt rác bên dưới.
                    // Còn việc mở bảng Chat thì cái Script 'NPC_DialogueTrigger' của Bò sẽ tự động lo liệu!
                    daTuongTacXong = true; 
                    break;
                }
            }

            // Ưu tiên 3: Nếu không đứng gần cây chín hay NPC nào -> Quét hút rác
            if (!daTuongTacXong)
            {
                RPC_YeuCauNhatRac(); 
            }
        }

        // 2. CHUỘT TRÁI: CÔNG CỤ (Tool/Vũ khí)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            switch (idDangCam)
            {
                case 4: HandleAttackAnimal(); break; 
                case 5: HandleChopping(); break;     
                case 6: HandleMining(); break;       
            }
        }

        // 3. CHUỘT PHẢI: VẬT PHẨM TIÊU HAO & CÂU CÁ
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (idDangCam == 8)
            {
                if (currentState == FishState.Idle) BatDauCauCa();
                else if (currentState == FishState.Waiting) ThuCanCau("Kéo sớm quá!");
                else if (currentState == FishState.Giatca) ThanhCongGiatCa();
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

        if (BanTiaTuTamManHinh(interactRange, 0, out RaycastHit hit))
        {
            var animalAI = hit.collider.GetComponent<ithappy.Animals_FREE.AnimalAI_Controller>();
            if (animalAI != null) animalAI.RPC_AnimalTakeDamage(attackDamageToAnimal, Runner.LocalPlayer);
        }
    }

    private void HandleChopping()
    {
        if (playerCamera == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        RPC_AnimChatCay();
        RPC_BaoHieuBatDauAction(1, 1.5f, 0.6f);
    }

    private void HandleMining()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        RPC_BaoHieuBatDauAction(2, 1.5f, 0.6f);
        if (BanTiaTuTamManHinh(interactRange, rockLayer, out RaycastHit hit))
        {
            RockScript cucDa = hit.collider.GetComponent<RockScript>();
            if (cucDa != null) cucDa.RPC_NhanSatThuongCuoc(25f); 
        }
    }

    private void ThucHienXetVaChamChop()
    {
        if (playerCamera == null) return;

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        _lastRayOrigin = ray.origin;
        _lastRayDir    = ray.direction;

        LayerMask maskDung = (chopLayer.value != 0) ? chopLayer : Physics.DefaultRaycastLayers;

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, maskDung))
        {
            _lastRayHit      = true;
            _lastRayHitPoint = hit.point;

            Terrain hitTerrain = hit.collider.GetComponent<Terrain>();
            if (hitTerrain != null && TreeManager.Instance != null)
            {
                TreeManager.Instance.TryChopTree(hitTerrain, hit.point, Runner);
            }
        }
        else _lastRayHit = false;
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
        string chuChuoiUI = ""; // Chuỗi này sẽ cộng dồn các hướng dẫn lại với nhau

        // --- 1. TIA NGẮM (Chỉ dành riêng cho việc Gieo Hạt / Xem tiến độ cây) ---
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

        // --- 2. PHẠM VI XUNG QUANH (Thu hoạch, NPC, Lụm đồ) ---
        // Quét một vòng tròn quanh nhân vật xem có cái gì đứng gần không
        Collider[] cacVatTheGan = Physics.OverlapSphere(transform.position, banKinhNhat, farmlandLayer | interactLayer);
        float khoangCachNganNhat = float.MaxValue;
        Collider mucTieuGanNhat = null;

        // Tìm cái vật thể GẦN NHẤT để hiện UI
        foreach (var col in cacVatTheGan)
        {
            FarmPlot plot = col.GetComponentInParent<FarmPlot>();
            bool laCayLon = (plot != null && plot.CurrentState == FarmPlot.PlotState.CayLon);
            bool laNPC = col.CompareTag("NPC");
            bool laItem = col.CompareTag("Items");

            if (laCayLon || laNPC || laItem)
            {
                float khoangCach = Vector3.Distance(transform.position, col.transform.position);
                if (khoangCach < khoangCachNganNhat)
                {
                    khoangCachNganNhat = khoangCach;
                    mucTieuGanNhat = col;
                }
            }
        }

        // Hiện chữ tương ứng cho vật thể GẦN NHẤT đó
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
            else if (mucTieuGanNhat.CompareTag("Items"))
            {
                XuLyItem theCanCuoc = mucTieuGanNhat.GetComponent<XuLyItem>();
                if (theCanCuoc != null && theCanCuoc.thongTinDoVat != null)
                    chuChuoiUI += $"[F] Nhặt {theCanCuoc.thongTinDoVat.itemName}\n";
                else
                    chuChuoiUI += "[F] Nhặt đồ\n";
            }
        }

        // --- 3. ĐỒ TIÊU HAO ĐANG CẦM TRÊN TAY ---
        if (idDangCam > 0 && InventoryManager.instance != null)
        {
            Item thongTinItem = InventoryManager.instance.TraCuuItem(idDangCam);
            if (thongTinItem != null && thongTinItem.loaiTieuHao != Item.LoaiTieuHao.KhongPhai)
            {
                chuChuoiUI += $"[Chuột Phải] Dùng {thongTinItem.itemName}";
            }
        }

        // Hiển thị ra màn hình (Loại bỏ cái dấu \n bị dư ở cuối)
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
    }

    public void PhaoDaChamNuoc()
    {
        currentState = FishState.Waiting;
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

    #region 6. TÚI ĐỒ & GIAO DIỆN (INVENTORY & UI)

    private void OnToolChanged()
    {
        if (HasInputAuthority && UI_HotBar.Instance != null)
            UI_HotBar.Instance.HighlightSlot(CurrentToolIndex);

        if (vuKhiDangCamThucTe != null)
        {
            Destroy(vuKhiDangCamThucTe);
            vuKhiDangCamThucTe = null;
        }

        if (CurrentToolIndex < 0 || CurrentToolIndex > 5) return;

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
        if(MapManager.Instance != null && MapManager.Instance.dangMoMap) MapManager.Instance.DongMap();
        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo)
            InventoryManager.instance.BatTatBalo(TuiDo, this);

        if (QuestManager.instance != null && QuestManager.instance.isQuest_Open)
            QuestManager.instance.Battatbangnhiemvu();

        if (ShopUIController.instance != null && ShopUIController.instance.isShopOpen)
        {
            ShopUIController.instance.isShopOpen   = false;
            ShopUIController.instance.dangmoshop    = false;
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

    #endregion

    #region 7. CHỈ SỐ & SÁT THƯƠNG (STATS & DAMAGE)

    private void XuLyTheLuc()
    {
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
                if (dongHoDelayHoi > 0) dongHoDelayHoi -= Time.deltaTime;
                else CurrentStamina += tocDoHoi * Time.deltaTime;
            }
        }
        CurrentStamina = Mathf.Clamp(CurrentStamina, 0, MaxStamina);
    }

    #endregion

    #region 8. HỆ THỐNG GỌI HÀM TỪ XA (RPC)


    // Dán hàm này vào bên trong Player_Controller để Script kia tìm thấy
    public int DemSoLuongVatPham(int itemID)
    {
        int tong = 0;
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == itemID) tong += TuiDo[i].SoLuong;
        }
        return tong;
    }

    // Dán hàm RPC này vào để thực hiện lệnh chế tạo từ UI
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
        // Hàm này y hệt hàm ThemDoVaoTui của Bò, Bò kiểm tra xem file có chưa nhé
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == idCanThem)
            {
                O_VatPham doVat = TuiDo[i];
                doVat.SoLuong += soLuongCanThem;
                TuiDo.Set(i, doVat);
                return;
            }
        }
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == 0)
            {
                TuiDo.Set(i, new O_VatPham { ItemID = idCanThem, SoLuong = soLuongCanThem });
                return;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_TakeDame(float Dame)
    {
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
        float bonusHealth = 0f, bonusStamina = 0f, bonusSpeed = 0f, bonusDamage = 0f, bonusArmor = 0f;

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
            ExpCurrent = 0;
            level++;
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
                NetworkObject nObj      = Obj.GetComponent<NetworkObject>();
                XuLyItem theCanCuoc     = Obj.GetComponent<XuLyItem>();

                if (nObj != null && nObj.IsValid && theCanCuoc != null && theCanCuoc.thongTinDoVat != null)
                {
                    int idThucTe = theCanCuoc.thongTinDoVat.itemID;
                    bool daNhat  = ThemDoVaoTui(idThucTe, 1);
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
        bool daNhat  = ThemDoVaoTui(idThucTe, 1);
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
    public void RPC_HoanThanhQuest(int idVatPham, int soLuongCanTru, int tienThuong)
    {
        int soLuongDaTru = 0;
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == idVatPham && TuiDo[i].SoLuong > 0)
            {
                var doVat = TuiDo[i];
                int soLuongCoTheTru = Mathf.Min(doVat.SoLuong, soLuongCanTru - soLuongDaTru);
                doVat.SoLuong      -= soLuongCoTheTru;
                soLuongDaTru       += soLuongCoTheTru;
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_BaoHieuBatDauAction(int actionType, float totalAnimTime, float timeToHit)
    {
        isDoingAction = true;
        pendingActionType = actionType;
        actionTimer = TickTimer.CreateFromSeconds(Runner, totalAnimTime);
        hitTimer = TickTimer.CreateFromSeconds(Runner, timeToHit);
        if (actionType == 1) RPC_AnimChatCay();
        else if (actionType == 2) RPC_AnimDapDa();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimChatCay() { if (animator != null) animator.SetTrigger("Chatcay"); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimDapDa() { if (animator != null) animator.SetTrigger("dapda"); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_AnimSlash() { if (animator != null) animator.SetTrigger("slash"); }

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
        if (animator != null) animator.SetTrigger("QuangCan"); 
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_ThuPhao(bool laHuy)
    {
        if (currentphaocauca != null) Destroy(currentphaocauca);
        if (laHuy) animator.SetTrigger("HuyCau"); 
        else animator.SetTrigger("GiatCan"); 
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
}