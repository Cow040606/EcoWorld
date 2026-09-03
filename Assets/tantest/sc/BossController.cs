using UnityEngine;
using Fusion;
using UnityEngine.AI;
using System.Collections.Generic;

public class BossController : NetworkBehaviour
{
    [Header("Định danh Boss")]
    [Tooltip("ID của Boss. Dùng để đối chiếu với targetID trong Nhiệm Vụ.")]
    public int bossID = 100;
    public string tenBoss = "Quái Khổng Lồ";
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

    [Header("Âm Thanh (Audio)")]
    [Tooltip("Âm thanh khi vung vũ khí hoặc tấn công")]
    public AudioClip attackSound;
    [Tooltip("Âm thanh khi bị dính đòn")]
    public AudioClip hurtSound;
    [Tooltip("Âm thanh khi ngã gục")]
    public AudioClip deathSound;

    // Component phát âm thanh
    private AudioSource audioSource;

    [Header("Cơ chế Vùng & Di chuyển (NavMesh)")]
    public float tocDoTuanTra = 1.5f;
    public float tocDoDuoiTheo = 2.8f;
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
    [Tooltip("Thời gian hồi chiêu tối thiểu giữa các lần tung skill để tránh spam")]
    public float skillCooldown = 8f;
    [Networked] private int hitCount { get; set; }
    [Networked] private TickTimer skillCooldownTimer { get; set; }

    [Header("Bị Đánh & Kháng Choáng (Chống Spam)")]
    public float thoiGianChoang = 1f;
    public float thoiGianMienChoang = 2f;
    [Networked] private TickTimer stunTimer { get; set; }
    [Networked] private TickTimer mienChoangTimer { get; set; }

    [Header("Giao diện (UI Thanh Máu)")]
    public float khoangCachHienThanhMau = 25f;
    public static BossController currentActiveBoss;

    [Header("Cài đặt Despawn")]
    public float thoiGianBienMat = 4f;
    [Networked] private TickTimer despawnTimer { get; set; }

    [Networked] public float tocDoDiChuyen { get; set; }
    [Networked] public Vector3 NetworkViTriGoc { get; set; }

