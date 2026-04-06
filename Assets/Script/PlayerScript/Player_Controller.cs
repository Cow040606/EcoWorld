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
            // 1. NHẶT ĐỒ
            if (Keyboard.current.eKey.wasPressedThisFrame) RPC_YeuCauNhatRac();

            // 2. CHẠY NHANH
            sprintPressedLocal = Keyboard.current.leftShiftKey.isPressed;

            // 3. NHẢY
            if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressedLocal = true;

            // --- 4. DI CHUYỂN WASD ---
            float trucX = 0f;
            float trucY = 0f;
            if (Keyboard.current.wKey.isPressed) trucY += 1f;
            if (Keyboard.current.sKey.isPressed) trucY -= 1f;
            if (Keyboard.current.dKey.isPressed) trucX += 1f;
            if (Keyboard.current.aKey.isPressed) trucX -= 1f;
            
            moveInputLocal = new Vector2(trucX, trucY).normalized; 

            // ========================================================= //
            // 5. MỞ / ĐÓNG CÁC BẢNG UI
            // ========================================================= //
            
            bool isChat = (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive);
            bool isShop = (ShopUIController.instance != null && ShopUIController.instance.isShopOpen);

            // Nút ESC là "Quyền lực tối thượng", lúc nào cũng cho bấm để thoát hiểm
            if (Keyboard.current.escapeKey.wasPressedThisFrame)            
            {
                TatToanBoUI(); 
                if (ESC.instance != null) ESC.instance.BatTatESC();
            }

            if (isChat == false && isShop == false)
            {
                // Bấm B: Mở / Đóng Balo
                if (Keyboard.current.bKey.wasPressedThisFrame)
                {
                    if (InventoryManager.instance != null) InventoryManager.instance.BatTatBalo(TuiDo, this); 
                }

                // Bấm Tab: Mở / Đóng Bảng nhiệm vụ
                if(Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    if (QuestManager.instance != null) QuestManager.instance.Battatbangnhiemvu();
                }
            }

            // ========================================================= //
            // 6. KIỂM TRA TRẠNG THÁI UI ĐỂ BẬT/TẮT CHUỘT
            // ========================================================= //
            
            // Lấy trạng thái thực tế từ các bản hack (Instance)
            bool baloDangMo = (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo);
            bool ishopopen = (ShopUIController.instance != null && ShopUIController.instance.isShopOpen);
            bool questDangMo = (QuestManager.instance != null && QuestManager.instance.isQuest_Open);
            bool IsChat = (DialogueEditor.ConversationManager.Instance != null && DialogueEditor.ConversationManager.Instance.IsConversationActive);
            bool ESCDangMo = (ESC.instance != null && ESC.instance.isESC_Open);

            // Nếu CÓ BẤT KỲ CÁI UI NÀO ĐANG MỞ -> Bật chuột, khóa xoay camera
            if (baloDangMo || ESCDangMo || ishopopen || IsChat || questDangMo)
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
            // Tắt phần hồn (Biến)
            ShopUIController.instance.isShopOpen = false;
            ShopUIController.instance.dangmoshop = false; 
            
            // ShopUIController.instance.CloseShop();
            
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
            Vector3 diemNhin = transform.position + Vector3.up * 1.5f; // Vị trí ngang đầu nhân vật
            Vector3 huongCamera = -(camRotation * Vector3.forward); // Hướng chỉ từ đầu ra sau lưng
            
            // 1. Tính toán vị trí xa nhất (4f) mà camera muốn tới
            Vector3 viTriDuKien = diemNhin + huongCamera * khoangCachCamera;
            
            // 2. BẮN TIA LASER TỪ ĐẦU NHÂN VẬT RA SAU LƯNG CAMERA
            // Nếu tia laser đụng trúng bức tường (nằm trong layerVaChamCamera)...
            if (Physics.Raycast(diemNhin, huongCamera, out RaycastHit hit, khoangCachCamera, layerVaChamCamera))
            {
                // ...thì kéo Camera tới ngay điểm va chạm, đẩy nhẹ ra 0.1f để không cạ sát tường
                cameraTransform.position = hit.point + hit.normal * 0.1f; 
            }
            else
            {
                // Không đụng gì thì cứ đứng ở vị trí xa nhất
                cameraTransform.position = viTriDuKien;
            }
            
            cameraTransform.rotation = camRotation;
        }
    }

    // =======================================================
    // TRÁI TIM CỦA GAME MẠNG NẰM Ở ĐÂY NÈ BÒ!
    // =======================================================
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority && !HasInputAuthority)
            return;

        if (GetInput(out DuLieuInput data))
        {
            // 1. SỬA LỖI Ở ĐÂY: Đổi IsGrounded thành Grounded
            if (data.isJumpPressed && character.Grounded)
            {
                if (dongHoChoNhay.ExpiredOrNotRunning(Runner)) 
                {
                    character.Jump(); // Thằng NCC tự động tính lực búng lên
                    isJumping = true; 
                    dongHoChoNhay = TickTimer.CreateFromSeconds(Runner, thoiGianHoiNhay);
                }
            }
            else if (character.Grounded)
            {
                isJumping = false;
            }

            // 2. XỬ LÝ DI CHUYỂN
            Vector3 huongDiChuyen = new Vector3(data.moveInput.x, 0f, data.moveInput.y);
            float tocDoHienTai = data.isRunfast ? runfast : speed; 

            isrun = data.moveInput.magnitude > 0.1f;
            isSprinting = isrun && data.isRunfast;

            if (huongDiChuyen.magnitude >= 0.1f) 
            {
                character.maxSpeed = tocDoHienTai; 
                // Đi thẳng
                character.Move(huongDiChuyen.normalized);
                Quaternion huongMucTieu = Quaternion.LookRotation(huongDiChuyen);
                transform.rotation = Quaternion.Slerp(transform.rotation, huongMucTieu, Runner.DeltaTime * 15f); 
            }
            else
            {
                // Truyền Vector zero để nhân vật phanh lại
                character.Move(Vector3.zero);
            }
        }
    }

    public override void Render()
    {
        if (animator != null)
        {
            if(isJumping)
            {
                isSprinting = false;
                isrun = false;
                animator.SetBool("isJump", isJumping);
            }
            else if(!isJumping) animator.SetBool("isJump", false);
            
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
        //Debug.Log($"[CLIENT] Đã nhận được lệnh hiện UI cho Item ID: {itemID_ServerGui}");

        if (InventoryManager.instance == null)
        {
            //Debug.LogError("Lỗi: InventoryManager.instance đang bị NULL! Bò chưa gắn script hoặc chưa gán Instance ở Awake.");
            return;
        }

        Item thongTinItem = InventoryManager.instance.TraCuuItem(itemID_ServerGui);
        
        if (thongTinItem == null)
        {
            //Debug.LogError($"Lỗi: Không tìm thấy vật phẩm nào có ID {itemID_ServerGui} trong Database!");
            return;
        }

        if (ItemNotifyManager.Instance == null)
        {
            //Debug.LogError("Lỗi: ItemNotifyManager.Instance đang bị NULL! Quên bật Object chứa script này à?");
            return;
        }

        //Debug.Log($"[CLIENT] Đang gọi UI hiện thị: {thongTinItem.itemName}");
        
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
                        if (thongTin != null) 
                        {
                            isstack = thongTin.stackable; 
                        }
                    }
                    if(isstack)
                    for (int i = 0; i < TuiDo.Length; i++) {
                        if (TuiDo[i].ItemID == idThucTe) {
                            O_VatPham doVat = TuiDo[i];
                            doVat.SoLuong++;
                            TuiDo.Set(i, doVat);
                            daNhat = true;
                            break;
                        }
                    }

                    if (!daNhat) {
                        for (int i = 0; i < TuiDo.Length; i++) {
                            if (TuiDo[i].ItemID == 0) { 
                                TuiDo.Set(i, new O_VatPham { ItemID = idThucTe, SoLuong = 1 });
                                daNhat = true;
                                break;
                            }
                        }
                    }

                    if (daNhat) 
                    {
                        RPC_XoaRacKhapBanDo(nObj); 
                        
                        // TRUYỀN BIẾN VÀO ĐÂY NÈ BÒ:
                        Rpc_NotifyPickupClient(idThucTe, 1); 
                        
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
    public void RPC_ThayDoiTien(int soTien)
    {
        // Server sẽ trực tiếp cộng/trừ vào biến Networked
        Gold += soTien;

        // Tránh để tiền bị âm (nếu Bò muốn)
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
        // 1. Lục lọi trong túi đồ xem có món này không
        for (int i = 0; i < TuiDo.Length; i++)
        {
            // Nếu tìm thấy đúng ID và số lượng lớn hơn 0
            if (TuiDo[i].ItemID == idBan && TuiDo[i].SoLuong > 0)
            {
                // 2. Lấy món đồ ra, trừ đi 1
                var doVat = TuiDo[i];
                doVat.SoLuong -= 1;
                
                // Nếu bán hết sạch thì xóa luôn ID để ô đó thành ô trống
                if (doVat.SoLuong <= 0) 
                {
                    doVat.ItemID = 0;
                }
                
                // Cất lại vào túi đồ mạng (Cú pháp chuẩn của Fusion)
                TuiDo.Set(i, doVat); 

                // 3. Cộng tiền vào ví
                Gold += giaBan;
                
                Debug.Log($"[Server] Đã bán 1 cái (ID: {idBan}), thu về {giaBan} Xu. Tiền hiện tại: {Gold}");
                return; // Bán xong 1 cái thì thoát hàm luôn để không bị trừ lố
            }
        }
        
        Debug.Log("[Server] Bán thất bại: Túi đồ không có món này!");
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_MuaVatPham(int idMatHang, int giaTien)
    {
        // 1. Kiểm tra xem túi có đủ Xu không?
        if (Gold < giaTien) 
        {
            return; 
        }

        // 2. Tra từ điển xem món này có xếp chồng (stack) được không
        if (InventoryManager.instance == null) return;
        
        Item thongTin = InventoryManager.instance.TraCuuItem(idMatHang);
        if (thongTin == null) return;

        bool daNhetVaoTui = false;
        bool isstack = thongTin.stackable;

        // 3. BẮT ĐẦU NHÉT ĐỒ VÀO TÚI
        if (isstack)
        {
            // Nếu đồ xếp chồng được, tìm xem trong túi có ô nào đang chứa món này không
            for (int i = 0; i < TuiDo.Length; i++) 
            {
                if (TuiDo[i].ItemID == idMatHang) 
                {
                    O_VatPham doVat = TuiDo[i];
                    doVat.SoLuong++;       // Tăng số lượng lên 1
                    TuiDo.Set(i, doVat);   // Lưu lại vào mạng
                    daNhetVaoTui = true;
                    break;
                }
            }
        }

        // Nếu đồ KHÔNG xếp chồng được HOẶC trong túi chưa có món này -> Tìm ô trống
        if (!daNhetVaoTui) 
        {
            for (int i = 0; i < TuiDo.Length; i++) 
            {
                if (TuiDo[i].ItemID == 0) // Ô có ID = 0 nghĩa là ô đang trống
                { 
                    // Nhét đồ mới tinh vào ô trống này
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


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_HoanThanhQuest(int idVatPham, int soLuongCanTru, int tienThuong)
    {
        int soLuongDaTru = 0;

        // 1. Quét balo để tìm và trừ đồ nhiệm vụ
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == idVatPham && TuiDo[i].SoLuong > 0)
            {
                var doVat = TuiDo[i];
                
                // Tính xem ô này có đủ để trừ không, hay chỉ trừ được 1 phần
                int soLuongCoTheTru = Mathf.Min(doVat.SoLuong, soLuongCanTru - soLuongDaTru);
                
                doVat.SoLuong -= soLuongCoTheTru;
                soLuongDaTru += soLuongCoTheTru;

                // Nếu trừ xong mà ô đó về 0 thì làm rỗng ô đó luôn
                if (doVat.SoLuong <= 0)
                {
                    doVat.ItemID = 0; 
                }

                TuiDo.Set(i, doVat); // Cập nhật lại Balo lên mạng

                // Nếu đã gom đủ số lượng cần thiết thì dừng quét
                if (soLuongDaTru >= soLuongCanTru)
                {
                    break;
                }
            }
        }

        // 2. Cộng tiền thưởng vào ví
        Gold += tienThuong;
        Debug.Log($"[Server] Trả Quest thành công! Trừ {soLuongCanTru} món (ID: {idVatPham}), Thưởng {tienThuong} Vàng. Tiền: {Gold}");
    }


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