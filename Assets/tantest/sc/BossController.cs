using UnityEngine;
using Fusion;
using UnityEngine.AI;
using System.Collections.Generic;

public class BossController : NetworkBehaviour
{
    [Header("Định danh Boss")]
    // THÊM MỚI: ID của Boss để đối chiếu với nhiệm vụ
    [Tooltip("ID của Boss. Dùng để đối chiếu với targetID trong Nhiệm Vụ.")]
    public int bossID = 100;
    public string tenBoss = "Quỷ Khổng Lồ";
    public Sprite avatarBoss;

    [Header("Chỉ số Boss")]
    public float maxHealth = 1000f;
    [Networked] public float CurrentHealth { get; set; }

    [Header("Thưởng Kinh Nghiệm")]
    public float expReward = 500f;

    [Header("Drop Settings (Rớt đồ)")]
    [Tooltip("Danh sách các vật phẩm có thể rớt (Bắt buộc phải có component NetworkObject)")]
    public List<GameObject> dropItems;

    [Tooltip("Tỉ lệ rớt đồ (0 đến 100%)")]
    [Range(0f, 100f)] public float dropChance = 100f;

    [Header("Cơ chế Vùng & Di chuyển (NavMesh)")]
    public float tocDoTuanTra = 1.5f;
    public float tocDoDuoiTheo = 2f;
    public float banKinhTuanTra = 15f;
    public float banKinhPhatHien = 20f;
    public float banKinhTanCong = 3f;

    [Header("Sát Thương & Tấn Công")]
    public float attackDamage = 30f;
    public float thoiGianHoiDon = 2f;
    [Networked] private TickTimer attackTimer { get; set; }

    [Header("Kỹ Năng Boss (Skill)")]
    public int soDonDeTungSkill = 3;
    public float skillDamage = 80f;
    public float thoiGianHoiSkill = 3.5f;
    [Networked] private int hitCount { get; set; }

    [Header("Bị Đánh & Kháng Choáng (Chống Spam)")]
    public float thoiGianChoang = 1f;
    public float thoiGianMienChoang = 2f;
    [Networked] private TickTimer stunTimer { get; set; }
    [Networked] private TickTimer mienChoangTimer { get; set; }

    [Header("Giao diện (UI Thanh Máu)")]
    public float khoangCachHienThanhMau = 25f;
    public static BossController currentActiveBoss;

    [Header("Cài Đặt Despawn")]
    public float thoiGianBienMat = 4f;
    [Networked] private TickTimer despawnTimer { get; set; }

    [Networked] public float tocDoDiChuyen { get; set; }

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 viTriGoc;
    private Player_Controller mucTieuHienTai;
    private float heSoScale = 1f;
    private TickTimer scanTargetTimer;
    private TickTimer updatePathTimer;

    private readonly int hashSpeed = Animator.StringToHash("Speed");
    private readonly int hashAttack = Animator.StringToHash("Attack");
    private readonly int hashSkill = Animator.StringToHash("Skill");
    private readonly int hashIsDead = Animator.StringToHash("isDead");
    private readonly int hashHit = Animator.StringToHash("Hit");

    public enum BossState { TuanTra, DiTheo, TanCong, BiDanh, Chet }
    [Networked] public BossState currentState { get; set; }

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        viTriGoc = transform.position;
        heSoScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        if (HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            currentState = BossState.TuanTra;
            hitCount = 0;
            PhatSinhDiemTuanTraMoi();
        }
        else
        {
            if (agent != null) agent.enabled = false;
        }

        if (BossHealthBarHUD.Instance != null)
        {
            BossHealthBarHUD.Instance.ResetHealthBar();
            BossHealthBarHUD.Instance.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (currentState == BossState.Chet)
        {
            if (despawnTimer.Expired(Runner)) Runner.Despawn(Object);
            return;
        }

        TimMucTieuGanNhat();
        CapNhatTrangThaiAI();

        if (agent.enabled) tocDoDiChuyen = agent.velocity.magnitude;
    }

    public override void Render()
    {
        if (animator != null) animator.SetFloat(hashSpeed, tocDoDiChuyen, 0.1f, Time.deltaTime);
    }

