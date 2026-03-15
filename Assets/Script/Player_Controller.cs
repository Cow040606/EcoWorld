using UnityEngine;
using Fusion;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using static UnityEditor.Progress;



public struct DuLieuInput : INetworkInput 
{
    public Vector2 moveInput;
    public NetworkBool isJumpPressed;
    public float mouseX;
}

public struct O_VatPham : INetworkStruct
{
    public int ItemID;
    public int SoLuong; 
}

public class Player_Controller : NetworkBehaviour, INetworkRunnerCallbacks
{
    [SerializeField]
    
    [Header("Di chuyển")] CharacterController character;
    public float speed = 5f;
    private Vector2 moveInputLocal;
    [Header("Camera & Chuột")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.5f;
    private float xRotation = 0f;
    private float yRotation = 0f; // Thêm trục Y để xoay trái/phải tự do
    public float khoangCachCamera = 4f; // Khoảng cách từ cam đến lưng nhân vật
    private float mouseXLocalAcc;
    
    [Header("Nhặt vật phẩm")]
    public float banKinhNhat = 5f;
    private bool NutE = false;
    private bool _daNhatRac = false; 
    [Header("Trọng lực")]
    public float gravity = -9.81f; // Lực hút Trái Đất chuẩn
    private Vector3 vanTocRoi; // Biến để lưu trữ vận tốc rơi tự do
    public float jumpForce = 5f; // Lực nhảy (Số càng to nhảy càng cao)
    private bool jumpPressedLocal; // Lưu trạng thái bấm phím ở máy mình
    [Networked, Capacity(20)] 
    public NetworkArray<O_VatPham> TuiDo { get; }












    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            Runner.AddCallbacks(this); 
            // ĐÁ CAMERA RA NGOÀI ĐỂ NÓ ĐỘC LẬP, KHÔNG BỊ XOAY THEO THÂN HÌNH
            if (cameraTransform != null) cameraTransform.SetParent(null); 
        }
        else
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = false;
            
            AudioListener listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    













    void Update()
    {
        // GỘP CHUNG BÀN PHÍM VÀ CHUỘT VÀO 1 CÁI IF THÔI!
        if (HasInputAuthority && Keyboard.current != null && Mouse.current != null)
        {
            // 1. NHẶT ĐỒ
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                RPC_YeuCauNhatRac();
            }

            // 2. NHẢY
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpPressedLocal = true;
            }

