using UnityEngine;
using Fusion;
using UnityEngine.AI;

public class BossController : NetworkBehaviour
{
    [Header("Định danh Boss")]
    public string tenBoss = "Quỷ Khổng Lồ";
    public Sprite avatarBoss;

    [Header("Chỉ số Boss")]
    public float maxHealth = 1000f;
    [Networked] public float CurrentHealth { get; set; }

    [Header("Cơ chế Vùng & Di chuyển (NavMesh)")]
    public float tocDoTuanTra = 1.5f;
    public float tocDoDuoiTheo = 2f;
    public float banKinhTuanTra = 15f;
    public float banKinhPhatHien = 20f;
    public float banKinhTanCong = 3f;

    [Header("Sát Thương & Tấn Công")]
    public float attackDamage = 30f;
    public float thoiGianHoiDon = 2f; // Thời gian nghỉ giữa 2 lần đánh thường
    [Networked] private TickTimer attackTimer { get; set; }

    [Header("Kỹ Năng Boss (Skill)")]
    public int soDonDeTungSkill = 3; // Đánh 3 hit thường sẽ ra 1 skill
    public float skillDamage = 80f;  // Sát thương của chiêu đặc biệt
    public float thoiGianHoiSkill = 3.5f; // Ra skill xong phải đứng thở lâu hơn
    [Networked] private int hitCount { get; set; } // Đếm số hit đã đánh

    [Header("Bị Đánh & Kháng Choáng (Chống Spam)")]
    public float thoiGianChoang = 1f; // Độ dài animation Hit (td1)
    public float thoiGianMienChoang = 2f; // Kháng choáng trong 2 giây sau khi bị đơ
    [Networked] private TickTimer stunTimer { get; set; }
    [Networked] private TickTimer mienChoangTimer { get; set; }

    [Header("Giao diện (UI Thanh Máu)")]
    public float khoangCachHienThanhMau = 25f;
    public HealthBar healthBarUI;

    [Header("Cài Đặt Despawn")]
    public float thoiGianBienMat = 4f;
    [Networked] private TickTimer despawnTimer { get; set; }

    // --- Biến nội bộ ---
    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 viTriGoc;
    private Player_Controller mucTieuHienTai;

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

        // --- CODE TỰ ĐỘNG NHẬN DIỆN UI THANH MÁU ---
        if (healthBarUI == null)
        {
            // Tìm đối tượng có tên exat là "Slider" trong Scene
            GameObject thanhMauTìmĐược = GameObject.Find("Slider");

            if (thanhMauTìmĐược != null)
            {
                // Lấy component HealthBar gắn vào biến
                healthBarUI = thanhMauTìmĐược.GetComponent<HealthBar>();
            }
            else
            {
                Debug.LogWarning("Boss không tìm thấy UI nào tên là 'Slider' trong Scene!");
            }
        }
        // ------------------------------------------

        if (HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            currentState = BossState.TuanTra;
            hitCount = 0;
            PhatSinhDiemTuanTraMoi();
        }

        if (healthBarUI != null)
        {
            healthBarUI.CapNhatTenBoss(tenBoss);
            healthBarUI.ResetHealthBar();
            healthBarUI.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (currentState == BossState.Chet)
        {
            if (despawnTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
            return;
        }

        TimMucTieuGanNhat();
        CapNhatTrangThaiAI();
        DongBoAnimation();
    }

    void Update()
    {
        if (Player_Controller.localPlayer != null && healthBarUI != null)
        {
            GameObject thanhMauObj = healthBarUI.gameObject;

            float binhPhuongKhoangCach = (transform.position - Player_Controller.localPlayer.transform.position).sqrMagnitude;
            float binhPhuongKhoangCachChoPhep = khoangCachHienThanhMau * khoangCachHienThanhMau;

            if (binhPhuongKhoangCach <= binhPhuongKhoangCachChoPhep)
            {
                thanhMauObj.SetActive(true);
                healthBarUI.UpdateHealthBar(CurrentHealth, maxHealth);

                if (currentState == BossState.Chet && healthBarUI.lazySlider.value <= 0.01f)
                {
                    thanhMauObj.SetActive(false);
                }
            }
            else
            {
                thanhMauObj.SetActive(false);
            }
        }
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
                else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    PhatSinhDiemTuanTraMoi();
                }
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
                    if (khoangCachToiPlayer <= banKinhTanCong)
                    {
                        currentState = BossState.TanCong;
                        agent.isStopped = true;
                    }
                }
                break;

            case BossState.TanCong:
                if (mucTieuHienTai == null || Vector3.Distance(transform.position, mucTieuHienTai.transform.position) > banKinhTanCong)
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

                    if (attackTimer.ExpiredOrNotRunning(Runner))
                    {
                        ThucHienDanhPlayer();
                    }
                }
                break;
        }
    }

    private void TimMucTieuGanNhat()
    {
        if (!scanTargetTimer.ExpiredOrNotRunning(Runner)) return;
        scanTargetTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);

        if (mucTieuHienTai != null)
        {
            if (mucTieuHienTai.isDead || Vector3.Distance(transform.position, mucTieuHienTai.transform.position) > banKinhPhatHien)
            {
                mucTieuHienTai = null;
            }
        }

        if (mucTieuHienTai == null)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, banKinhPhatHien);
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
        Vector3 diemRandom = viTriGoc + Random.insideUnitSphere * banKinhTuanTra;
        NavMeshHit navHit;

        if (NavMesh.SamplePosition(diemRandom, out navHit, banKinhTuanTra, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
        else
        {
            agent.SetDestination(viTriGoc);
        }
    }

    private void DongBoAnimation()
    {
        if (animator == null) return;
        // Sử dụng dampTime = 0.1f để vận tốc chuyển đổi mượt mà, giúp nhân vật không bị khựng cứng
        animator.SetFloat(hashSpeed, agent.velocity.magnitude, 0.1f, Runner.DeltaTime);
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

    #region NHẬN SÁT THƯƠNG

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayerHitBoss(float damage)
    {
        if (currentState == BossState.Chet) return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        if (CurrentHealth <= 0)
        {
            currentState = BossState.Chet;
            agent.isStopped = true;
            RPC_AnimDead();
            despawnTimer = TickTimer.CreateFromSeconds(Runner, thoiGianBienMat);
        }
        else
        {
            // Kiểm tra bộ đếm Miễn Choáng
            if (mienChoangTimer.ExpiredOrNotRunning(Runner))
            {
                currentState = BossState.BiDanh;
                agent.isStopped = true;

                stunTimer = TickTimer.CreateFromSeconds(Runner, thoiGianChoang);
                mienChoangTimer = TickTimer.CreateFromSeconds(Runner, thoiGianChoang + thoiGianMienChoang);

                RPC_AnimHurt();
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? viTriGoc : transform.position, banKinhTuanTra);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, banKinhPhatHien);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, banKinhTanCong);
    }
}