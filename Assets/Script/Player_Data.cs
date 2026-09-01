using Fusion;
using UnityEngine;

public class Player_Data : NetworkBehaviour
{
    [Networked] 
    public NetworkString<_32> tenTrenMang { get; set; }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            // Lấy tên đã lưu từ Menu
            string tenHienTai = PlayerPrefs.GetString("TenNhanVat", "VoDanh");
            RPC_SetPlayerName(tenHienTai);
        }
    }

    // Đổi thành PUBLIC để EventManager gọi được
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetPlayerName(string tenMoi)
    {
        tenTrenMang = tenMoi;
        // Debug.Log("Server đã nhận tên mới: " + tenMoi);
    }
}