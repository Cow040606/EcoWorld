using UnityEngine;
using Fusion;
using System.Collections;

public class RockScript : NetworkBehaviour
{
    #region KHAI BÁO BIẾN
    [Header("Thông số")]
    public float maxHP = 100f;
    public float thoiGianHoiSinh = 10f; // Số giây để đá mọc lại

    // [Networked] Đồng bộ trạng thái máu, sống/chết và đồng hồ đếm ngược
    [Networked] public float HP { get; set; }
    [Networked] public NetworkBool IsActive { get; set; }
    [Networked] public TickTimer RespawnTimer { get; set; }

    [Header("Items & Visuals")]
    public NetworkObject prefabQuangDa;

    [Tooltip("Kéo Object con chứa Mesh cục đá vào đây (Rock_Visual)")]
    public GameObject rockVisual;
    [Tooltip("Kéo Box Collider của cục đá vào đây")]
    public Collider rockCollider;

    private Vector3 scaleGoc;
    private Coroutine hieuUngCoroutine;

    // Công cụ dò sự thay đổi biến mạng của Fusion V2
    private ChangeDetector _changeDetector;
    #endregion

    public override void Spawned()
    {
        // Khởi tạo bộ dò thay đổi trạng thái
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // Chỉ Master (người nắm quyền) mới set chỉ số khởi điểm
        if (HasStateAuthority)
        {
            HP = maxHP;
            IsActive = true;
        }

        // Lưu scale gốc của Model 3D để chạy hiệu ứng
        if (rockVisual != null) scaleGoc = rockVisual.transform.localScale;
    }

    public override void FixedUpdateNetwork()
    {
        // Server/Host kiểm tra thời gian hồi sinh
        if (!HasStateAuthority) return;

        // Nếu đá đang bị ẩn và thời gian hồi sinh đã hết -> Gọi mọc lại
        if (!IsActive && RespawnTimer.Expired(Runner))
        {
            ResetRock();
        }
    }

    public override void Render()
    {
        // 🚨 CHỐT CHẶN AN TOÀN: Báo lỗi ra Console nếu quên kéo gán biến, giúp game không bị crash!
        if (rockVisual == null)
        {
            // Debug.LogError($"[Thiếu Biến] Cục đá {gameObject.name} chưa được gán Rock Visual trong Inspector!");
            return;
        }

        // 1. ĐỒNG BỘ ẨN/HIỆN CLIENT: Tự động bật tắt Model và Va chạm
        bool activeState = IsActive;
        if (rockVisual.activeSelf != activeState)
        {
            rockVisual.SetActive(activeState);
            if (rockCollider != null) rockCollider.enabled = activeState;
        }

        // 2. CHẠY HIỆU ỨNG TỰ ĐỘNG (Không lạm dụng RPC)
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(HP):
                    // Kích hoạt hiệu ứng nhún nhảy nếu mất máu và đá vẫn còn sống
                    if (IsActive && HP > 0 && HP < maxHP)
                    {
                        PlayHitEffect();
                    }
                    break;
            }
        }
    }

    // --- HÀM NÀY ĐỂ CLIENT GỌI LÊN MÁY CHỦ KHI QUẶT CUỐC VÀO ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_NhanSatThuongCuoc(float dame)
    {
        // Nếu đá đã vỡ (ẩn) thì cuốc trúng cũng không tính
        if (!IsActive) return;

        HP -= dame;

        // Kiểm tra máu và vỡ ngay khi nhận sát thương chí mạng
        if (HP <= 0)
        {
            BreakRock();
        }
    }

    private void BreakRock()
    {
        IsActive = false; // Đánh dấu là đã chết (ẩn đi)
        SpawnItem();      // Rớt đồ 1 lần duy nhất
        RespawnTimer = TickTimer.CreateFromSeconds(Runner, thoiGianHoiSinh); // Hẹn giờ mọc lại
    }

    private void ResetRock()
    {
        HP = maxHP;
        IsActive = true;
        RespawnTimer = TickTimer.None;
    }

    private void SpawnItem()
    {
        if (prefabQuangDa != null)
        {
            // Rớt cao hơn vị trí đá 1 chút để không bị chìm xuống đất
            Vector3 viTriRot = transform.position + Vector3.up * 1f;
            Runner.Spawn(prefabQuangDa, viTriRot, Quaternion.identity);
            // Debug.Log("Keng! Đá vỡ, rớt ra khoáng sản!");
        }
    }

    #region HIỆU ỨNG NHÚN NHẢY
    private void PlayHitEffect()
    {
        // Kiểm tra lại scale gốc để chống lỗi chia cho 0
        if (scaleGoc == Vector3.zero && rockVisual != null) scaleGoc = rockVisual.transform.localScale;

        // Dập Coroutine cũ nếu người chơi đập quá nhanh
        if (hieuUngCoroutine != null) StopCoroutine(hieuUngCoroutine);

        hieuUngCoroutine = StartCoroutine(ChayHieuUngScale());
    }

    private IEnumerator ChayHieuUngScale()
    {
        Vector3 scaleTo = scaleGoc * 1.1f;
        float thoiGianZoom = 0.05f;

        // 1. Phóng to
        float thoiGian = 0;
        while (thoiGian < thoiGianZoom)
        {
            rockVisual.transform.localScale = Vector3.Lerp(scaleGoc, scaleTo, thoiGian / thoiGianZoom);
            thoiGian += Time.deltaTime;
            yield return null;
        }

        // 2. Thu nhỏ về cũ
        thoiGian = 0;
        while (thoiGian < thoiGianZoom)
        {
            rockVisual.transform.localScale = Vector3.Lerp(scaleTo, scaleGoc, thoiGian / thoiGianZoom);
            thoiGian += Time.deltaTime;
            yield return null;
        }

        // 3. Chốt kích thước chuẩn
        rockVisual.transform.localScale = scaleGoc;
    }
    #endregion
}