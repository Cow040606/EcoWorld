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
    [Tooltip("Không cần kéo thủ công nữa, code sẽ tự tìm!")]
    public GameObject treeVisual;
    public Collider treeCollider;

    // Hàm Awake chạy ngay khi Prefab xuất hiện trong game
    private void Awake()
    {
        // Tự động tìm object con có tên chính xác là "Visual"
        if (treeVisual == null)
        {
            Transform visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                treeVisual = visualTransform.gameObject;
            }
            else
            {
                Debug.LogError($"<color=red>[LỖI]</color> Cây {gameObject.name} chưa có object con nào tên là 'Visual'. Hãy tạo ngay!");
            }
        }
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            HP = maxHP;
            IsActive = true;
            Debug.Log($"[TreeScript] Cây {gameObject.name} đã sẵn sàng.");
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
            Debug.Log($"[TreeScript] Cây {gameObject.name} đã hồi sinh!");
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
        if (!IsActive) return;

        HP -= damage;
        Debug.Log($"[TreeScript] Cây bị chém! Máu còn: {HP}");

        if (HP <= 0)
        {
            IsActive = false;
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, thoiGianHoiSinh);
            Debug.Log($"[TreeScript] Cây đã đổ. Hồi sinh sau {thoiGianHoiSinh}s.");

            if (woodPrefab.IsValid)
            {
                Runner.Spawn(woodPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            }
        }
    }
}