    public void SetViTriGoc(Vector3 pos)
    {
        if (pos.sqrMagnitude > 1f)
        {
            viTriGoc = pos;
            NetworkViTriGoc = pos;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(pos);
            }
            else
            {
                transform.position = pos;
            }
        }
    }

    [Header("Visual Effects (VFX)")]
    [Tooltip("VFX cho don danh thuong")]
    public GameObject attackVFXPrefab;
    [Tooltip("VFX cho don danh skill")]
    public GameObject skillVFXPrefab;
    [Tooltip("Vi tri de spawn VFX (thuong la o tay hoac kiem)")]
    public Transform vfxSpawnPoint;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 viTriGoc;
    private Player_Controller mucTieuHienTai;
    private float heSoScale = 1f;
    private TickTimer scanTargetTimer;
    private TickTimer updatePathTimer;
    private Collider[] scanResults = new Collider[64];

    // Chống kẹt địa hình
    private float stuckTimer = 0f;
    private Vector3 lastStuckPos = Vector3.zero;

    private readonly int hashSpeed = Animator.StringToHash("Speed");
    private readonly int hashAttack = Animator.StringToHash("Attack");
    private readonly int hashSkill = Animator.StringToHash("Skill");
    private readonly int hashIsDead = Animator.StringToHash("isDead");
    private readonly int hashHit = Animator.StringToHash("Hit");

    public enum BossState { TuanTra, DiTheo, TanCong, BiDanh, Chet }
    [Networked, OnChangedRender(nameof(OnStateChanged))] public BossState currentState { get; set; }

    private void Awake()
    {
        // 1. Khử va chạm vật lý giữa Enemy với nhau, với động vật và với Terrain/Default
        Physics.IgnoreLayerCollision(13, 13, true);
        Physics.IgnoreLayerCollision(13, 14, true);
        Physics.IgnoreLayerCollision(13, 0, true);

        // 2. Chuyển Collider thành Trigger và nâng đáy để không cào lún vào địa hình dốc
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.center = new Vector3(0f, 1.2f, 0f);
            col.height = 2.2f;
            col.radius = 0.45f;
        }
    }

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        // Tắt Root Motion để NavMeshAgent toàn quyền định vị
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 5f;
        audioSource.maxDistance = 30f;

        heSoScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        if (agent != null)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.angularSpeed = 600f;
            agent.acceleration = 35f;
            agent.baseOffset = 0.05f;
            agent.autoRepath = true;
            agent.stoppingDistance = 0.5f;
        }

        if (HasStateAuthority)
        {
            if (NetworkViTriGoc.sqrMagnitude > 1f)
            {
                viTriGoc = NetworkViTriGoc;
            }
            else if (transform.position.sqrMagnitude > 1f)
            {
                viTriGoc = transform.position;
                NetworkViTriGoc = transform.position;
            }

            if (agent != null)
            {
                agent.enabled = true;
                if (!agent.isOnNavMesh)
                {
                    Vector3 testPos = viTriGoc.sqrMagnitude > 1f ? viTriGoc : transform.position;
                    if (testPos.sqrMagnitude > 1f && NavMesh.SamplePosition(testPos, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
                    {
                        agent.Warp(navHit.position);
                        viTriGoc = navHit.position;
                        NetworkViTriGoc = navHit.position;
                    }
                }
            }
            CurrentHealth = maxHealth;
            currentState = BossState.TuanTra;
            hitCount = 0;
            lastStuckPos = transform.position;
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

        if (viTriGoc.sqrMagnitude < 1f)
        {
            if (NetworkViTriGoc.sqrMagnitude > 1f) viTriGoc = NetworkViTriGoc;
            else if (transform.position.sqrMagnitude > 1f)
            {
                viTriGoc = transform.position;
                NetworkViTriGoc = transform.position;
            }
        }

        if (transform.position.y < -30f && viTriGoc.sqrMagnitude > 10f)
        {
            if (NavMesh.SamplePosition(viTriGoc, out NavMeshHit safeHit, 15f, NavMesh.AllAreas))
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.Warp(safeHit.position);
                    agent.isStopped = true;
                }
                else
                {
                    transform.position = safeHit.position;
                }
                currentState = BossState.TuanTra;
            }
        }

        if (currentState == BossState.Chet)
        {
            if (despawnTimer.Expired(Runner)) Runner.Despawn(Object);
            return;
        }

        TimMucTieuGanNhat();
        CapNhatTrangThaiAI();

        if (agent != null && agent.enabled) tocDoDiChuyen = agent.velocity.magnitude;
    }

    public override void Render()
    {
        float speed = HasStateAuthority && agent != null && agent.enabled ? agent.velocity.magnitude : tocDoDiChuyen;
        // Chỉ phát animation di chuyển khi thực sự có vận tốc
        if (animator != null) animator.SetFloat(hashSpeed, speed, 0.1f, Time.deltaTime);
    }

    public void OnStateChanged()
    {
        if (currentState == BossState.Chet)
        {
            Collider[] colliders = GetComponents<Collider>();
            foreach (var col in colliders) col.enabled = false;

            Collider[] childColliders = GetComponentsInChildren<Collider>();
            foreach (var col in childColliders) col.enabled = false;

            if (BossHealthBarHUD.Instance != null && currentActiveBoss == this)
            {
                BossHealthBarHUD.Instance.gameObject.SetActive(false);
                currentActiveBoss = null;
            }
        }

        if (animator == null) return;

        switch (currentState)
        {
            case BossState.Chet:
                animator.ResetTrigger(hashHit);
                animator.ResetTrigger(hashAttack);
                animator.ResetTrigger(hashSkill);
                animator.SetBool(hashIsDead, true);
                break;
            default:
                if (currentState != BossState.BiDanh)
                {
                    animator.ResetTrigger(hashHit);
                }
                break;
        }
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
        if (agent == null || !agent.enabled) return;

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
                    agent.isStopped = false;
                    updatePathTimer = TickTimer.None;
                    stuckTimer = 0f;
                    lastStuckPos = transform.position;
                }
                else
                {
                    // Chống kẹt tuần tra
                    if (Vector3.Distance(transform.position, lastStuckPos) < 0.1f)
                    {
                        stuckTimer += Runner.DeltaTime;
                        if (stuckTimer >= 3.5f)
                        {
                            stuckTimer = 0f;
                            PhatSinhDiemTuanTraMoi();
                        }
                    }
                    else
                    {
                        stuckTimer = 0f;
                        lastStuckPos = transform.position;
                    }

                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        PhatSinhDiemTuanTraMoi();
                    }
                }
                break;
            case BossState.DiTheo:
                agent.speed = tocDoDuoiTheo;
                if (agent.isStopped) agent.isStopped = false;
                if (mucTieuHienTai == null)
                {
                    currentState = BossState.TuanTra;
                    PhatSinhDiemTuanTraMoi();
                }
                else
                {
                    if (updatePathTimer.ExpiredOrNotRunning(Runner))
                    {
                        Vector3 targetNavPos = mucTieuHienTai.transform.position;
                        if (NavMesh.SamplePosition(mucTieuHienTai.transform.position, out NavMeshHit pNavHit, 6f, NavMesh.AllAreas))
                        {
                            targetNavPos = pNavHit.position;
                        }
                        agent.SetDestination(targetNavPos);
                        updatePathTimer = TickTimer.CreateFromSeconds(Runner, 0.25f);
                    }

                    // Chống kẹt khi rượt đuổi trên địa hình dốc
                    if (Vector3.Distance(transform.position, lastStuckPos) < 0.08f)
                    {
                        stuckTimer += Runner.DeltaTime;
                        if (stuckTimer >= 2.5f)
                        {
                            if (NavMesh.SamplePosition(transform.position, out NavMeshHit unStuckHit, 4f, NavMesh.AllAreas))
                            {
                                agent.Warp(unStuckHit.position);
                            }
                            stuckTimer = 0f;
                        }
                    }
                    else
                    {
                        stuckTimer = 0f;
                        lastStuckPos = transform.position;
                    }

                    float khoangCachToiPlayer = Vector3.Distance(transform.position, mucTieuHienTai.transform.position);
                    float tamDanhThucTe = Mathf.Max(banKinhTanCong * heSoScale, agent.radius + 1.2f);
                    if (khoangCachToiPlayer <= tamDanhThucTe)
                    {
                        currentState = BossState.TanCong;
                        agent.isStopped = true;
                    }
                }
                break;
            case BossState.TanCong:
                float tamDanh = Mathf.Max(banKinhTanCong * heSoScale, agent.radius + 1.2f);
                if (mucTieuHienTai == null || Vector3.Distance(transform.position, mucTieuHienTai.transform.position) > tamDanh + 1.0f)
                {
                    currentState = BossState.DiTheo;
                    agent.isStopped = false;
                    updatePathTimer = TickTimer.None;
                    stuckTimer = 0f;
                    lastStuckPos = transform.position;
                }
                else
                {
                    Vector3 huongNhin = (mucTieuHienTai.transform.position - transform.position).normalized;
                    huongNhin.y = 0;
                    if (huongNhin != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(huongNhin), Runner.DeltaTime * 10f);
                    }
                    if (attackTimer.ExpiredOrNotRunning(Runner)) ThucHienDanhPlayer();
                }
                break;
        }
    }

    private void TimMucTieuGanNhat()
    {
        if (scanTargetTimer.IsRunning && !scanTargetTimer.Expired(Runner)) return;
        scanTargetTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);

        float tamQuet = Mathf.Max(banKinhPhatHien * heSoScale, 15f);
        int hitCountNonAlloc = Physics.OverlapSphereNonAlloc(transform.position, tamQuet, scanResults);

        Player_Controller mucTieuGanNhat = null;
        float khoangCachGanNhat = float.MaxValue;

        for (int i = 0; i < hitCountNonAlloc; i++)
        {
            Collider col = scanResults[i];
            if (col == null) continue;

            Player_Controller player = col.GetComponentInParent<Player_Controller>();
            if (player != null && !player.isDead)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist < khoangCachGanNhat)
                {
                    khoangCachGanNhat = dist;
                    mucTieuGanNhat = player;
                }
            }
        }

        System.Array.Clear(scanResults, 0, hitCountNonAlloc);

        // Fallback quét danh sách player trong scene
        if (mucTieuGanNhat == null)
        {
            Player_Controller[] allPlayers = FindObjectsOfType<Player_Controller>();
            foreach (var p in allPlayers)
            {
                if (p != null && !p.isDead)
                {
                    float dist = Vector3.Distance(transform.position, p.transform.position);
                    if (dist <= tamQuet && dist < khoangCachGanNhat)
                    {
                        khoangCachGanNhat = dist;
                        mucTieuGanNhat = p;
                    }
                }
            }
        }

        mucTieuHienTai = mucTieuGanNhat;
    }

    private void PhatSinhDiemTuanTraMoi()
    {
        if (agent == null || !agent.enabled) return;

        float tamTuanTraThucTe = banKinhTuanTra * heSoScale;
        for (int i = 0; i < 6; i++)
        {
            Vector2 circle = Random.insideUnitCircle * tamTuanTraThucTe;
            if (circle.magnitude < 3f) circle = circle.normalized * 3.5f;

            Vector3 testPos = viTriGoc + new Vector3(circle.x, 25f, circle.y);
            Vector3 targetPos = viTriGoc + new Vector3(circle.x, 0f, circle.y);

            if (Physics.Raycast(testPos, Vector3.down, out RaycastHit hitInfo, 50f, ~((1 << 13) | (1 << 14))))
            {
                targetPos.y = hitInfo.point.y;
            }

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 8f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(navHit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.isStopped = false;
                    agent.SetPath(path);
                    return;
                }
            }
        }

        agent.SetDestination(viTriGoc);
    }

    private void ThucHienDanhPlayer()
    {
        hitCount++;
        if (hitCount >= soDonDeTungSkill && skillCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            RPC_AnimSkill();
            hitCount = 0;
            attackTimer = TickTimer.CreateFromSeconds(Runner, thoiGianHoiSkill);
            skillCooldownTimer = TickTimer.CreateFromSeconds(Runner, skillCooldown);
        }
        else
        {
            RPC_AnimAttack();
            attackTimer = TickTimer.CreateFromSeconds(Runner, thoiGianHoiDon);
        }
    }
    #endregion

    #region ANIMATION EVENTS (GỌI TỪ ANIMATOR)
    private void SpawnAttackVFX()
    {
        if (attackVFXPrefab != null)
        {
            Vector3 spawnPos = vfxSpawnPoint != null ? vfxSpawnPoint.position : (transform.position + transform.forward * 1.5f + Vector3.up * 1f);
            Quaternion spawnRot = vfxSpawnPoint != null ? vfxSpawnPoint.rotation : transform.rotation;
            GameObject vfx = Instantiate(attackVFXPrefab, spawnPos, spawnRot);
            Destroy(vfx, 5f);
        }
    }

    private float lastSkillVfxTime = -999f;
    private void SpawnSkillVFX()
    {
        if (Time.time < lastSkillVfxTime + 3f) return;
        lastSkillVfxTime = Time.time;

        if (skillVFXPrefab != null)
        {
            Vector3 spawnPos = vfxSpawnPoint != null ? vfxSpawnPoint.position : (transform.position + transform.forward * 1.5f + Vector3.up * 1f);
            Quaternion spawnRot = vfxSpawnPoint != null ? vfxSpawnPoint.rotation : transform.rotation;
            GameObject vfx = Instantiate(skillVFXPrefab, spawnPos, spawnRot);
            Destroy(vfx, 3.5f);
        }
    }

    private float lastBossAttackTime = -999f;

    public void AnimEvent_DealNormalDamage()
    {
        SpawnAttackVFX();

        if (!HasStateAuthority) return;

        if (Time.time < lastBossAttackTime + 0.5f) return;
        lastBossAttackTime = Time.time;

        if (mucTieuHienTai != null && !mucTieuHienTai.isDead)
        {
            float distance = Vector3.Distance(transform.position, mucTieuHienTai.transform.position);
            float tamDanh = Mathf.Max(banKinhTanCong * heSoScale, (agent != null ? agent.radius : 0.5f) + 1.2f) + 1.5f;
            if (distance <= tamDanh)
            {
                mucTieuHienTai.Server_TakeDamageFromBoss(attackDamage);
            }
        }
    }

    public void AnimEvent_DealSkillDamage()
    {
        SpawnSkillVFX();

        if (!HasStateAuthority) return;

        if (mucTieuHienTai != null && !mucTieuHienTai.isDead)
        {
            float distance = Vector3.Distance(transform.position, mucTieuHienTai.transform.position);
            float tamDanh = Mathf.Max(banKinhTanCong * heSoScale, (agent != null ? agent.radius : 0.5f) + 1.2f) + 2.5f;
            if (distance <= tamDanh)
            {
                mucTieuHienTai.Server_TakeDamageFromBoss(skillDamage);
            }
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
            if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;
            RPC_AnimDead();
            despawnTimer = TickTimer.CreateFromSeconds(Runner, thoiGianBienMat);

            GiveExpToKiller(info.Source, expReward);

            if (Player_QuestManager.localQuest != null)
            {
                Player_QuestManager.localQuest.TangTienDoNhiemVu(LoaiNhiemVu.TieuDietQuai, bossID, 1);
            }

            DropItem();
        }
        else
        {
            if (mienChoangTimer.ExpiredOrNotRunning(Runner))
            {
                currentState = BossState.BiDanh;
                if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;
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

    private void DropItem()
    {
        if (!HasStateAuthority) return;

        if (dropItems != null && dropItems.Count > 0 && Random.Range(0f, 100f) <= dropChance)
        {
            GameObject itemToDrop = dropItems[Random.Range(0, dropItems.Count)];
            if (itemToDrop != null)
            {
                NetworkObject netObj = itemToDrop.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    Runner.Spawn(netObj, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                }
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnimAttack()
    {
        if (animator != null) animator.SetTrigger(hashAttack);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnimSkill()
    {
        if (animator != null) animator.SetTrigger(hashSkill);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnimHurt()
    {
        if (animator != null)
        {
            animator.ResetTrigger(hashHit);
            animator.SetTrigger(hashHit);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnimDead()
    {
        if (animator != null)
        {
            animator.ResetTrigger(hashHit);
            animator.ResetTrigger(hashAttack);
            animator.ResetTrigger(hashSkill);
            animator.SetBool(hashIsDead, true);
        }
    }

    public void AnimEvent_PlayAttackSound()
    {
        PlaySound(attackSound);
    }

    public void AnimEvent_PlayHurtSound()
    {
        PlaySound(hurtSound);
    }

    public void AnimEvent_PlayDeathSound()
    {
        PlaySound(deathSound);
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        float tiLe = Application.isPlaying ? heSoScale : Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(Application.isPlaying ? viTriGoc : transform.position, banKinhTuanTra * tiLe);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, Mathf.Max(banKinhPhatHien * tiLe, 15f));
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, banKinhTanCong * tiLe);
    }
}
