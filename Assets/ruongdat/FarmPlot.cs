using Fusion;
using UnityEngine;

public class FarmPlot : NetworkBehaviour
{
    public enum PlotState { Normal, Tilled, Seeded, Grown }

    // Dùng OnChangedRender trong Fusion 2 để cập nhật UI/Visual ở client
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
        // Khởi tạo visual ban đầu khi object được spawn trên mạng
        OnStateChanged();
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ Host/Server (người nắm StateAuthority) mới xử lý logic đếm thời gian phát triển
        if (!Object.HasStateAuthority) return;

        if (CurrentState == PlotState.Seeded && growTimer.Expired(Runner))
        {
            CurrentState = PlotState.Grown;
            // Tắt timer sau khi cây đã lớn
            growTimer = TickTimer.None; 
        }
    }

    // Hàm này tự động chạy trên mọi Client khi CurrentState thay đổi
    private void OnStateChanged()
    {
        // Tắt hết model trước khi bật cái đúng lên
        modelDatThuong.SetActive(false);
        modelDatCay.SetActive(false);
        modelCayCon.SetActive(false);
        modelCayLon.SetActive(false);

        switch (CurrentState)
        {
            case PlotState.Normal:
                modelDatThuong.SetActive(true);
                break;
            case PlotState.Tilled:
                modelDatCay.SetActive(true);
                break;
            case PlotState.Seeded:
                modelDatCay.SetActive(true);
                modelCayCon.SetActive(true);
                break;
            case PlotState.Grown:
                modelDatCay.SetActive(true);
                modelCayLon.SetActive(true);
                break;
        }
    }

    // Bất kỳ ai cũng có thể gọi lên StateAuthority để cày đất
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CayDat()
    {
        if (CurrentState == PlotState.Normal)
        {
            CurrentState = PlotState.Tilled;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_GieoHat(PlayerRef nguoi)
    {
        if (CurrentState != PlotState.Tilled) return;

        // Lấy Player_Controller của người gieo hạt
        var playerObj = Runner.GetPlayerObject(nguoi);
        if (playerObj == null) return;

        Player_Controller player = playerObj.GetComponent<Player_Controller>();
        if (player == null) return;

        // Tìm hạt giống trong TuiDo
        bool hasSeed = false;
        for (int i = 0; i < player.TuiDo.Length; i++)
        {
            var item = player.TuiDo[i];
            if (item.ID == seedItemID && item.SoLuong > 0)
            {
                // Trừ 1 hạt giống
                item.SoLuong -= 1;
                if (item.SoLuong <= 0) item.ID = 0; // Ô trống nếu hết hạt
                player.TuiDo.Set(i, item); // Lưu lại vào NetworkArray
                
                hasSeed = true;
                break; // Chỉ trừ 1 ô đầu tiên tìm thấy
            }
        }

        // Nếu có hạt giống thì tiến hành gieo
        if (hasSeed)
        {
            CurrentState = PlotState.Seeded;
            growTimer = TickTimer.CreateFromSeconds(Runner, growTime);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ThuHoach(PlayerRef nguoi)
    {
        if (CurrentState != PlotState.Grown) return;

        var playerObj = Runner.GetPlayerObject(nguoi);
        if (playerObj == null) return;

        Player_Controller player = playerObj.GetComponent<Player_Controller>();
        if (player == null) return;

        // LOGIC CỘNG ĐỒ (Giống RPC_YeuCauNhatRac)
        int remainingToGive = harvestCount;
        bool added = false;

        // 1. Tìm ô đã có sẵn quả này để cộng dồn (Stack)
        for (int i = 0; i < player.TuiDo.Length; i++)
        {
            var item = player.TuiDo[i];
            if (item.ID == fruitItemID) // (Giả định stack vô hạn, nếu có maxStack bạn thêm điều kiện nhé)
            {
                item.SoLuong += remainingToGive;
                player.TuiDo.Set(i, item);
                added = true;
                break;
            }
        }

        // 2. Nếu không có ô để cộng dồn, tìm ô trống (ID == 0)
        if (!added)
        {
            for (int i = 0; i < player.TuiDo.Length; i++)
            {
                var item = player.TuiDo[i];
                if (item.ID == 0) // Ô trống
                {
                    item.ID = fruitItemID;
                    item.SoLuong = remainingToGive;
                    player.TuiDo.Set(i, item);
                    added = true;
                    break;
                }
            }
        }

        // Nếu cộng thành công
        if (added)
        {
            CurrentState = PlotState.Normal; // Reset về đất thường
            
            // Bắn RPC về cục bộ người thu hoạch để hiện UI thông báo
            Rpc_ThongBaoThuHoach(nguoi, harvestCount); 
        }
    }

    // Dùng RpcTarget (tính năng của Fusion) để chỉ bắn RPC này về đúng máy của người thu hoạch
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ThongBaoThuHoach([RpcTarget] PlayerRef targetPlayer, int soLuong)
    {
        // Hiện UI thông báo (Chỉ chạy trên máy có PlayerRef tương ứng)
        if (ItemNotifyManager.Instance != null)
        {
            ItemNotifyManager.Instance.ShowNotify($"Thu hoạch thành công +{soLuong} quả!");
        }
    }
}