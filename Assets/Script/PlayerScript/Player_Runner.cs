using Fusion;
using UnityEngine;

// BẮT BUỘC phải đổi lại thành NetworkBehaviour để dùng được hàm Spawned()
public class Player_Runner : NetworkBehaviour, IPlayerJoined
{
    [SerializeField] NetworkPrefabRef playerPrefab; 
    public GameObject spawn;
    
    // ==========================================
    // 1. HÀM CHẠY KHI SCENE VỪA LOAD XONG (CHỮA BỆNH LỠ ĐÒ)
    // ==========================================
    public override void Spawned()
    {
        // Chỉ Host (StateAuthority) mới được quyền đẻ
        if (Object.HasStateAuthority)
        {
            // Quét xem trong phòng lúc này có những ai đã vào sẵn rồi (Thường là Host)
            foreach (var player in Runner.ActivePlayers)
            {
                ThucHienDeNhanVat(player);
            }

        }
    }

    // ==========================================
    // 2. HÀM CHẠY KHI CÓ KHÁCH (CLIENT) VÀO SAU
    // ==========================================
    public void PlayerJoined(PlayerRef player)
    {
        // Khi có đứa bạn vào sau, Host sẽ thấy và đẻ cho nó
        if (Object.HasStateAuthority)
        {
            ThucHienDeNhanVat(player);
        }
    }

    // ==========================================
    // 3. LOGIC ĐẺ NHÂN VẬT GOM CHUNG CHO GỌN
    // ==========================================
    private void ThucHienDeNhanVat(PlayerRef chuSohuu)
    {
        Vector3 vitrispawn = spawn != null ? spawn.transform.position : Vector3.up;
            
        // Đẻ nhân vật và giao chìa khóa điều khiển cho đúng người
        Runner.Spawn(playerPrefab, vitrispawn, Quaternion.identity, chuSohuu);
        Debug.Log($"<color=cyan>Server thông báo:</color> Đã đẻ Player thành công cho ID: {chuSohuu.PlayerId}");
    }
}