            // 3. MỞ / ĐÓNG BALO BẰNG PHÍM B
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                if (InventoryManager.instance != null)
                {
                    InventoryManager.instance.BatTatBalo(TuiDo); 
                }
            }

            // ==========================================
            // 4. KIỂM TRA TRẠNG THÁI BALO ĐỂ XỬ LÝ CHUỘT
            // ==========================================
            bool baloDangMo = false;
            if (InventoryManager.instance != null)
            {
                baloDangMo = InventoryManager.instance.trangThaiBalo;
            }

            if (baloDangMo == true)
            {
                // MỞ BALO: Hiện trỏ chuột lên, KHÔNG cho xoay camera
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // ĐÓNG BALO: Giấu trỏ chuột đi
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // 1. Đọc dữ liệu chuột di chuyển
                float mouseX = Mouse.current.delta.x.ReadValue() * mouseSensitivity;
                float mouseY = Mouse.current.delta.y.ReadValue() * mouseSensitivity;

                // 2. Tính toán góc quay tự do (Cả Lên/Xuống và Trái/Phải)
                yRotation += mouseX;
                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -60f, 60f); 

                // 3. Ốp tọa độ để Camera bay theo đuôi nhân vật
                if (cameraTransform != null)
                {
                    Quaternion camRotation = Quaternion.Euler(xRotation, yRotation, 0f);
                    
                    // Điểm nhìn nhắm vào ngang vai nhân vật (Cộng thêm 1.5f chiều cao)
                    Vector3 diemNhin = transform.position + Vector3.up * 1.5f; 
                    
                    cameraTransform.position = diemNhin - (camRotation * Vector3.forward * khoangCachCamera);
                    cameraTransform.rotation = camRotation;
                }
            }
        }
    }













    
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out DuLieuInput data))
        {
            // 1. TÍNH TOÁN HƯỚNG DI CHUYỂN
            // Ép dữ liệu từ W A S D vào trục X và Z của thế giới 3D (không dùng transform.forward nữa)
            Vector3 huongDiChuyen = new Vector3(data.moveInput.x, 0f, data.moveInput.y);

            // 2. XOAY MẶT THEO HƯỚNG ĐI
            if (huongDiChuyen.magnitude >= 0.1f) // Nếu có bấm phím di chuyển
            {
                // Xác định góc quay mục tiêu
                Quaternion huongMucTieu = Quaternion.LookRotation(huongDiChuyen);
                
                // Dùng Slerp để nhân vật xoay mượt mà (chỉnh số 10f to hơn thì quay nhanh hơn)
                transform.rotation = Quaternion.Slerp(transform.rotation, huongMucTieu, Runner.DeltaTime * 10f);
            }

            // 3. DI CHUYỂN NHÂN VẬT THỰC TẾ
            character.Move(huongDiChuyen.normalized * speed * Runner.DeltaTime);
            
            // 4. XỬ LÝ TRỌNG LỰC
            vanTocRoi.y += gravity * Runner.DeltaTime;
            character.Move(vanTocRoi * Runner.DeltaTime);
        }
        
        // ==========================================
        // ĐOẠN DƯỚI NÀY GIỮ NGUYÊN (CHẠM ĐẤT VÀ NHẢY)
        // ==========================================
        if (character.isGrounded)
        {
            if (vanTocRoi.y < 0)
            {
                vanTocRoi.y = -2f; // Ép xuống đất
            }

            // Nếu vừa chạm đất mà người chơi bấm phím Space -> Búng lên!
            if (GetInput(out DuLieuInput dataJump) && dataJump.isJumpPressed)
            {
                vanTocRoi.y = jumpForce;
            }
        }
    }














    public void OnInput(NetworkRunner runner, NetworkInput input)
    {   
        var data = new DuLieuInput();

        // Lúc mở balo không cho điều khiển (Đúng chuẩn ý Bò luôn)
        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo == true)
        {
            data.moveInput = Vector2.zero; // không cho di chuyển
            data.isJumpPressed = false;    // không cho nhảy
            data.mouseX = 0f;              // KHÓA CỔ LUÔN
        }
        else
        {
            Vector3 huongChuanBiGui = Vector3.zero;
            if (cameraTransform != null)
            {
                // Lấy hướng Nhìn Thẳng và Nhìn Ngang của Camera hiện tại
                Vector3 camForward = cameraTransform.forward;
                Vector3 camRight = cameraTransform.right;
                
                // Bỏ trục Y đi để nhân vật không bị hướng bay lên trời
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                // Tính toán: Bấm W thì đi theo camForward, bấm D thì đi theo camRight
                huongChuanBiGui = camForward * moveInputLocal.y + camRight * moveInputLocal.x;
            }

            // Đóng gói cái hướng thật sự này gửi lên mạng
            data.moveInput = new Vector2(huongChuanBiGui.x, huongChuanBiGui.z);
            data.isJumpPressed = jumpPressedLocal;
        }

        input.Set(data);
        
        // Gửi xong thì dọn sạch hộp chứa tạm
        jumpPressedLocal = false;
        mouseXLocalAcc = 0f; 
    }













    public void OnMove(InputValue value)
    {
        moveInputLocal = value.Get<Vector2>();
    }












    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_YeuCauNhatRac() 
    {
        Collider[] ketQuaQuet = Physics.OverlapSphere(transform.position, banKinhNhat);
        foreach (var Obj in ketQuaQuet)
        {
            if (Obj.CompareTag("Rac"))
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
                            // Bò check lại file Item.cs xem Bò đặt tên biến là isStackable hay stackable nha
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
                        //Debug.Log("<color=green>Server: Nhặt thành công ID: </color>" + idThucTe);
                        break; 
                    }
                }
            }
        }
    }












    // --- CÁI LOA PHÁT THANH ĐỂ XÓA RÁC (THAY THẾ CHO CÁI CŨ) ---
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_XoaRacKhapBanDo(NetworkObject rac)
    {
        if (rac != null && rac.IsValid)
        {
            // Máy nào cũng tự động giấu đi cho mượt
            rac.gameObject.SetActive(false);

            // Chỉ Trưởng phòng (StateAuthority) mới được phép ra lệnh xóa thật
            if (rac.HasStateAuthority)
            {
                Runner.Despawn(rac);
            }
        }
    }














    // ==========================================================
    // BƯỚC 2: HOST PHÁT LỆNH CHO CẢ LÀNG (Tất cả máy đều chạy hàm này)
    // ==========================================================
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











































    // ==========================================================
    // GIẤU ĐỐNG NÀY ĐI BẰNG DẤU [-] CHO ĐỠ RÁC MẮT NHÉ BÒ
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

