using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class FarmPlot : NetworkBehaviour
{
    public enum PlotState { Normal, Tilled, Seeded, Grown }

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public PlotState CurrentState { get; set; }

    [Networked]
    public TickTimer growTimer { get; set; }

    [Header("--- Models (Kéo Prefab từ Project vào đây) ---")]
    [SerializeField] private GameObject modelDatThuong;
    [SerializeField] private GameObject modelDatCay;
    [SerializeField] private GameObject modelCayCon;
    [SerializeField] private GameObject modelCayLon;

    [Header("--- Điểm Neo (Vị trí mọc cây) ---")]
    [Tooltip("Kéo GameObject rỗng (DiemGieoHat) vào đây để làm tâm mọc cây")]
    [SerializeField] private Transform diemGieoHat; 

    [Header("--- Farm Settings ---")]
    [SerializeField] private int fruitItemID = 201;
    [SerializeField] private int harvestCount = 3;
    [SerializeField] private float growTime = 10f; 

    private List<GameObject> spawnedVisuals = new List<GameObject>();

    public override void Spawned()
    {
        // Tắt hình ảnh của cục đất gốc trên Map
        MeshRenderer rootMesh = GetComponent<MeshRenderer>();
        if (rootMesh != null) rootMesh.enabled = false;

        OnStateChanged();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Xử lý đếm giờ mọc cây
        if (CurrentState == PlotState.Seeded && growTimer.Expired(Runner))
        {
            CurrentState = PlotState.Grown;
            growTimer = TickTimer.None;
        }
    }

    private void ClearVisuals()
    {
        foreach (var obj in spawnedVisuals)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedVisuals.Clear();
    }

    private void SpawnVisual(GameObject prefab)
    {
        if (prefab == null) return;
        
        // CỐT LÕI CỦA CÁCH 1: Xác định điểm sinh ra
        // Nếu bạn đã kéo DiemGieoHat vào Inspector thì lấy tọa độ đó, nếu quên kéo thì lấy tọa độ gốc của cục đất
        Transform viTriSinhRa = (diemGieoHat != null) ? diemGieoHat : transform;

        // Sinh ra Prefab tại đúng vị trí Điểm Neo
        GameObject newVisual = Instantiate(prefab, viTriSinhRa.position, viTriSinhRa.rotation, transform);
        
        // Tắt hết collider của mô hình mới sinh ra để không cản tia Raycast
        Collider[] cols = newVisual.GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;
        
        spawnedVisuals.Add(newVisual);
    }

    private void OnStateChanged()
    {
        ClearVisuals(); 

        switch (CurrentState)
        {
            case PlotState.Normal:
                SpawnVisual(modelDatThuong);
                break;
            case PlotState.Tilled:
                SpawnVisual(modelDatCay);
                break;
            case PlotState.Seeded:
                SpawnVisual(modelDatCay); 
                SpawnVisual(modelCayCon); // Cây con sẽ tự động chui vào DiemGieoHat
                break;
            case PlotState.Grown:
                SpawnVisual(modelDatCay); 
                SpawnVisual(modelCayLon); // Cây lớn sẽ tự động chui vào DiemGieoHat
                break;
        }
    }

    // =========================================================
    // CÁC LỆNH RPC XỬ LÝ LOGIC TRỒNG CÂY (Giữ nguyên)
    // =========================================================

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CayDat()
    {
        if (CurrentState == PlotState.Normal) 
            CurrentState = PlotState.Tilled;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_GieoHat()
    {
        if (CurrentState != PlotState.Tilled) return;

        CurrentState = PlotState.Seeded;
        growTimer = TickTimer.CreateFromSeconds(Runner, growTime); 
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ThuHoach(PlayerRef nguoi)
    {
        if (CurrentState != PlotState.Grown) return;

        var playerObj = Runner.GetPlayerObject(nguoi);
        if (playerObj == null) return;

        Player_Controller player = playerObj.GetComponent<Player_Controller>();
        if (player == null) return;

        bool added = false;

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
            CurrentState = PlotState.Normal;
            Rpc_ThongBaoThuHoach(nguoi, harvestCount);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ThongBaoThuHoach([RpcTarget] PlayerRef targetPlayer, int soLuong)
    {
        if (ItemNotifyManager.Instance == null || InventoryManager.instance == null) return;

        Item thongTin = InventoryManager.instance.TraCuuItem(fruitItemID);
        if (thongTin != null)
            ItemNotifyManager.Instance.ShowNotify(thongTin.itemName, soLuong, thongTin.icon);
    }
}