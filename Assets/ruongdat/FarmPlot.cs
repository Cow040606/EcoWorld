using Fusion;
using UnityEngine;

public class FarmPlot : NetworkBehaviour
{
    public enum PlotState { Normal, Tilled, Seeded, Grown }

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public PlotState CurrentState { get; set; }

    [Networked]
    public TickTimer growTimer { get; set; }

    [Header("--- Models ---")]
    [SerializeField] private GameObject modelDatThuong;
    [SerializeField] private GameObject modelDatCay;
    [SerializeField] private GameObject modelCayCon;
    [SerializeField] private GameObject modelCayLon;

    [Header("--- Farm Settings ---")]
    [SerializeField] private int seedItemID = 102;
    [SerializeField] private int fruitItemID = 201;
    [SerializeField] private int harvestCount = 3;
    [SerializeField] private float growTime = 10f;

    public override void Spawned()
    {
        OnStateChanged();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (CurrentState == PlotState.Seeded && growTimer.Expired(Runner))
        {
            CurrentState = PlotState.Grown;
            growTimer = TickTimer.None;
        }
    }

    // Tự động chạy trên mọi Client khi CurrentState thay đổi
    private void OnStateChanged()
    {
        if (modelDatThuong) modelDatThuong.SetActive(false);
        if (modelDatCay)    modelDatCay.SetActive(false);
        if (modelCayCon)    modelCayCon.SetActive(false);
        if (modelCayLon)    modelCayLon.SetActive(false);

        switch (CurrentState)
        {
            case PlotState.Normal:
                if (modelDatThuong) modelDatThuong.SetActive(true);
                break;
            case PlotState.Tilled:
                if (modelDatCay) modelDatCay.SetActive(true);
                break;
            case PlotState.Seeded:
                if (modelDatCay)  modelDatCay.SetActive(true);
                if (modelCayCon)  modelCayCon.SetActive(true);
                break;
            case PlotState.Grown:
                if (modelDatCay)  modelDatCay.SetActive(true);
                if (modelCayLon)  modelCayLon.SetActive(true);
                break;
        }
    }

    // -----------------------------------------------------------------------
    // RPC CÀY ĐẤT
    // -----------------------------------------------------------------------
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CayDat()
    {
        if (CurrentState == PlotState.Normal)
            CurrentState = PlotState.Tilled;
    }

    // -----------------------------------------------------------------------
    // RPC GIEO HẠT
    // -----------------------------------------------------------------------
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_GieoHat(PlayerRef nguoi)
    {
        if (CurrentState != PlotState.Tilled) return;

        var playerObj = Runner.GetPlayerObject(nguoi);
        if (playerObj == null) return;

        Player_Controller player = playerObj.GetComponent<Player_Controller>();
        if (player == null) return;

        // Tìm hạt giống trong TuiDo — dùng đúng tên field: ItemID và SoLuong
        bool hasSeed = false;
        for (int i = 0; i < player.TuiDo.Length; i++)
        {
            O_VatPham item = player.TuiDo[i];

            if (item.ItemID == seedItemID && item.SoLuong > 0)
            {
                item.SoLuong -= 1;
                if (item.SoLuong <= 0) item.ItemID = 0; // Xóa ô nếu hết hạt

                player.TuiDo.Set(i, item);
                hasSeed = true;
                break;
            }
        }

        if (hasSeed)
        {
            CurrentState = PlotState.Seeded;
            growTimer = TickTimer.CreateFromSeconds(Runner, growTime);
        }
    }

    // -----------------------------------------------------------------------
    // RPC THU HOẠCH
    // -----------------------------------------------------------------------
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ThuHoach(PlayerRef nguoi)
    {
        if (CurrentState != PlotState.Grown) return;

        var playerObj = Runner.GetPlayerObject(nguoi);
        if (playerObj == null) return;

        Player_Controller player = playerObj.GetComponent<Player_Controller>();
        if (player == null) return;

        bool added = false;

        // Bước 1: Tìm ô đã có quả này để cộng dồn (stack)
        for (int i = 0; i < player.TuiDo.Length; i++)
        {
            O_VatPham item = player.TuiDo[i];
            if (item.ItemID == fruitItemID)
            {
                item.SoLuong += harvestCount;
                player.TuiDo.Set(i, item);
                added = true;
                break;
            }
        }

        // Bước 2: Nếu chưa có ô nào, tìm ô trống
        if (!added)
        {
            for (int i = 0; i < player.TuiDo.Length; i++)
            {
                O_VatPham item = player.TuiDo[i];
                if (item.ItemID == 0)
                {
                    item.ItemID = fruitItemID;
                    item.SoLuong = harvestCount;
                    player.TuiDo.Set(i, item);
                    added = true;
                    break;
                }
            }
        }

        if (added)
        {
            CurrentState = PlotState.Normal; // Reset về đất thường
            Rpc_ThongBaoThuHoach(nguoi, harvestCount);
        }
    }

    // -----------------------------------------------------------------------
    // RPC THÔNG BÁO THU HOẠCH (chỉ bắn về đúng máy người thu hoạch)
    // -----------------------------------------------------------------------
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ThongBaoThuHoach([RpcTarget] PlayerRef targetPlayer, int soLuong)
    {
        if (ItemNotifyManager.Instance == null || InventoryManager.instance == null) return;

        Item thongTin = InventoryManager.instance.TraCuuItem(fruitItemID);
        if (thongTin != null)
            ItemNotifyManager.Instance.ShowNotify(thongTin.itemName, soLuong, thongTin.icon);
        else
            ItemNotifyManager.Instance.ShowNotify("Nông sản", soLuong, null);
    }
}