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
    // ----------------------------------------------------------------------
    // [KHAI BÁO BIẾN] - Nơi chứa toàn bộ dữ liệu của Player
    // Biến có [Networked] là biến xài chung trên mạng (Server quản lý)
    // Biến bình thường là biến xài cục bộ (Chỉ máy người chơi đó biết)
    // ----------------------------------------------------------------------

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


    [Header("Camera & Chuột")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.5f;
    private float xRotation = 0f;
    private float yRotation = 0f;
    public float khoangCachCamera = 4f;
    private float mouseXLocalAcc;
    public LayerMask layerVaChamCamera;

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
    
    public Transform viTriCamVuKhi; // Điểm Neo trên tay
    private GameObject vuKhiDangCamThucTe; // Nhớ vũ khí đang cầm để xóa

    [Header("Tương Tác - Chặt Cây & Nhặt Đồ")]
    public Camera playerCamera;
    public float interactRange = 3f;
    public LayerMask interactLayer;

    #endregion

    #region KHỞI TẠO (SPAWNED)
    // ----------------------------------------------------------------------
    // [SPAWNED] - Hàm này chạy ĐẦU TIÊN khi nhân vật vừa được sinh ra.
    // Thay thế cho hàm Start() và Awake() trong game Singleplayer.
    // Dùng để setup camera, nhận diện máy khách/máy chủ.
    // ----------------------------------------------------------------------
    public override void Spawned()
    {
        animator = GetComponent<Animator>();
        CurrentHealth = 100;
        // Nếu mình không phải là chủ của con nhân vật này (Nhân vật của thằng khác) -> Tắt điều khiển
        if (!HasStateAuthority && !HasInputAuthority)
        {
            if (character != null) character.enabled = false;
        }

        // Nếu ĐÂY LÀ NHÂN VẬT CỦA MÌNH
        if (HasInputAuthority)
        {
            
            localPlayer = this;
            Runner.AddCallbacks(this);
            Runner.SetPlayerObject(Runner.LocalPlayer, Object);

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            TuiDo.Set(0, new O_VatPham { ItemID = 101, SoLuong = 50 });
        }
        else // NẾU LÀ NHÂN VẬT CỦA ĐỨA KHÁC TRÊN MÀN HÌNH MÌNH
        {
            if (character != null) character.enabled = true;

            // Tắt camera và tai nghe của đứa khác đi, để không bị nhìn lộn xộn
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = false;

            AudioListener listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }
    #endregion

    #region THU THẬP ĐẦU VÀO CỤC BỘ (UPDATE & ON INPUT)
    // ----------------------------------------------------------------------
    // [UPDATE] - Chạy liên tục mỗi khung hình (Cục bộ).
    // Tuyệt đối KHÔNG code di chuyển ở đây trong Fusion. 
    // Chỉ dùng Update để: Đọc phím bật/tắt UI, đọc chuột, gom data chuẩn bị gửi Server.
    // ----------------------------------------------------------------------
    void Update()
    {
        if (HasInputAuthority && Keyboard.current != null && Mouse.current != null)
        {
            bool dangGoPhim = EventSystem.current != null && 
                              EventSystem.current.currentSelectedGameObject != null && 
                              EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;
            if (ChatSystem.IsChatting || dangGoPhim) return;
            // 1. ĐỌC TRẠNG THÁI BẢNG UI
            bool baloDangMo = (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo);
            bool ishopopen = (ShopUIController.instance != null && ShopUIController.instance.isShopOpen);
            bool questDangMo = (QuestManager.instance != null && QuestManager.instance.isQuest_Open);
            bool IsChatAct = (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive);
            bool ESCDangMo = (ESC.instance != null && ESC.instance.isESC_Open);

            // 2. QUẢN LÝ CHUỘT & CAMERA 
            if (baloDangMo || ESCDangMo || ishopopen || IsChatAct || questDangMo)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                yRotation += Mouse.current.delta.x.ReadValue() * mouseSensitivity;
                xRotation -= Mouse.current.delta.y.ReadValue() * mouseSensitivity;
                xRotation = Mathf.Clamp(xRotation, -60f, 60f);
            }

            // 3. UI TƯƠNG TÁC (ESC, Balo, Nhiệm vụ)
            if (Keyboard.current.escapeKey.wasPressedThisFrame)            
            {
                TatToanBoUI(); 
                if (ESC.instance != null) ESC.instance.BatTatESC();
            }
            if (Keyboard.current.cKey.wasPressedThisFrame)            
            {
                RPC_TakeDame(10);
            }
            if (Keyboard.current.vKey.wasPressedThisFrame)            
            {
                RPC_TakeDame(-10);
            }

            if (!IsChatAct && !ishopopen)
            {
                if (Keyboard.current.bKey.wasPressedThisFrame && InventoryManager.instance != null)
                    InventoryManager.instance.BatTatBalo(TuiDo, this); 

                if(Keyboard.current.tabKey.wasPressedThisFrame && QuestManager.instance != null)
                    QuestManager.instance.Battatbangnhiemvu();
            }

            // 4. HỆ THỐNG HOTBAR (GÁN ĐỒ)
            bool dangBam1 = Keyboard.current.digit1Key.wasPressedThisFrame;
            bool dangBam2 = Keyboard.current.digit2Key.wasPressedThisFrame;
            bool dangBam3 = Keyboard.current.digit3Key.wasPressedThisFrame;
            bool dangBam4 = Keyboard.current.digit4Key.wasPressedThisFrame;

            if (baloDangMo && ItemHover.itemID_DangDiChuot != 0)
            {
                // TRƯỜNG HỢP 1: ĐANG MỞ BALO -> GÁN ĐỒ VÀO Ô
                if (dangBam1) RPC_GanVaoHotbar(0, ItemHover.itemID_DangDiChuot); 
                if (dangBam2) RPC_GanVaoHotbar(1, ItemHover.itemID_DangDiChuot);
                if (dangBam3) RPC_GanVaoHotbar(2, ItemHover.itemID_DangDiChuot);
                if (dangBam4) RPC_GanVaoHotbar(3, ItemHover.itemID_DangDiChuot);
            }
            else if (!baloDangMo && !ishopopen && !IsChatAct && !ESCDangMo && !questDangMo)
            {
                // TRƯỜNG HỢP 2: ĐI DẠO BÌNH THƯỜNG (KHÔNG MỞ UI NÀO HẾT) -> RÚT/CẤT VŨ KHÍ
                if (dangBam1) RPC_EquipTool(0); 
                if (dangBam2) RPC_EquipTool(1); 
                if (dangBam3) RPC_EquipTool(2); 
                if (dangBam4) RPC_EquipTool(3); 
            }

            // 5. CÁC HÀNH ĐỘNG CƠ BẢN CẦN GỬI CHO SERVER
            if (Keyboard.current.eKey.wasPressedThisFrame) RPC_YeuCauNhatRac();
            sprintPressedLocal = Keyboard.current.leftShiftKey.isPressed;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressedLocal = true;

            float trucX = Keyboard.current.dKey.isPressed ? 1f : (Keyboard.current.aKey.isPressed ? -1f : 0f);
            float trucY = Keyboard.current.wKey.isPressed ? 1f : (Keyboard.current.sKey.isPressed ? -1f : 0f);
            moveInputLocal = new Vector2(trucX, trucY).normalized;

            if (Keyboard.current.kKey.wasPressedThisFrame) RPC_ThayDoiTien(5);
            if (Keyboard.current.lKey.wasPressedThisFrame) RPC_ThayDoiTien(-5);

            // 6. CHẶT CÂY & NHẶT ĐỒ BẰNG RAYCAST
            if (!baloDangMo && !ESCDangMo && !ishopopen && !IsChatAct && !questDangMo)
            {
                HandleChopping();
                HandlePickup();
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
            {
                cameraTransform.position = hit.point + hit.normal * 0.1f;
            }
            else
            {
                cameraTransform.position = viTriDuKien;
            }
            cameraTransform.rotation = camRotation;
        }
    }

    // ----------------------------------------------------------------------
    // [CHẶT CÂY] - Bấm chuột trái khi đang cầm rìu, bắn raycast vào Terrain
    // ----------------------------------------------------------------------
   private void HandleChopping()
    {
        if (playerCamera == null) return;
        
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Bắn tia Raycast từ giữa màn hình camera (phù hợp với góc nhìn thứ 3)
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
            {
                // Nếu tia bắn trúng TerrainCollider
                if (hit.collider.GetComponent<TerrainCollider>() != null)
                {
                    // Gọi lệnh chặt cây ngay lập tức
                    TreeManager.Instance.TryChopTree(hit.point, Runner);
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // [NHẶT ĐỒ RAYCAST] - Bấm chuột phải hoặc F để nhặt item nhìn thấy
    // ----------------------------------------------------------------------
    private void HandlePickup()
    {
        if (playerCamera == null) return;
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
            {
                if (hit.collider.CompareTag("Items"))
                {
                    NetworkObject itemNetObj = hit.collider.GetComponent<NetworkObject>();
                    if (itemNetObj != null)
                    {
                        RPC_YeuCauNhatRacTheoID(itemNetObj.Id);
                    }
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // [ON INPUT] - Đóng gói dữ liệu phím bấm để ship lên Server
    // ----------------------------------------------------------------------
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
            return; // Cắt điện toàn bộ phím gửi lên Server!
        }

        data.isJumpPressed = jumpPressedLocal;
        bool baloDangMo = (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo);
        bool ESCDangMo = (ESC.instance != null && ESC.instance.isESC_Open);
        bool ishopopen = (ShopUIController.instance != null && ShopUIController.instance.isShopOpen);
        bool IsChat = (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive);

        if (baloDangMo || ESCDangMo || ishopopen || IsChat)
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
    // ----------------------------------------------------------------------
    // [FIXED UPDATE NETWORK] - Trái tim của Fusion. 
    // Chạy với tốc độ cố định trên Server. Mọi tính toán liên quan đến 
    // vị trí, di chuyển, vật lý bắt buộc phải nằm ở đây để chống hack và lag.
    // ----------------------------------------------------------------------
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
    // ----------------------------------------------------------------------
    // [RENDER] - Dành riêng cho Animation và Hiệu ứng hình ảnh.
    // Hàm này chạy rất mượt, dùng để nội suy, làm mềm chuyển động 3D.
    // ----------------------------------------------------------------------
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
            else if (!isJumping) animator.SetBool("isJump", false);

            animator.SetBool("isRunning", isrun);
            animator.SetBool("isRunFast", isSprinting);
        }
    }

    // Hàm gọi khi Biến Mạng CurrentToolIndex thay đổi (Bật tắt Model trên tay)
    private void OnToolChanged()
{
    // 1. Cập nhật khung sáng UI (Chỉ chạy trên máy người chơi)
    if (HasInputAuthority && UI_HotBar.Instance != null)
    {
        UI_HotBar.Instance.HighlightSlot(CurrentToolIndex);
    }

    // 2. DỌN DẸP: Luôn hủy model cũ nếu nó đang tồn tại
    if (vuKhiDangCamThucTe != null)
    {
        Destroy(vuKhiDangCamThucTe);
        vuKhiDangCamThucTe = null;
    }

    // 3. Nếu CurrentToolIndex = -1 (đã cất đồ) thì dừng lại ở đây
    if (CurrentToolIndex < 0 || CurrentToolIndex > 3) return;

    // 4. Lấy ID từ Hotbar để sinh ra model mới
    int idDangCam = HotbarIDs[CurrentToolIndex];

    if (idDangCam > 0 && InventoryManager.instance != null)
    {
        Item thongTinItem = InventoryManager.instance.TraCuuItem(idDangCam);
        if (thongTinItem != null && thongTinItem.model3DPrefab != null)
        {
            // Sinh ra vật phẩm mới tại Điểm Neo (Socket)
            vuKhiDangCamThucTe = Instantiate(thongTinItem.model3DPrefab, viTriCamVuKhi);
            vuKhiDangCamThucTe.transform.localPosition = Vector3.zero;
            vuKhiDangCamThucTe.transform.localRotation = Quaternion.identity;
        }
    }
}
    #endregion

    #region HỆ THỐNG GỌI HÀM TỪ XA (RPC - REMOTE PROCEDURE CALLS)
    // ----------------------------------------------------------------------
    // [RPC] - Dùng để "hét" qua mạng. 
    // Ví dụ: Client bấm nhặt rác -> Hét lên cho Server xử lý (Input -> State)
    // Server cập nhật tiền -> Hét lên cho Client biết để cập nhật UI (State -> Input)
    // ----------------------------------------------------------------------

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_TakeDame(float Dame)
    {
        CurrentHealth -= Dame;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        if(CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            //Die
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
                Debug.Log("Quét trúng được: " + Obj);
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

    // ----------------------------------------------------------------------
    // [RPC MỚI] - Nhặt item cụ thể theo NetworkId (dùng cho raycast pickup)
    // ----------------------------------------------------------------------
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_YeuCauNhatRacTheoID(NetworkId itemId)
    {
        NetworkObject nObj = Runner.FindObject(itemId);
        if (nObj == null || !nObj.IsValid) return;

        XuLyItem theCanCuoc = nObj.GetComponent<XuLyItem>();
        if (theCanCuoc == null || theCanCuoc.thongTinDoVat == null) return;

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
        }
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
        Gold += soTien;
        if (Gold < 0) Gold = 0;
        Debug.Log("Server đã cập nhật tiền: " + Gold);
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
                doVat.SoLuong -= 1;
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
        bool isstack = thongTin.stackable;

        if (isstack)
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
        if (CurrentToolIndex == toolIndex)
        {
            CurrentToolIndex = -1; 
        }
        else
        {
            // Nếu bấm ô khác -> Chuyển sang ô đó
            CurrentToolIndex = toolIndex; 
        }
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
    #endregion

    #region CÁC HÀM TIỆN ÍCH & XỬ LÝ GIAO DIỆN CỤC BỘ
    // ----------------------------------------------------------------------
    // [HÀM TIỆN ÍCH] - Chứa các đoạn mã xử lý vòng lặp, UI cục bộ
    // Không liên quan tới mạng lưới, chỉ chạy trên máy người chơi
    // ----------------------------------------------------------------------

    private void TatToanBoUI()
    {
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
        if (itemID == 0)
        {
            UI_HotBar.Instance.CapNhatHinhAnhSlot(slotIndex, null); 
            return;
        }

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
            if (idDangGan > 0)
            {
                bool conHangTrongBalo = false;
                for (int j = 0; j < TuiDo.Length; j++)
                {
                    if (TuiDo[j].ItemID == idDangGan && TuiDo[j].SoLuong > 0)
                    {
                        conHangTrongBalo = true;
                        break;
                    }
                }

                if (conHangTrongBalo == false)
                {
                    HotbarIDs.Set(i, 0); 
                    RPC_CapNhatUIHotbarKhach(i, 0); 

                    if (CurrentToolIndex == i)
                    {
                        CurrentToolIndex = -1; 
                    }
                }
            }
        }
    }

    public void Click_NutBanGo()
    {
        Player_Controller myPlayer = NetworkRunner.Instances[0].GetPlayerObject(NetworkRunner.Instances[0].LocalPlayer).GetComponent<Player_Controller>();
        if(myPlayer != null) myPlayer.RPC_BanVatPham(1, 10);
    }
    #endregion

    #region HÀM TRỐNG BẮT BUỘC CỦA INTERFACE (Bỏ qua)
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