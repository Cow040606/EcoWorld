using UnityEngine;
using Fusion;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using static UnityEditor.Progress;



public struct DuLieuInput : INetworkInput 
{
    public Vector2 moveInput;
    public NetworkBool isJumpPressed;
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
        if (HasInputAuthority && Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                RPC_YeuCauNhatRac();
            }
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                if (InventoryManager.instance != null)
                {
                    // NÉM CÁI TÚI ĐỒ (TuiDo) VÀO TRONG HÀM NÀY:
                    InventoryManager.instance.BatTatBalo(TuiDo); 
                }
            }
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpPressedLocal = true;
            }
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out DuLieuInput data))
        {
            // Bây giờ hàm này chỉ lo mỗi việc di chuyển
            Vector3 move = data.moveInput.x * transform.right + data.moveInput.y * transform.forward;
            character.Move(move * speed * Runner.DeltaTime);
            vanTocRoi.y += gravity * Runner.DeltaTime;
            character.Move(vanTocRoi * Runner.DeltaTime);
        }
        if (character.isGrounded && vanTocRoi.y < 0)
            {
                vanTocRoi.y = -2f; 
            }
        if (character.isGrounded)
            {
                if (vanTocRoi.y < 0)
                {
                    vanTocRoi.y = -2f; // Ép xuống đất
                }

                // Nếu vừa chạm đất mà người chơi bấm phím Space -> Búng lên!
                if (data.isJumpPressed)
                {
                    vanTocRoi.y = jumpForce;
                }
            }
        
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {   
        
        var data = new DuLieuInput();
        data.moveInput = moveInputLocal;
        data.isJumpPressed = jumpPressedLocal;


        // Lúc mở balo không cho điều khiển 

        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo == true)
        {
            data.moveInput = Vector2.zero; // không cho di chuyển
            data.isJumpPressed = false; // không cho nhảy
        }
        else
        {
            data.moveInput = moveInputLocal;
        }

        //



        
        input.Set(data);
        jumpPressedLocal = false;
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

