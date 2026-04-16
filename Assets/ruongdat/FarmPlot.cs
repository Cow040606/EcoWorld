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
        
        Transform viTriSinhRa = (diemGieoHat != null) ? diemGieoHat : transform;

        GameObject newVisual = Instantiate(prefab, viTriSinhRa.position, viTriSinhRa.rotation, transform);
        
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
                SpawnVisual(modelCayCon); 
                break;
            case PlotState.Grown:
                SpawnVisual(modelDatCay); 
                SpawnVisual(modelCayLon); 
                break;
        }
    }

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

        // Cộng dồn vào ô có sẵn
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

        // Tạo ô mới nếu chưa có
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
            // [ĐÃ SỬA LẠI LOGIC TẠI ĐÂY]
            // Quay về cây baby (Seeded) thay vì đất trống (Normal)
            CurrentState = PlotState.Seeded; 
            
            // Khởi động lại đồng hồ đếm ngược 10 giây để cây lớn tiếp
            growTimer = TickTimer.CreateFromSeconds(Runner, growTime); 
            
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