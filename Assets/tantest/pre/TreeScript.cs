using UnityEngine;
using Fusion;

public class TreeScript : NetworkBehaviour
{
    [Header("Thông số Cây")]
    public float maxHP = 100f;
    public float thoiGianHoiSinh = 30f;

    [Networked] public float HP { get; set; }
    [Networked] public NetworkBool IsActive { get; set; }
    [Networked] public TickTimer RespawnTimer { get; set; }

    [Header("Vật Phẩm Rớt")]
    public NetworkPrefabRef woodPrefab;

    [Header("Hình Ảnh & Va Chạm")]
    public GameObject treeVisual;
    public Collider treeCollider;

    private void Awake()
    {
        if (treeVisual == null)
        {
            Transform visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                treeVisual = visualTransform.gameObject;
            }
            else
            {
                Debug.LogError($"<color=red>[LỖI TREE]</color> Cây {gameObject.name} chưa có object con tên 'Visual'.");
            }
        }

        // TỰ ĐỘNG KIỂM TRA LAYER KHI VỪA CHẠY GAME
        if (gameObject.layer != LayerMask.NameToLayer("Tree"))
        {
            Debug.LogWarning($"<color=orange>[CẢNH BÁO]</color> Cây {gameObject.name} đang ở Layer '{LayerMask.LayerToName(gameObject.layer)}'. Hãy đổi nó sang Layer 'Tree' ngay trên Inspector để Player chém trúng!");
        }
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            HP = maxHP;
            IsActive = true;
            Debug.Log($"<color=white>[TreeScript]</color> Cây {gameObject.name} đã Spawn thành công (HP: {HP}).");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!IsActive && RespawnTimer.Expired(Runner))
        {
            HP = maxHP;
            IsActive = true;
            RespawnTimer = TickTimer.None;
            Debug.Log($"<color=green>[TreeScript]</color> Cây {gameObject.name} đã mọc lại!");
        }
    }

    public override void Render()
    {
        if (treeVisual == null) return;

        if (treeVisual.activeSelf != IsActive)
        {
            treeVisual.SetActive(IsActive);
            if (treeCollider != null) treeCollider.enabled = IsActive;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage)
    {
        // BÁO CÁO NGAY KHI NHẬN ĐƯỢC TÍN HIỆU CHÉM TỪ PLAYER
        Debug.Log($"<color=cyan>[TreeScript - RPC]</color> Đã nhận lệnh chặt cây! Sát thương: {damage} | Trạng thái hiện tại IsActive: {IsActive}");

        if (!IsActive)
        {
            Debug.Log($"<color=yellow>[TreeScript]</color> Cây đang bị đốn ngã chờ hồi sinh, từ chối nhận sát thương.");
            return;
        }

        HP -= damage;
        Debug.Log($"<color=yellow>[TreeScript]</color> Bị chém trúng! Máu còn lại: {HP}");

        if (HP <= 0)
        {
            IsActive = false;
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, thoiGianHoiSinh);
            Debug.Log($"<color=red>[TreeScript]</color> Cây {gameObject.name} đã đổ! Bắt đầu đếm ngược hồi sinh {thoiGianHoiSinh}s.");

            if (woodPrefab.IsValid)
            {
                Runner.Spawn(woodPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                Debug.Log($"<color=magenta>[TreeScript]</color> Đã Spawn vật phẩm Gỗ thành công!");
            }
            else
            {
                Debug.LogError($"<color=red>[LỖI TREE]</color> Chưa kéo Wood Prefab vào cây {gameObject.name}!");
            }
        }
    }
}