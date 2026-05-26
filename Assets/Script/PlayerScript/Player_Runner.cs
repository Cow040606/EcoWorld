using Fusion;
using UnityEngine;
using System.Collections.Generic; // Bắt buộc phải có thư viện này để dùng Sổ Hộ Khẩu (Dictionary)

public class Player_Runner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] NetworkPrefabRef playerPrefab; 
    public GameObject spawn;
    
    // ==========================================
    // SỔ HỘ KHẨU: Theo dõi ID nào đã đẻ nhân vật nào
    // ==========================================
    private Dictionary<PlayerRef, NetworkObject> danhSachDaDe = new Dictionary<PlayerRef, NetworkObject>();

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            foreach (var player in Runner.ActivePlayers)
            {
                ThucHienDeNhanVat(player);
            }
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (Object.HasStateAuthority)
        {
            ThucHienDeNhanVat(player);
        }
    }

    // ==========================================
    // MỚI: DỌN RÁC KHI CÓ NGƯỜI THOÁT GAME
    // ==========================================
    public void PlayerLeft(PlayerRef player)
    {
        // Nếu Host thấy có đứa thoát, và thằng đó có trong sổ
        if (Object.HasStateAuthority && danhSachDaDe.TryGetValue(player, out NetworkObject caiXac))
        {
            // Xóa sổ cái xác nó trên map
            Runner.Despawn(caiXac);
            // Gạch tên nó khỏi sổ
            danhSachDaDe.Remove(player);
            //Debug.Log($"<color=yellow>Server thông báo:</color> ID {player.PlayerId} đã out, dọn dẹp xác!");
        }
    }

    private void ThucHienDeNhanVat(PlayerRef chuSohuu)
    {
        // KIỂM TRA TƯỜNG LỬA: Nếu trong sổ ĐÃ CÓ tên thằng này rồi -> QUAY XE! Không đẻ nữa!
        if (danhSachDaDe.ContainsKey(chuSohuu))
        {
            return;
        }

        Vector3 vitrispawn = spawn != null ? spawn.transform.position : Vector3.up;
            
        // Đẻ ra và lưu luôn cái NetworkObject đó vào một biến
        NetworkObject nhanVatMoi = Runner.Spawn(playerPrefab, vitrispawn, Quaternion.identity, chuSohuu);
        
        // Ghi tên nó vào sổ (ID của nó + Cái xác vừa đẻ)
        danhSachDaDe.Add(chuSohuu, nhanVatMoi);
        
        //Debug.Log($"<color=cyan>Server thông báo:</color> Đã đẻ Player thành công cho ID: {chuSohuu.PlayerId}");
    }
}