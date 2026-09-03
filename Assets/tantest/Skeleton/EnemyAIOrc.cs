using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class EnemyAIOrc : NetworkBehaviour
{
    public enum EnemyState { Idle, Patrol, Scream, Chase, Attack, Return, Dead }

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public EnemyState CurrentState { get; set; }

    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }

    [Networked, OnChangedRender(nameof(OnLevelChanged))]
    public int NetworkedLevel { get; set; }

    // Đồng bộ vận tốc di chuyển thực tế cho Client render animation
    [Networked] public float CurrentMoveSpeed { get; set; }

    [Networked] public Vector3 NetworkSpawnPosition { get; set; }

    public void SetSpawnPosition(Vector3 pos)
    {
        if (pos.sqrMagnitude > 1f)
        {
            startPosition = pos;
            NetworkSpawnPosition = pos;
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

    [Header("Enemy Info")]
    [Tooltip("ID của quái vật. Dùng để đối chiếu với targetID trong Nhiệm Vụ.")]
    public int enemyID = 10;
    public string enemyName = "Skeleton";

    [Header("Thưởng Kinh Nghiệm")]
    public float expReward = 50f;

    [Header("Audio Settings")]
    [Tooltip("AudioSource để phát âm thanh của quái. Nếu để trống sẽ tự động tìm hoặc thêm mới.")]
    public AudioSource audioSource;
    public AudioClip attackSound; // atk
    public AudioClip hurtSound;   // hurt
    public AudioClip screamSound; // argy (scream)
    public AudioClip deathSound;  // die

    [Header("Level & Stats Settings")]
    public int minLevel = 1;
    public int maxLevel = 5;
    public int baseHealth = 100;
    public int healthPerLevel = 20;
    public float baseDamage = 15f;
    public float damagePerLevel = 3f;

    [Header("AI Settings")]
    public float patrolRadius = 10f;
    public float detectionRadius = 16f;
    public float loseRadius = 32f;
    public float attackRadius = 2f;
    public float idleWaitTime = 3f;
    public float attackCooldown = 1.0f;
    public float chaseSpeed = 4.5f;
    public float walkSpeed = 2.5f;

    [Header("UI Component Settings")]
    public Canvas healthCanvas;
    public Slider healthSlider;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;

    [Header("Drop Settings")]
    public List<GameObject> dropItems;
    [Range(0f, 100f)] public float dropChance = 100f;

    [Networked] private TickTimer despawnTimer { get; set; }

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 startPosition;
    private float stateTimer = 0f;
    private Transform targetPlayer;
    private Camera mainCamera;
    private Collider[] detectionResults = new Collider[32];
    private TickTimer updatePathTimer;
    private float lastDetectSoundTime = -999f;

    // Biến chống kẹt và đệm tính toán đường
    private float patrolWaitPathTimer = 0f;
    private float stuckTimer = 0f;
    private Vector3 lastStuckPos = Vector3.zero;

    public int GetMaxHealth(int level) => baseHealth + ((Mathf.Max(1, level) - 1) * healthPerLevel);
    public float GetDamage(int level) => baseDamage + ((Mathf.Max(1, level) - 1) * damagePerLevel);

    private void Awake()
    {
        // 1. Loại bỏ va chạm vật lý giữa Enemy với nhau và với động vật
        Physics.IgnoreLayerCollision(13, 13, true); // 13: Enemy
        Physics.IgnoreLayerCollision(13, 14, true); // 14: Animal
        // 2. Bỏ qua va chạm vật lý với Layer 0 (Default/Terrain) để NavMeshAgent toàn quyền di chuyển mượt mà trên dốc
        Physics.IgnoreLayerCollision(13, 0, true);

        // 3. Đảm bảo CapsuleCollider là Trigger an toàn và nâng đáy khỏi mặt đất
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.center = new Vector3(0f, 1.0f, 0f);
            col.height = 1.8f;
            col.radius = 0.4f;
        }
    }

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;

        // Tắt Root Motion để NavMeshAgent kiểm soát vị trí chính xác
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // Khóa Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Cấu hình NavMeshAgent cực nhạy và tối ưu trên địa hình nhấp nhô
        if (agent != null)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(10, 90);
            agent.radius = 0.45f;
            agent.angularSpeed = 720f;  // Xoay tức thì, không bị khựng đơ tại chỗ khi rẽ
            agent.acceleration = 45f;   // Khởi động di chuyển lập tức, triệt tiêu độ trễ
            agent.baseOffset = 0.05f;   // Nâng nhẹ chân tránh cọ lún vào gờ mép dốc
            agent.autoRepath = true;
            agent.speed = walkSpeed;
            agent.stoppingDistance = 0.2f;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = loseRadius > 0 ? loseRadius : 25f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        if (HasStateAuthority)
        {
            if (NetworkSpawnPosition.sqrMagnitude > 1f)
            {
                startPosition = NetworkSpawnPosition;
            }
            else if (transform.position.sqrMagnitude > 1f)
            {
                startPosition = transform.position;
                NetworkSpawnPosition = transform.position;
            }

            if (agent != null)
            {
                agent.enabled = true;
                if (!agent.isOnNavMesh)
                {
                    Vector3 testPos = startPosition.sqrMagnitude > 1f ? startPosition : transform.position;
                    if (testPos.sqrMagnitude > 1f && NavMesh.SamplePosition(testPos, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
                    {
                        agent.Warp(navHit.position);
                        startPosition = navHit.position;
                        NetworkSpawnPosition = navHit.position;
                    }
                }
                agent.isStopped = true;
            }

            NetworkedLevel = Random.Range(minLevel, maxLevel + 1);
            Health = GetMaxHealth(NetworkedLevel);
            CurrentState = EnemyState.Idle;
            stateTimer = 0f;
            lastStuckPos = transform.position;
        }
        else
        {
            if (agent != null) agent.enabled = false;
        }

        if (nameText != null) nameText.text = enemyName;
        OnLevelChanged();
        OnHealthChanged();
    }

    private void LateUpdate()
    {
        if (healthCanvas != null && mainCamera != null && CurrentState != EnemyState.Dead)
            healthCanvas.transform.forward = mainCamera.transform.forward;
    }

    private bool IsAgentValid() => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    public override void Render()
    {
        if (animator == null || CurrentState == EnemyState.Dead) return;

        // Vận tốc di chuyển thực tế: Authority đọc từ agent.velocity, Client đọc từ networked CurrentMoveSpeed
        float speed = HasStateAuthority && IsAgentValid() ? agent.velocity.magnitude : CurrentMoveSpeed;

        // CHỈ PHÁT ANIMATION KHI THỰC SỰ DI CHUYỂN
        bool isMoving = speed > 0.2f;

        if (isMoving)
        {
            if (CurrentState == EnemyState.Chase)
            {
                animator.SetBool("isRunning", true);
                animator.SetBool("isWalking", false);
            }
            else
            {
                animator.SetBool("isWalking", true);
                animator.SetBool("isRunning", false);
            }
        }
        else
        {
            // Đang đứng yên, xoay người hoặc đợi lệnh -> chuyển về IDLE ngay lập tức, không diễn hoạt ảnh di chuyển tại chỗ
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // Cập nhật vận tốc thực tế để đồng bộ cho các Client
        CurrentMoveSpeed = IsAgentValid() ? agent.velocity.magnitude : 0f;

        if (CurrentState == EnemyState.Dead)
        {
            if (!despawnTimer.IsRunning)
            {
                despawnTimer = TickTimer.CreateFromSeconds(Runner, 5f);
            }

            if (despawnTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
            return;
        }

        // Cập nhật startPosition an toàn nếu lúc Spawned chưa kịp nhận tọa độ
        if (startPosition.sqrMagnitude < 1f)
        {
            if (NetworkSpawnPosition.sqrMagnitude > 1f)
            {
                startPosition = NetworkSpawnPosition;
            }
            else if (transform.position.sqrMagnitude > 1f)
            {
                startPosition = transform.position;
                NetworkSpawnPosition = transform.position;
            }
        }

        // Lưới bảo hiểm: CHỈ khi quái thực sự rơi lọt qua map xuống vực sâu (Y < -30)
        // và đã có startPosition hợp lệ trong map (không bị warp về 0,0,0)
        if (transform.position.y < -30f && startPosition.sqrMagnitude > 10f)
        {
            if (NavMesh.SamplePosition(startPosition, out NavMeshHit safeHit, 15f, NavMesh.AllAreas))
            {
                if (IsAgentValid())
                {
                    agent.Warp(safeHit.position);
                    agent.isStopped = true;
                }
                else
                {
                    transform.position = safeHit.position;
                }
                CurrentState = EnemyState.Idle;
                stateTimer = 0f;
                return;
            }
        }

        switch (CurrentState)
        {
            case EnemyState.Idle:
                if (IsAgentValid())
                {
                    if (!agent.isStopped) agent.isStopped = true;
                    agent.speed = walkSpeed;
                }
                stateTimer += Runner.DeltaTime;
                if (stateTimer >= idleWaitTime)
                {
                    stateTimer = 0f;
                    StartPatrol();
                }
                DetectPlayer();
                break;

            case EnemyState.Patrol:
                if (IsAgentValid())
                {
                    if (agent.isStopped) agent.isStopped = false;
                    agent.speed = walkSpeed;

                    if (patrolWaitPathTimer > 0f)
                    {
                        patrolWaitPathTimer -= Runner.DeltaTime;
                    }
                    else
                    {
                        // Kiểm tra kẹt địa hình khi tuần tra
                        if (Vector3.Distance(transform.position, lastStuckPos) < 0.1f)
                        {
                            stuckTimer += Runner.DeltaTime;
                            if (stuckTimer >= 3.0f) // Quá 3s không di chuyển được
                            {
                                CurrentState = EnemyState.Idle;
                                stateTimer = 0f;
                                agent.isStopped = true;
                                stuckTimer = 0f;
                            }
                        }
                        else
                        {
                            stuckTimer = 0f;
                            lastStuckPos = transform.position;
                        }

                        if (!agent.pathPending && agent.remainingDistance < 0.6f)
                        {
                            CurrentState = EnemyState.Idle;
                            stateTimer = 0f;
                            agent.isStopped = true;
                        }
                    }
                }
                DetectPlayer();
                break;

            case EnemyState.Scream:
                CurrentState = EnemyState.Chase;
                if (IsAgentValid()) agent.isStopped = false;
                updatePathTimer = TickTimer.None;
                break;

            case EnemyState.Chase:
                if (targetPlayer == null)
                {
                    DetectPlayer();
                    if (targetPlayer == null)
                    {
                        CurrentState = EnemyState.Return;
                        break;
                    }
                }

                Player_Controller pc = targetPlayer.GetComponentInParent<Player_Controller>();
                if (pc != null && pc.isDead)
                {
                    targetPlayer = null;
                    CurrentState = EnemyState.Return;
                    if (IsAgentValid())
                    {
                        agent.isStopped = false;
                        agent.speed = walkSpeed;
                        agent.SetDestination(startPosition);
                    }
                    break;
                }

                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
                float actualAttackRadius = Mathf.Max(attackRadius, (agent != null ? agent.radius : 0.5f) + 0.8f);
                float effectiveLoseRadius = Mathf.Max(loseRadius, 32f);

                if (distanceToPlayer <= actualAttackRadius)
                {
                    CurrentState = EnemyState.Attack;
                    stateTimer = 0f;
                    if (IsAgentValid()) agent.isStopped = true;
                    RPC_PlayAttackAnim();
                }
                else if (distanceToPlayer > effectiveLoseRadius)
                {
                    targetPlayer = null;
                    CurrentState = EnemyState.Return;
                    if (IsAgentValid())
                    {
                        agent.isStopped = false;
                        agent.speed = walkSpeed;
                        agent.SetDestination(startPosition);
                    }
                }
                else
                {
                    if (IsAgentValid())
                    {
                        if (agent.isStopped) agent.isStopped = false;
                        agent.speed = chaseSpeed;

                        if (updatePathTimer.ExpiredOrNotRunning(Runner))
                        {
                            Vector3 targetNavPos = targetPlayer.position;
                            if (NavMesh.SamplePosition(targetPlayer.position, out NavMeshHit pNavHit, 6f, NavMesh.AllAreas))
                            {
                                targetNavPos = pNavHit.position;
                            }

                            agent.SetDestination(targetNavPos);
                            updatePathTimer = TickTimer.CreateFromSeconds(Runner, 0.25f);
                        }

                        // Tự gỡ kẹt khi đang rượt đuổi trên địa hình nhấp nhô
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
                    }
                }
                break;

            case EnemyState.Attack:
                if (targetPlayer != null)
                {
                    if (IsAgentValid() && !agent.isStopped) agent.isStopped = true;

                    Player_Controller playerCtrl = targetPlayer.GetComponentInParent<Player_Controller>();
                    if (playerCtrl != null && playerCtrl.isDead)
                    {
                        targetPlayer = null;
                        CurrentState = EnemyState.Return;
                        if (IsAgentValid())
                        {
                            agent.isStopped = false;
                            agent.speed = walkSpeed;
                            agent.SetDestination(startPosition);
                        }
                        break;
                    }

                    Vector3 lookPos = new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z);
                    if (lookPos != transform.position) transform.LookAt(lookPos);

                    stateTimer += Runner.DeltaTime;
                    if (stateTimer >= attackCooldown)
                    {
                        stateTimer = 0f;
                        float dist = Vector3.Distance(transform.position, targetPlayer.position);
                        float inAtkRadius = Mathf.Max(attackRadius, (agent != null ? agent.radius : 0.5f) + 0.8f);
                        if (dist > inAtkRadius)
                        {
                            CurrentState = EnemyState.Chase;
                            if (IsAgentValid())
                            {
                                agent.isStopped = false;
                                agent.speed = chaseSpeed;
                            }
                        }
                        else
                        {
                            RPC_PlayAttackAnim();
                        }
                    }
                }
                else
                {
                    CurrentState = EnemyState.Return;
                }
                break;

            case EnemyState.Return:
                if (IsAgentValid())
                {
                    if (agent.isStopped) agent.isStopped = false;
                    agent.speed = walkSpeed;
                    if (!agent.pathPending && agent.remainingDistance < 0.6f)
                    {
                        CurrentState = EnemyState.Idle;
                        stateTimer = 0f;
                        agent.isStopped = true;
                    }
                }
                DetectPlayer();
                break;
        }
    }

    private float lastDamageSwingTime = -999f;
    private HashSet<Player_Controller> hitPlayersThisSwing = new HashSet<Player_Controller>();

    public void EnemyDoDamage() => AnimEvent_DealDamage();

    public void AnimEvent_DealDamage()
    {
        if (!HasStateAuthority || CurrentState == EnemyState.Dead) return;

        // Chống lặp sát thương: Đảm bảo cú chém của quái này chỉ gây sát thương 1 lần trong 0.5 giây
        if (Time.time < lastDamageSwingTime + 0.5f) return;
        lastDamageSwingTime = Time.time;

        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1.2f, attackRadius);
        hitPlayersThisSwing.Clear();

        foreach (var target in hits)
        {
            if (target == null) continue;

            Player_Controller player = target.GetComponentInParent<Player_Controller>();
            if (player != null && !player.isDead && !hitPlayersThisSwing.Contains(player))
            {
                hitPlayersThisSwing.Add(player);
                player.RPC_TakeDame(GetDamage(NetworkedLevel));
            }
        }
    }

    private void StartPatrol()
    {
        if (!IsAgentValid()) return;

        for (int attempts = 0; attempts < 6; attempts++)
        {
            Vector2 rnd = Random.insideUnitCircle * patrolRadius;
            if (rnd.magnitude < 2.5f) rnd = rnd.normalized * 3f;

            // Dò độ cao mặt đất thực tế bằng raycast từ trên cao xuống
            Vector3 rayStart = startPosition + new Vector3(rnd.x, 25f, rnd.y);
            Vector3 targetPos = startPosition + new Vector3(rnd.x, 0f, rnd.y);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, 50f, ~((1 << 13) | (1 << 14))))
            {
                targetPos.y = hitInfo.point.y;
            }

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.isStopped = false;
                    agent.speed = walkSpeed;
                    agent.SetPath(path);
                    CurrentState = EnemyState.Patrol;
                    stateTimer = 0f;
                    patrolWaitPathTimer = 0.4f; // Đệm thời gian không check remainingDistance ngay tick đầu
                    stuckTimer = 0f;
                    lastStuckPos = transform.position;
                    return;
                }
            }
        }

        // Nếu khu vực quá hiểm trở không tìm được đường, đứng nghỉ ngơi rồi thử lại sau
        CurrentState = EnemyState.Idle;
        stateTimer = 0f;
        agent.isStopped = true;
    }

    private void DetectPlayer()
    {
        if (Runner.Tick % 5 != 0) return;

        float effectiveRadius = Mathf.Max(detectionRadius, 16f);

        Player_Controller foundPlayer = null;
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, effectiveRadius, detectionResults);
        for (int i = 0; i < numHits; i++)
        {
            Collider hit = detectionResults[i];
            if (hit != null)
            {
                Player_Controller player = hit.GetComponentInParent<Player_Controller>();
                if (player != null && !player.isDead)
                {
                    foundPlayer = player;
                    break;
                }
            }
        }
        System.Array.Clear(detectionResults, 0, numHits);

        // Dự phòng quét trực tiếp danh sách players
        if (foundPlayer == null)
        {
            Player_Controller[] players = FindObjectsOfType<Player_Controller>();
            float minDistance = float.MaxValue;
            foreach (var p in players)
            {
                if (p != null && !p.isDead)
                {
                    float dist = Vector3.Distance(transform.position, p.transform.position);
                    if (dist <= effectiveRadius && dist < minDistance)
                    {
                        minDistance = dist;
                        foundPlayer = p;
                    }
                }
            }
        }

        if (foundPlayer != null)
        {
            targetPlayer = foundPlayer.transform;

            if (Time.time >= lastDetectSoundTime + 10f)
            {
                lastDetectSoundTime = Time.time;
                RPC_PlayDetectSound();
            }

            CurrentState = EnemyState.Chase;
            stateTimer = 0f;
            updatePathTimer = TickTimer.None;
            stuckTimer = 0f;
            lastStuckPos = transform.position;

            if (IsAgentValid())
            {
                agent.isStopped = false;
                agent.speed = chaseSpeed;

                Vector3 targetNavPos = targetPlayer.position;
                if (NavMesh.SamplePosition(targetPlayer.position, out NavMeshHit pNavHit, 6f, NavMesh.AllAreas))
                {
                    targetNavPos = pNavHit.position;
                }
                agent.SetDestination(targetNavPos);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDetectSound()
    {
        PlaySound(screamSound);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamageFromPlayer(int damage, RpcInfo info = default)
    {
        if (CurrentState == EnemyState.Dead) return;

        Health -= damage;
        if (Health <= 0)
        {
            CurrentState = EnemyState.Dead;
            if (IsAgentValid()) agent.isStopped = true;
            DropItem();

            GiveExpToKiller(info.Source, expReward);

            if (Player_QuestManager.localQuest != null)
            {
                Player_QuestManager.localQuest.TangTienDoNhiemVu(LoaiNhiemVu.TieuDietQuai, enemyID, 1);
            }
        }
        else
        {
            RPC_PlayTakeDamageAnim();
        }
    }

    private void GiveExpToKiller(PlayerRef playerRef, float expAmount)
    {
        if (!HasStateAuthority || playerRef == PlayerRef.None) return;

        NetworkObject playerObj = Runner.GetPlayerObject(playerRef);
        if (playerObj != null)
        {
            Player_Controller player = playerObj.GetComponent<Player_Controller>();
            if (player != null) player.Server_AddExp(expAmount);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void OnHealthChanged() { if (healthSlider != null) healthSlider.value = Health; }
    public void OnLevelChanged()
    {
        int safeLevel = Mathf.Max(1, NetworkedLevel);
        if (levelText != null) levelText.text = safeLevel.ToString();
        if (healthSlider != null) healthSlider.maxValue = GetMaxHealth(safeLevel);
    }

    private void DropItem()
    {
        if (dropItems != null && dropItems.Count > 0 && Random.Range(0f, 100f) <= dropChance)
        {
            GameObject itemToDrop = dropItems[Random.Range(0, dropItems.Count)];
            if (itemToDrop != null)
            {
                NetworkObject netObj = itemToDrop.GetComponent<NetworkObject>();
                if (netObj != null) Runner.Spawn(netObj, transform.position + Vector3.up * 1f, Quaternion.identity);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayTakeDamageAnim() 
    { 
        if (animator != null) animator.SetTrigger("takedame"); 
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAttackAnim() 
    { 
        if (animator != null) animator.SetTrigger("slash"); 
    }

    public void OnStateChanged()
    {
        if (CurrentState == EnemyState.Dead)
        {
            Collider[] colliders = GetComponents<Collider>();
            foreach (var col in colliders) col.enabled = false;

            if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);

            if (animator != null)
            {
                animator.SetBool("isWalking", false);
                animator.SetBool("isRunning", false);
                animator.SetTrigger("death");
            }
        }
        else if (CurrentState == EnemyState.Scream)
        {
            if (animator != null) animator.SetTrigger("scream");
        }
    }

    // =========================================================================
    // ANIMATION EVENTS
    // =========================================================================
    public void AnimEvent_PlayAttackSound()
    {
        PlaySound(attackSound);
    }

    public void AnimEvent_PlayHurtSound()
    {
        PlaySound(hurtSound);
    }

    public void AnimEvent_PlayScreamSound()
    {
    }

    public void AnimEvent_PlayDeathSound()
    {
        PlaySound(deathSound);
    }
}
