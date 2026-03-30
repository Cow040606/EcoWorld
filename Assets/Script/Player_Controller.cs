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
            Runner.AddCallbacks(this); 
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
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                RPC_YeuCauNhatRac();
            }

            // 2. CHẠY NHANH
            sprintPressedLocal = Keyboard.current.leftShiftKey.isPressed;

            // 3. NHẢY
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpPressedLocal = true;
            }

            // 4. MỞ / ĐÓNG BALO BẰNG PHÍM B
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                if (InventoryManager.instance != null)
                {
                    InventoryManager.instance.BatTatBalo(TuiDo, this); 
                }
            }
            // 5. MỞ / ĐÓNG ESC BẰNG PHÍM ESC

            bool ESCDangMo = false;
            if (Keyboard.current.escapeKey.wasPressedThisFrame)            {
                if (ESC.instance != null)
                {
                    ESC.instance.BatTatESC();
                }
            }

            // 6. KIỂM TRA TRẠNG THÁI BALO ĐỂ XỬ LÝ CHUỘT
            bool baloDangMo = false;
            if (InventoryManager.instance != null || ESC.instance != null)
            {
                baloDangMo = InventoryManager.instance.trangThaiBalo;
                ESCDangMo = ESC.instance.isESC_Open;
            }

            if (baloDangMo == true || ESCDangMo == true)
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

            bool shopDangMo = false; // Thêm biến này

            if (InventoryManager.instance != null) baloDangMo = InventoryManager.instance.trangThaiBalo;
            if (ESC.instance != null) ESCDangMo = ESC.instance.isESC_Open;

            // KIỂM TRA THÊM: Tìm cái Shop_Panel của NPC xem có đang mở không
            GameObject shopObj = GameObject.Find("Shop_Panel"); // Hoặc dùng cách tham chiếu khác tốt hơn
            if (shopObj != null) shopDangMo = shopObj.activeSelf;

            // Cập nhật điều kiện: Nếu 1 trong 3 cái mở thì HIỆN CHUỘT
            if (baloDangMo || ESCDangMo || shopDangMo)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Ngắt không cho xoay Camera khi đang dùng chuột trong UI
                xRotation = xRotation;
                yRotation = yRotation;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // Chỉ xử lý xoay Camera khi không mở UI
                float mouseX = Mouse.current.delta.x.ReadValue() * mouseSensitivity;
                float mouseY = Mouse.current.delta.y.ReadValue() * mouseSensitivity;
                // ... code xoay camera tiếp theo của bạn ...
            }
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

        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo == true)
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_YeuCauNhatRac() 
    {
        Collider[] ketQuaQuet = Physics.OverlapSphere(transform.position, banKinhNhat);
        foreach (var Obj in ketQuaQuet)
        {
            if (Obj.CompareTag("Rac") || Obj.CompareTag("Wood"))
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

                    if (daNhat) {
                        RPC_XoaRacKhapBanDo(nObj); 
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
    public void RPC_BanVatPham(int id, int gia)
    {
        for (int i = 0; i < TuiDo.Length; i++)
        {
            if (TuiDo[i].ItemID == id && TuiDo[i].SoLuong > 0)
            {
                var item = TuiDo[i];
                item.SoLuong--;
                if (item.SoLuong <= 0) item.ItemID = 0;

                TuiDo.Set(i, item); // Cập nhật túi đồ
                Gold += gia;        // CỘNG XU TẠI ĐÂY
                return;
            }
        }
    }
    public void Click_NutBanGo()
    {
        // Tìm Player của mình (người đang mở UI)
        Player_Controller myPlayer = NetworkRunner.Instances[0].GetPlayerObject(NetworkRunner.Instances[0].LocalPlayer).GetComponent<Player_Controller>();

        if (myPlayer != null)
        {
            // Ví dụ: ID gỗ là 1, giá bán là 10 xu
            myPlayer.RPC_BanVatPham(1, 10);
        }
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