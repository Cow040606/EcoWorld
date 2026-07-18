using UnityEngine;
using Fusion;

public class TreeScript : NetworkBehaviour
{
    [Header("Thông số Cây")]
    public float maxHP = 100f;
    public float thoiGianHoiSinh = 30f; // Chặt xong 30s sau mọc lại

    [Networked] public float HP { get; set; }
    [Networked] public NetworkBool IsActive { get; set; }
    [Networked] public TickTimer RespawnTimer { get; set; }

    [Header("Vật Phẩm Rớt")]
    public NetworkPrefabRef woodPrefab;

    [Header("Hình Ảnh & Va Chạm")]
    public GameObject treeVisual;  // Kéo Object Visual (con) vào đây
    public Collider treeCollider;  // Kéo CapsuleCollider vào đây

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            HP = maxHP;
            IsActive = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // Logic hồi sinh cây
        if (!IsActive && RespawnTimer.Expired(Runner))
        {
            HP = maxHP;
            IsActive = true;
            RespawnTimer = TickTimer.None;
        }
    }

    public override void Render()
    {
        if (treeVisual == null) return;

        // Cập nhật hiển thị và va chạm dựa trên trạng thái IsActive
        bool activeState = IsActive;
        if (treeVisual.activeSelf != activeState)
        {
            treeVisual.SetActive(activeState);
            if (treeCollider != null) treeCollider.enabled = activeState;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage)
    {
        if (!IsActive) return;

        HP -= damage;
        if (HP <= 0)
        {
            IsActive = false; // Ẩn cây
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, thoiGianHoiSinh);

            // Rớt vật phẩm
            if (woodPrefab.IsValid)
            {
                Runner.Spawn(woodPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            }
        }
    }
}