using UnityEngine;
using Fusion;

public class TreeScript : NetworkBehaviour
{
    [Header("Thông số Cây")]
    public float maxHP = 100f;
    public float thoiGianHoiSinh = 120f; // Đúng 2 phút

    [Networked] public float HP { get; set; }
    [Networked] public NetworkBool IsActive { get; set; }
    [Networked] public TickTimer RespawnTimer { get; set; }

    [Header("Vật Phẩm Rớt")]
    public NetworkPrefabRef woodPrefab;

    // Bộ 3 sát thủ: Bắt gọn Hình ảnh, Va chạm và LOD
    private Renderer[] cacHinhAnh;
    private Collider[] cacVaCham;
    private LODGroup[] cacLOD;

    private void Awake()
    {
        // Quét toàn bộ component bên trong Prefab (dù nó lồng ghép cỡ nào)
        cacHinhAnh = GetComponentsInChildren<Renderer>();
        cacVaCham = GetComponentsInChildren<Collider>();
        cacLOD = GetComponentsInChildren<LODGroup>(); // Bắt thủ phạm LOD
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            HP = maxHP;
            IsActive = true;
        }
        CapNhatHienThi(IsActive);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // Cây mọc lại sau 2 phút
        if (!IsActive && RespawnTimer.Expired(Runner))
        {
            HP = maxHP;
            IsActive = true;
            RespawnTimer = TickTimer.None;
        }
    }

    public override void Render()
    {
        CapNhatHienThi(IsActive);
    }

    private void CapNhatHienThi(bool trangThai)
    {
        // 1. TẮT LOD GROUP TRƯỚC (Rất quan trọng, trị tận gốc bệnh không tàng hình)
        if (cacLOD != null)
        {
            foreach (var lod in cacLOD)
            {
                if (lod != null && lod.enabled != trangThai) lod.enabled = trangThai;
            }
        }

        // 2. TẮT RENDERER
        if (cacHinhAnh != null)
        {
            foreach (var anh in cacHinhAnh)
            {
                if (anh != null && anh.enabled != trangThai) anh.enabled = trangThai;
            }
        }

        // 3. TẮT COLLIDER
        if (cacVaCham != null)
        {
            foreach (var vc in cacVaCham)
            {
                if (vc != null && vc.enabled != trangThai) vc.enabled = trangThai;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage)
    {
        if (!IsActive) return;

        HP -= damage;

        if (HP <= 0)
        {
            IsActive = false;
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, thoiGianHoiSinh);

            // Ép tắt toàn bộ cây trên Server NGAY LẬP TỨC
            CapNhatHienThi(false);

            if (woodPrefab.IsValid)
            {
                // FIX LỖI GỖ BAY: Sinh gỗ cao 2.5 mét (vượt qua đầu nhân vật) 
                // và xích ra ngẫu nhiên 1 chút xíu để tránh rớt trúng đầu.
                Vector2 lechNgauNhien = Random.insideUnitCircle * 1.5f;
                Vector3 viTriAnToan = transform.position + new Vector3(lechNgauNhien.x, 2.5f, lechNgauNhien.y);

                Runner.Spawn(woodPrefab, viTriAnToan, Quaternion.identity);
            }
        }
    }
}