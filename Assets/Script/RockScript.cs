using UnityEngine;
using Fusion;
using System.Collections;
// Bỏ INetworkRunnerCallbacks đi vì cái cục đá không cần thiết dùng đến nó
public class RockScript : NetworkBehaviour
{
    #region KHAI BÁO BIẾN (VARIABLES)
    [Header("Thông số")]
    // 1. Phải có [Networked] để đồng bộ máu cho tất cả người chơi thấy
    [Networked] public float HP { get; set; } = 100f; 

    private Vector3 scaleGoc; 
    private Coroutine hieuUngCoroutine;

    [Header("Items")]
    
    // Thêm biến để nhét cái Prefab cục quặng vào đây cho nó đẻ ra
    public NetworkObject prefabQuangDa; 
    #endregion

    // 2. Chuyển từ Update sang FixedUpdateNetwork
    public override void FixedUpdateNetwork()
    {
        // CHỈ CÓ MÁY CHỦ (State Authority) MỚI ĐƯỢC QUYỀN XÓA ĐÁ VÀ RỚT ĐỒ
        if (!HasStateAuthority) return;

        if (HP <= 0)
        {
            SpawnItem();
            Runner.Despawn(Object); // Xóa cục đá to
        }
    }

    // 3. Viết nội dung cho hàm đẻ ra đồ
    private void SpawnItem()
    {
        if (prefabQuangDa != null)
        {
            // Sinh ra cục đá nhỏ tại vị trí này, hơi nhích lên trên một xíu
            Vector3 viTriRot = transform.position + Vector3.up * 1f;
            Runner.Spawn(prefabQuangDa, viTriRot, Quaternion.identity);
            
            Debug.Log("Keng! Đá vỡ, rớt ra khoáng sản!");
        }
    }

    // --- HÀM NÀY ĐỂ BÊN PLAYER GỌI KHI QUẶT CUỐC VÀO ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_NhanSatThuongCuoc(float dame)
    {
        // Trừ máu cục đá
        HP -= dame;
        RPC_HieuUngDapDa();
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_HieuUngDapDa()
    {
        // 🚨 CHỐT CHẶN AN TOÀN: Nếu scaleGoc bị rỗng (0, 0, 0), lập tức đo lại kích thước của cục đá!
        if (scaleGoc == Vector3.zero)
        {
            scaleGoc = transform.localScale;
        }

        // Nếu có hiệu ứng cũ đang chạy thì dập nó đi để chạy cái mới
        if (hieuUngCoroutine != null) StopCoroutine(hieuUngCoroutine);
        
        // Bắt đầu nhún nhảy
        hieuUngCoroutine = StartCoroutine(ChayHieuUngScale());
    }
    private IEnumerator ChayHieuUngScale()
    {
        Vector3 scaleTo = scaleGoc * 1.1f; // Mục tiêu phóng to lên 1.1x
        float thoiGianZoom = 0.05f; // Tốc độ phóng to (càng nhỏ càng nhanh)
        
        // 1. Phóng to từ từ lên 1.1x
        float thoiGian = 0;
        while (thoiGian < thoiGianZoom)
        {
            transform.localScale = Vector3.Lerp(scaleGoc, scaleTo, thoiGian / thoiGianZoom);
            thoiGian += Time.deltaTime;
            yield return null; // Đợi tới khung hình tiếp theo rồi làm tiếp
        }

        // 2. Thu nhỏ từ từ về lại 1.0x (Kích thước gốc)
        thoiGian = 0;
        while (thoiGian < thoiGianZoom)
        {
            transform.localScale = Vector3.Lerp(scaleTo, scaleGoc, thoiGian / thoiGianZoom);
            thoiGian += Time.deltaTime;
            yield return null; 
        }

        // 3. Chốt hạ! Đảm bảo cục đá về đúng y boong kích thước ban đầu
        transform.localScale = scaleGoc;
    }
}