    void Update()
    {
        if (Player_Controller.localPlayer == null || BossHealthBarHUD.Instance == null) return;

        if (currentState == BossState.Chet)
        {
            if (currentActiveBoss == this)
            {
                BossHealthBarHUD.Instance.gameObject.SetActive(false);
                currentActiveBoss = null;
            }
            return;
        }

        float khoangCachToiPlayer = Vector3.Distance(transform.position, Player_Controller.localPlayer.transform.position);
        float tamHienThiThucTe = khoangCachHienThanhMau * heSoScale;

        if (khoangCachToiPlayer <= tamHienThiThucTe)
        {
            if (currentActiveBoss == null || currentActiveBoss == this) HienThiThongTinLenUI();
            else
            {
                float khoangCachBossKia = Vector3.Distance(currentActiveBoss.transform.position, Player_Controller.localPlayer.transform.position);
                if (khoangCachToiPlayer < khoangCachBossKia) HienThiThongTinLenUI();
            }
        }
        else
        {
            if (currentActiveBoss == this)
            {
                BossHealthBarHUD.Instance.gameObject.SetActive(false);
                currentActiveBoss = null;
            }
        }
    }

    private void HienThiThongTinLenUI()
    {
        currentActiveBoss = this;
        BossHealthBarHUD.Instance.gameObject.SetActive(true);
        BossHealthBarHUD.Instance.CapNhatTenBoss(tenBoss);
        BossHealthBarHUD.Instance.UpdateHealthBar(CurrentHealth, maxHealth);
    }

