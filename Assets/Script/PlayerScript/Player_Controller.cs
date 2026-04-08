using UnityEngine;
using Fusion;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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
    [Header("Di chuyển")]
    public NetworkCharacterController character;
    public float speed = 5f;
    public float runfast = 15f;
    private Vector2 moveInputLocal;
    private bool sprintPressedLocal;

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

    [Header("Kinh tế")]
    [Networked] public int Gold { get; set; }
    [Networked] public bool isJumping { get; set; }
    private bool jumpPressedLocal;
    public float thoiGianHoiNhay = 1f; // Đổi thành 1 giây cho dễ test
    [Networked] public TickTimer dongHoChoNhay { get; set; }

    [Networked, Capacity(20)]
    public NetworkArray<O_VatPham> TuiDo { get; }

    [Networked] private NetworkBool isrun { get; set; }
    [Networked] private NetworkBool isSprinting { get; set; }

    private Animator animator;


    //Balo//
    [Networked, Capacity(4)]
    public NetworkArray<int> HotbarIDs { get; } // Lưu ItemID của phím 1, 2, 3, 4

    //        ======
    // === BỔ SUNG CHO HỆ THỐNG TRỒNG TRỌT (BIẾN MẠNG) ===
    //        ======
    [Networked, OnChangedRender(nameof(OnToolChanged))]
    public int CurrentToolIndex { get; set; }

    [Header("Hệ Thống Hiển Thị Công Cụ")]
    // Mảng này sẽ chứa các object model trên tay. 
    // Vị trí 0 = Tay không, Vị trí 1 = Cuốc, Vị trí 4 = Hạt giống...
    public GameObject[] toolModels;
    //        ======

    public override void Spawned()
    {
        animator = GetComponent<Animator>();
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
            {
                cameraTransform = Camera.main.transform;
            }
            Gold = 10000;
            TuiDo.Set(0, new O_VatPham { ItemID = 101, SoLuong = 50 });
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

    void Update()
    {
        if (HasInputAuthority && Keyboard.current != null && Mouse.current != null)
        {
            // ========================================================= //
            // 1. ĐỌC TRẠNG THÁI BẢNG UI (Phải đọc đầu tiên để ở dưới còn dùng)
            // ========================================================= //
            bool baloDangMo = (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo);
            bool ishopopen = (ShopUIController.instance != null && ShopUIController.instance.isShopOpen);
            bool questDangMo = (QuestManager.instance != null && QuestManager.instance.isQuest_Open);
            bool IsChatAct = (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive);
            bool ESCDangMo = (ESC.instance != null && ESC.instance.isESC_Open);

            // ========================================================= //
            // 2. QUẢN LÝ CHUỘT & CAMERA (Khóa/Mở chuột theo UI)
            // ========================================================= //
            if (baloDangMo || ESCDangMo || ishopopen || IsChatAct || questDangMo)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                float mouseX = Mouse.current.delta.x.ReadValue() * mouseSensitivity;
                float mouseY = Mouse.current.delta.y.ReadValue() * mouseSensitivity;

                yRotation += mouseX;
                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -60f, 60f);
            }

            // ========================================================= //
            // 3. NÚT ESC (Quyền lực tối thượng - Tắt mọi thứ)
            // ========================================================= //
            if (Keyboard.current.escapeKey.wasPressedThisFrame)            
            {
                TatToanBoUI(); 
                if (ESC.instance != null) ESC.instance.BatTatESC();
            }

            // ========================================================= //
            // 4. MỞ / ĐÓNG BALO VÀ NHIỆM VỤ (Chặn khi đang Chat/Shop)
            // ========================================================= //
            if (!IsChatAct && !ishopopen)
            {
                if (Keyboard.current.bKey.wasPressedThisFrame && InventoryManager.instance != null)
                {
                    InventoryManager.instance.BatTatBalo(TuiDo, this); 
                }

                if(Keyboard.current.tabKey.wasPressedThisFrame && QuestManager.instance != null)
                {
                    QuestManager.instance.Battatbangnhiemvu();
                }
            }

            // ========================================================= //
            // 5. HỆ THỐNG HOTBAR (GÁN ĐỒ & RÚT ĐỒ)
            // ========================================================= //
            bool dangBam1 = Keyboard.current.digit1Key.wasPressedThisFrame;
            bool dangBam2 = Keyboard.current.digit2Key.wasPressedThisFrame;
            bool dangBam3 = Keyboard.current.digit3Key.wasPressedThisFrame;
            bool dangBam4 = Keyboard.current.digit4Key.wasPressedThisFrame;

            if (baloDangMo)
            {
                // TRƯỜNG HỢP 1: ĐANG MỞ BALO -> GÁN ĐỒ
                if (ItemHover.itemID_DangDiChuot != 0) // Đã sửa thành ItemHover cho Bò
                {
                    if (dangBam1) RPC_GanVaoHotbar(0, ItemHover.itemID_DangDiChuot);
                    if (dangBam2) RPC_GanVaoHotbar(1, ItemHover.itemID_DangDiChuot);
                    if (dangBam3) RPC_GanVaoHotbar(2, ItemHover.itemID_DangDiChuot);
                    if (dangBam4) RPC_GanVaoHotbar(3, ItemHover.itemID_DangDiChuot);
                }
            }
            else if (!IsChatAct && !ishopopen && !ESCDangMo && !questDangMo)
            {
                // TRƯỜNG HỢP 2: KHÔNG MỞ UI NÀO CẢ -> RÚT ĐỒ
                if (dangBam1) RPC_EquipTool(0); 
                if (dangBam2) RPC_EquipTool(1); 
                if (dangBam3) RPC_EquipTool(2); 
                if (dangBam4) RPC_EquipTool(3); 
            }

            // ========================================================= //
            // 6. CÁC HÀNH ĐỘNG CƠ BẢN (DI CHUYỂN, NHẶT ĐỒ)
            // ========================================================= //
            // Nhặt đồ
            if (Keyboard.current.eKey.wasPressedThisFrame) RPC_YeuCauNhatRac();

            // Chạy nhanh & Nhảy
            sprintPressedLocal = Keyboard.current.leftShiftKey.isPressed;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressedLocal = true;

            // Đọc trục WASD
            float trucX = 0f;
            float trucY = 0f;
            if (Keyboard.current.wKey.isPressed) trucY += 1f;
            if (Keyboard.current.sKey.isPressed) trucY -= 1f;
            if (Keyboard.current.dKey.isPressed) trucX += 1f;
            if (Keyboard.current.aKey.isPressed) trucX -= 1f;

            moveInputLocal = new Vector2(trucX, trucY).normalized;

            // (Test) Cộng trừ tiền
            if (Keyboard.current.kKey.wasPressedThisFrame) RPC_ThayDoiTien(5);
            if (Keyboard.current.lKey.wasPressedThisFrame) RPC_ThayDoiTien(-5);
        }
    }

    private void TatToanBoUI()
    {
        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo == true)
        {
            InventoryManager.instance.BatTatBalo(TuiDo, this);
        }

        if (QuestManager.instance != null && QuestManager.instance.isQuest_Open == true)
        {
            QuestManager.instance.Battatbangnhiemvu();
        }
        
        if (ShopUIController.instance != null && ShopUIController.instance.isShopOpen == true) 
        {
            ShopUIController.instance.isShopOpen = false;
            ShopUIController.instance.dangmoshop = false; 
            ShopUIController.instance.khungShop.SetActive(false); 
        }

        if (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive)
        {
            DialogueEditor.ConversationManager.Instance.EndConversation();
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

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority && !HasInputAuthority)
            return;

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

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new DuLieuInput();
        if (!HasInputAuthority) return;

        data.isJumpPressed = jumpPressedLocal;
        bool baloDangMo = false;
        bool ESCDangMo = false;
        bool ishopopen = false;
        bool IsChat = false;

        if (InventoryManager.instance != null) baloDangMo = InventoryManager.instance.trangThaiBalo;
        if (ESC.instance != null) ESCDangMo = ESC.instance.isESC_Open;
        if (ShopUIController.instance != null) ishopopen = ShopUIController.instance.isShopOpen;
        if (DialogueEditor.ConversationManager.Instance != null)
        {
            IsChat = DialogueEditor.ConversationManager.Instance.IsConversationActive;
        }

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

                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

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

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_NotifyPickupClient(int itemID_ServerGui, int soLuong_ServerGui)
    {
        if (InventoryManager.instance == null) return;
        Item thongTinItem = InventoryManager.instance.TraCuuItem(itemID_ServerGui);
        if (thongTinItem == null || ItemNotifyManager.Instance == null) return;

        ItemNotifyManager.Instance.ShowNotify(
            thongTinItem.itemName,
            soLuong_ServerGui,
            thongTinItem.icon
        );
        if (Player_QuestManager.localQuest != null)
        {
            Player_QuestManager.localQuest.KiemTraTienDo();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_YeuCauNhatRac()
    {
        // Quét các vật thể xung quanh nhân vật
        Collider[] ketQuaQuet = Physics.OverlapSphere(transform.position, banKinhNhat);

        foreach (var Obj in ketQuaQuet)
        {
            // Kiểm tra xem có trúng 1 trong 3 Tag mới không
            if (Obj.CompareTag("Normal_Item") || Obj.CompareTag("Medium_Item") || Obj.CompareTag("Health_Item"))
            {
                NetworkObject nObj = Obj.GetComponent<NetworkObject>();
<<<<<<< HEAD:Assets/Script/PlayerScript/Player_Controller.cs
                XuLyItem theCanCuoc = Obj.GetComponent<XuLyItem>();

                if (nObj != null && nObj.IsValid && theCanCuoc != null && theCanCuoc.thongTinDoVat != null)
                {
                    int idThucTe = theCanCuoc.thongTinDoVat.itemID;
=======
                // Sử dụng ItemObject thay vì XuLyItem cho khớp với script mới
                ItemObject scriptItem = Obj.GetComponent<ItemObject>();

                if (nObj != null && nObj.IsValid && scriptItem != null)
                {
                    int idThucTe = scriptItem.itemID;
>>>>>>> b21cef80d2ff7bf17b1ec303653a39f80eb7cd7e:Assets/Script/Player_Controller.cs
                    bool daNhat = false;
                    bool isstack = true;

                    if (InventoryManager.instance != null)
                    {
                        Item thongTin = InventoryManager.instance.TraCuuItem(idThucTe);
<<<<<<< HEAD:Assets/Script/PlayerScript/Player_Controller.cs
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
                                daNhat = true;
                                break;
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
=======
                        if (thongTin != null)
                        {
                            isstack = thongTin.stackable;
                        }
                    }

                    // Logic cộng dồn (Stack)
                    if (isstack)
                    {
                        for (int i = 0; i < TuiDo.Length; i++)
                        {
                            if (TuiDo[i].ItemID == idThucTe)
                            {
                                O_VatPham doVat = TuiDo[i];
                                doVat.SoLuong += scriptItem.soLuong; // Cộng theo số lượng của item đó
                                TuiDo.Set(i, doVat);
>>>>>>> b21cef80d2ff7bf17b1ec303653a39f80eb7cd7e:Assets/Script/Player_Controller.cs
                                daNhat = true;
                                break;
                            }
                        }
                    }

<<<<<<< HEAD:Assets/Script/PlayerScript/Player_Controller.cs
                    if (daNhat)
                    {
                        RPC_XoaRacKhapBanDo(nObj);
                        Rpc_NotifyPickupClient(idThucTe, 1);
=======
                    // Nếu chưa nhặt được (ô mới)
                    if (!daNhat)
                    {
                        for (int i = 0; i < TuiDo.Length; i++)
                        {
                            if (TuiDo[i].ItemID == 0)
                            {
                                TuiDo.Set(i, new O_VatPham { ItemID = idThucTe, SoLuong = scriptItem.soLuong });
                                daNhat = true;
                                break;
                            }
                        }
                    }

                    if (daNhat)
                    {
                        // Xóa vật thể trên mạng
                        RPC_XoaRacKhapBanDo(nObj);

                        // Thông báo cho Client (Hiển thị UI hoặc âm thanh nhặt đồ)
                        Rpc_NotifyPickupClient(idThucTe, scriptItem.soLuong);

>>>>>>> b21cef80d2ff7bf17b1ec303653a39f80eb7cd7e:Assets/Script/Player_Controller.cs
                        break;
                    }
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_XoaRacKhapBanDo(NetworkObject rac)
    {
        if (rac != null && rac.IsValid)
        {
            rac.gameObject.SetActive(false);
            if (rac.HasStateAuthority)
            {
                Runner.Despawn(rac);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_GanVaoHotbar(int slotIndex, int itemID)
    {
        if (InventoryManager.instance != null)
        {
            Item thongTinItem = InventoryManager.instance.TraCuuItem(itemID);
            if (thongTinItem == null) return;
        }

        HotbarIDs.Set(slotIndex, itemID);
        Debug.Log($"[Server] Đã gán Item {itemID} vào phím số {slotIndex + 1}");
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
            if (HasStateAuthority)
            {
                Runner.Despawn(rac);
            }
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
                
                if (doVat.SoLuong <= 0) 
                {
                    doVat.ItemID = 0;
                }
                
                TuiDo.Set(i, doVat); 
                Gold += giaBan;
                
                Debug.Log($"[Server] Đã bán 1 cái (ID: {idBan}), thu về {giaBan} Xu. Tiền hiện tại: {Gold}");
                return; 
            }
        }
        Debug.Log("[Server] Bán thất bại: Túi đồ không có món này!");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_MuaVatPham(int idMatHang, int giaTien)
    {
        if (Gold < giaTien) return;
        if (InventoryManager.instance == null) return;

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
                    daNhetVaoTui = true;
                    break;
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
                    daNhetVaoTui = true;
                    break;
                }
            }
        }

        if (daNhetVaoTui)
        {
            Gold -= giaTien;
        }
    }

    public void Click_NutBanGo()
    {
        Player_Controller myPlayer = NetworkRunner.Instances[0].GetPlayerObject(NetworkRunner.Instances[0].LocalPlayer).GetComponent<Player_Controller>();
        if(myPlayer != null) myPlayer.RPC_BanVatPham(1, 10);
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

                if (doVat.SoLuong <= 0)
                {
                    doVat.ItemID = 0; 
                }

                TuiDo.Set(i, doVat); 

                if (soLuongDaTru >= soLuongCanTru)
                {
                    break;
                }
            }
        }

        Gold += tienThuong;
        Debug.Log($"[Server] Trả Quest thành công! Trừ {soLuongCanTru} món (ID: {idVatPham}), Thưởng {tienThuong} Vàng. Tiền: {Gold}");
    }

    //        ======
    // === BỔ SUNG CHO HỆ THỐNG TRỒNG TRỌT (HÀM XỬ LÝ RPC) ===
    //        ======
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_EquipTool(int toolIndex)
    {
        CurrentToolIndex = toolIndex;
    }

    private void OnToolChanged()
    {
        // 1. Nếu mảng công cụ chưa được gán trên Inspector thì bỏ qua để tránh lỗi Null
        if (toolModels == null || toolModels.Length == 0) return;

        // 2. Tắt TẤT CẢ các công cụ đang cầm trên tay
        for (int i = 0; i < toolModels.Length; i++)
        {
            if (toolModels[i] != null)
            {
                toolModels[i].SetActive(false);
            }
        }

        // 3. Chỉ bật lên đúng cái công cụ đang được chọn (Dựa vào CurrentToolIndex)
        if (CurrentToolIndex >= 0 && CurrentToolIndex < toolModels.Length)
        {
            if (toolModels[CurrentToolIndex] != null)
            {
                toolModels[CurrentToolIndex].SetActive(true);
            }
        }

        // Tạm thời comment lại để không bị lỗi đỏ do chưa có UI_HotBar
        // if (HasInputAuthority && UI_HotBar.Instance != null)
        // {
        //     UI_HotBar.Instance.HighlightSlot(CurrentToolIndex);
        // }
    }
    //        ======

    #region Hàm trống bắt buộc của Interface
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