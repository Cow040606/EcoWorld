using Fusion;
using UnityEngine;
using System.Collections.Generic; 

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

    // ==========================================
    // KHI CÓ NGƯỜI VÀO GAME
    // ==========================================
    public void PlayerJoined(PlayerRef player)
    {
        // 1. Chỉ Host mới có quyền đẻ nhân vật
        if (Object.HasStateAuthority)
        {
            ThucHienDeNhanVat(player);
        }

        // 2. THÔNG BÁO CHAT (Nằm ngoài if để máy ai cũng hiện)
        // ⚠️ BÒ LƯU Ý: Đổi tên "ChatManager.Instance.HienThiTinNhan" thành đúng cái script Chat của Bò nhé!
        // if (ChatManager.Instance != null)
        // {
        //     ChatManager.Instance.HienThiTinNhan($"<color=yellow>Người chơi {player.PlayerId} vừa tham gia server!</color>");
        // }
    }

    // ==========================================
    // KHI CÓ NGƯỜI THOÁT GAME (DỌN RÁC)
    // ==========================================
    public void PlayerLeft(PlayerRef player)
    {
        // 1. Chỉ Host mới có quyền dọn xác
        if (Object.HasStateAuthority)
        {
            if (danhSachDaDe.TryGetValue(player, out NetworkObject caiXac))
            {
                // Xóa sổ cái xác nó trên map
                Runner.Despawn(caiXac);
                // Gạch tên nó khỏi sổ
                danhSachDaDe.Remove(player);
                Debug.Log($"<color=yellow>Server thông báo:</color> ID {player.PlayerId} đã out, dọn dẹp xác an toàn!");
            }
        }

        // 2. THÔNG BÁO CHAT (Nằm ngoài if để máy ai cũng hiện)
        // if (ChatManager.Instance != null)
        // {
        //     ChatManager.Instance.HienThiTinNhan($"<color=gray>Người chơi {player.PlayerId} đã rời đi!</color>");
        // }
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
        
        // Đề phòng lỗi Fusion chưa đẻ kịp, check khác null mới ghi vào sổ
        if (nhanVatMoi != null)
        {
            // Ghi tên nó vào sổ (ID của nó + Cái xác vừa đẻ)
            danhSachDaDe.Add(chuSohuu, nhanVatMoi);
            //Debug.Log($"<color=cyan>Server thông báo:</color> Đã đẻ Player thành công cho ID: {chuSohuu.PlayerId}");
        }
    }
}