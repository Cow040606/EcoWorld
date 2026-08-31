using Fusion;
using UnityEngine;

public class DroppedBackpack : NetworkBehaviour
{
    [Networked, Capacity(20)]
    public NetworkArray<O_VatPham> VatPhamDaRoi { get; }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_YeuCauNhatLaiBalo(Player_Controller nguoiNhat)
    {
        if (nguoiNhat == null || nguoiNhat.Object == null || !nguoiNhat.Object.IsValid) return;

        bool coVatPhamNhatDuoc = false;

        // Chuyển toàn bộ vật phẩm từ balo rơi vào túi đồ người nhặt
        for (int i = 0; i < VatPhamDaRoi.Length; i++)
        {
            if (VatPhamDaRoi[i].ItemID != 0 && VatPhamDaRoi[i].SoLuong > 0)
            {
                int itemID = VatPhamDaRoi[i].ItemID;
                int soLuong = VatPhamDaRoi[i].SoLuong;
                int upgradeLvl = VatPhamDaRoi[i].UpgradeLevel;

                // Thêm vào túi đồ người chơi (kèm level nâng cấp)
                if (nguoiNhat.ThemDoVaoTui(itemID, soLuong, upgradeLvl))
                {
                    // Đánh dấu ô này đã được nhặt thành công
                    VatPhamDaRoi.Set(i, new O_VatPham { ItemID = 0, SoLuong = 0, UpgradeLevel = 0 });
                    coVatPhamNhatDuoc = true;
                }
            }
        }

        // Kiểm tra xem balo đã trống hoàn toàn chưa
        bool daTrongHoanToan = true;
        for (int i = 0; i < VatPhamDaRoi.Length; i++)
        {
            if (VatPhamDaRoi[i].ItemID != 0 && VatPhamDaRoi[i].SoLuong > 0)
            {
                daTrongHoanToan = false;
                break;
            }
        }

        // Nếu trống hoàn toàn thì hủy balo rơi khỏi mạng
        if (daTrongHoanToan)
        {
            Runner.Despawn(Object);
        }
    }
}