    #region LOGIC AI
    private void CapNhatTrangThaiAI()
    {
        switch (currentState)
        {
            case BossState.BiDanh:
                agent.isStopped = true;
                if (stunTimer.ExpiredOrNotRunning(Runner))
                {
                    currentState = BossState.DiTheo;
                    agent.isStopped = false;
                }
                break;
            case BossState.TuanTra:
                agent.speed = tocDoTuanTra;
                if (mucTieuHienTai != null)
                {
                    currentState = BossState.DiTheo;
                    updatePathTimer = TickTimer.None;
                }
                else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) PhatSinhDiemTuanTraMoi();
                break;
            case BossState.DiTheo:
                agent.speed = tocDoDuoiTheo;
                if (mucTieuHienTai == null)
                {
                    currentState = BossState.TuanTra;
                    PhatSinhDiemTuanTraMoi();
                }
                else
                {
                    if (updatePathTimer.ExpiredOrNotRunning(Runner))
                    {
                        agent.SetDestination(mucTieuHienTai.transform.position);
                        updatePathTimer = TickTimer.CreateFromSeconds(Runner, 0.25f);
                    }
                    float khoangCachToiPlayer = Vector3.Distance(transform.position, mucTieuHienTai.transform.position);
                    if (khoangCachToiPlayer <= banKinhTanCong * heSoScale)
                    {
                        currentState = BossState.TanCong;
                        agent.isStopped = true;
                    }
                }
                break;
            case BossState.TanCong:
                if (mucTieuHienTai == null || Vector3.Distance(transform.position, mucTieuHienTai.transform.position) > banKinhTanCong * heSoScale)
                {
                    currentState = BossState.DiTheo;
                    agent.isStopped = false;
                    updatePathTimer = TickTimer.None;
                }
                else
                {
                    Vector3 huongNhin = (mucTieuHienTai.transform.position - transform.position).normalized;
                    huongNhin.y = 0;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(huongNhin), Runner.DeltaTime * 10f);
                    if (attackTimer.ExpiredOrNotRunning(Runner)) ThucHienDanhPlayer();
                }
                break;
        }
    }

    private void TimMucTieuGanNhat()
    {
        if (!scanTargetTimer.ExpiredOrNotRunning(Runner)) return;
        scanTargetTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);

        float tamPhatHienThucTe = banKinhPhatHien * heSoScale;
        if (mucTieuHienTai != null)
        {
            if (mucTieuHienTai.isDead || Vector3.Distance(transform.position, mucTieuHienTai.transform.position) > tamPhatHienThucTe) mucTieuHienTai = null;
        }

        if (mucTieuHienTai == null)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, tamPhatHienThucTe);
            float khoangCachNganNhat = Mathf.Infinity;
            foreach (var hit in hits)
            {
                Player_Controller player = hit.GetComponentInParent<Player_Controller>();
                if (player != null && !player.isDead)
                {
                    float khoangCach = Vector3.Distance(transform.position, player.transform.position);
                    if (khoangCach < khoangCachNganNhat)
                    {
                        khoangCachNganNhat = khoangCach;
                        mucTieuHienTai = player;
                    }
                }
            }
        }
    }

    private void PhatSinhDiemTuanTraMoi()
    {
        float tamTuanTraThucTe = banKinhTuanTra * heSoScale;
        Vector3 diemRandom = viTriGoc + Random.insideUnitSphere * tamTuanTraThucTe;
        if (NavMesh.SamplePosition(diemRandom, out NavMeshHit navHit, tamTuanTraThucTe, NavMesh.AllAreas)) agent.SetDestination(navHit.position);
        else agent.SetDestination(viTriGoc);
    }

    private void ThucHienDanhPlayer()
    {
        hitCount++;
        if (hitCount >= soDonDeTungSkill)
        {
            RPC_AnimSkill();
            if (mucTieuHienTai != null) mucTieuHienTai.Server_TakeDamageFromBoss(skillDamage);
            hitCount = 0;
            attackTimer = TickTimer.CreateFromSeconds(Runner, thoiGianHoiSkill);
        }
        else
        {
            RPC_AnimAttack();
            if (mucTieuHienTai != null) mucTieuHienTai.Server_TakeDamageFromBoss(attackDamage);
            attackTimer = TickTimer.CreateFromSeconds(Runner, thoiGianHoiDon);
        }
    }
    #endregion

    #region NHẬN SÁT THƯƠNG & EXP
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayerHitBoss(float damage, RpcInfo info = default)
    {
        if (currentState == BossState.Chet) return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        if (CurrentHealth <= 0)
        {
            currentState = BossState.Chet;
            if (agent.isActiveAndEnabled) agent.isStopped = true;
            RPC_AnimDead();
            despawnTimer = TickTimer.CreateFromSeconds(Runner, thoiGianBienMat);

            // CHIA KINH NGHIỆM CHO NGƯỜI KẾT LIỄU
            GiveExpToKiller(info.Source, expReward);

            // --- THÊM MỚI: BÁO CHO NHIỆM VỤ LÀ BOSS ĐÃ CHẾT ---
            if (Player_QuestManager.localQuest != null)
            {
                Player_QuestManager.localQuest.TangTienDoNhiemVu(LoaiNhiemVu.TieuDietQuai, bossID, 1);
            }

            // --- GỌI HÀM RỚT ĐỒ ---
            DropItem();
        }
        else
        {
            if (mienChoangTimer.ExpiredOrNotRunning(Runner))
            {
                currentState = BossState.BiDanh;
                if (agent.isActiveAndEnabled) agent.isStopped = true;
                stunTimer = TickTimer.CreateFromSeconds(Runner, thoiGianChoang);
                mienChoangTimer = TickTimer.CreateFromSeconds(Runner, thoiGianChoang + thoiGianMienChoang);
                RPC_AnimHurt();
            }
        }
    }

    private void GiveExpToKiller(PlayerRef playerRef, float expAmount)
    {
        if (!HasStateAuthority || playerRef == PlayerRef.None) return;

        NetworkObject playerObj = Runner.GetPlayerObject(playerRef);
        if (playerObj != null)
        {
            Player_Controller player = playerObj.GetComponent<Player_Controller>();
            if (player != null)
            {
                player.Server_AddExp(expAmount);
            }
        }
    }

    // --- HÀM XỬ LÝ RỚT ĐỒ CỦA BOSS ---
    private void DropItem()
    {
        if (!HasStateAuthority) return; // Chỉ máy chủ mới được tạo vật phẩm

        if (dropItems != null && dropItems.Count > 0 && Random.Range(0f, 100f) <= dropChance)
        {
            GameObject itemToDrop = dropItems[Random.Range(0, dropItems.Count)];
            if (itemToDrop != null)
            {
                NetworkObject netObj = itemToDrop.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    // Rớt đồ cách mặt đất một chút để tránh bị chìm
                    Runner.Spawn(netObj, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnimAttack() { if (animator != null) animator.SetTrigger(hashAttack); }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnimSkill() { if (animator != null) animator.SetTrigger(hashSkill); }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnimHurt() { if (animator != null) animator.SetTrigger(hashHit); }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnimDead() { if (animator != null) animator.SetBool(hashIsDead, true); }
    #endregion

    private void OnDrawGizmosSelected()
    {
        float tiLe = Application.isPlaying ? heSoScale : Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(Application.isPlaying ? viTriGoc : transform.position, banKinhTuanTra * tiLe);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, banKinhPhatHien * tiLe);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, banKinhTanCong * tiLe);
    }
}