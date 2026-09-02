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
    public float idleWaitTime = 4f;
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

    public int GetMaxHealth(int level) => baseHealth + ((Mathf.Max(1, level) - 1) * healthPerLevel);
    public float GetDamage(int level) => baseDamage + ((Mathf.Max(1, level) - 1) * damagePerLevel);

    private void Awake()
    {
        // Loại bỏ va chạm vật lý giữa Enemy với Enemy và giữa Enemy với Động vật (Gà, Chó)
        // để tránh tình trạng các quái ép nhau, xô đẩy nhau văng khỏi sàn
        Physics.IgnoreLayerCollision(13, 13, true); // 13: Enemy
        Physics.IgnoreLayerCollision(13, 14, true); // 14: Animal
    }

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;

        // Khóa Rigidbody để PhysX không tác động lực đẩy văng hay kéo tụt trọng lực
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Cấu hình NavMeshAgent linh hoạt trên địa hình gồ ghề
        if (agent != null)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(30, 70);
            agent.angularSpeed = 360f; // Quay người nhanh, không bị đơ khi đổi hướng
            agent.acceleration = 16f;  // Khởi động di chuyển nhanh
            agent.speed = walkSpeed;
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
            startPosition = transform.position;

            if (agent != null)
            {
                agent.enabled = true;
                if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(navHit.position);
                    startPosition = navHit.position;
                }
                agent.isStopped = true; // Dừng yên tại chỗ ban đầu
            }

            NetworkedLevel = Random.Range(minLevel, maxLevel + 1);
            Health = GetMaxHealth(NetworkedLevel);
            CurrentState = EnemyState.Idle;
            stateTimer = 0f;
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

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

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

        // Lưới bảo hiểm: Nếu quái bị rớt khỏi độ cao sàn hoặc bị đẩy quá xa điểm spawn
        if (transform.position.y < startPosition.y - 8f || Vector3.Distance(transform.position, startPosition) > Mathf.Max(loseRadius * 2f, 50f))
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
                    if (!agent.pathPending && agent.remainingDistance < 0.6f)
                    {
                        CurrentState = EnemyState.Idle;
                        stateTimer = 0f;
                        agent.isStopped = true;
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

                        // Quay người tức thì về hướng mục tiêu để không bị khựng đơ khi rẽ hướng trên địa hình
                        Vector3 lookDir = targetPlayer.position - transform.position;
                        lookDir.y = 0;
                        if (lookDir != Vector3.zero)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Runner.DeltaTime * 12f);
                        }

                        if (updatePathTimer.ExpiredOrNotRunning(Runner))
                        {
                            Vector3 targetNavPos = targetPlayer.position;
                            // Luôn snap điểm đích vào NavMesh gần nhất dưới chân Player để vượt địa hình dốc/mấp mô
                            if (NavMesh.SamplePosition(targetPlayer.position, out NavMeshHit pNavHit, 6f, NavMesh.AllAreas))
                            {
                                targetNavPos = pNavHit.position;
                            }

                            agent.SetDestination(targetNavPos);
                            updatePathTimer = TickTimer.CreateFromSeconds(Runner, 0.25f);
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
                        float attackRange = Mathf.Max(attackRadius, (agent != null ? agent.radius : 0.5f) + 0.8f);
                        float dist = Vector3.Distance(transform.position, targetPlayer.position);
                        if (dist > attackRange + 0.8f)
                        {
                            CurrentState = EnemyState.Chase;
                            if (IsAgentValid())
                            {
                                agent.isStopped = false;
                                agent.speed = chaseSpeed;
                            }
                            updatePathTimer = TickTimer.None;
                        }
                        else
                        {
                            stateTimer = 0f;
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
                    agent.isStopped = false;
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

    public void EnemyDoDamage()
    {
        if (!HasStateAuthority) return;

        Transform target = targetPlayer;
        if (target == null)
        {
            Player_Controller[] players = FindObjectsOfType<Player_Controller>();
            foreach (var p in players)
            {
                if (p != null && !p.isDead && Vector3.Distance(transform.position, p.transform.position) <= attackRadius + 2f)
                {
                    target = p.transform;
                    break;
                }
            }
        }

        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= attackRadius + 2f)
            {
                Player_Controller player = target.GetComponentInParent<Player_Controller>();
                if (player != null && !player.isDead)
                {
                    player.RPC_TakeDame(GetDamage(NetworkedLevel));
                }
            }
        }
    }

    private void StartPatrol()
    {
        Vector2 rnd = Random.insideUnitCircle * patrolRadius;
        Vector3 randomDirection = new Vector3(rnd.x, 0, rnd.y);
        Vector3 targetPos = startPosition + randomDirection;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, patrolRadius * 1.5f, NavMesh.AllAreas))
        {
            if (IsAgentValid())
            {
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.SetDestination(hit.position);
                CurrentState = EnemyState.Patrol;
            }
        }
    }

    private void DetectPlayer()
    {
        if (Runner.Tick % 5 != 0) return; // Quét mỗi 5 ticks (nhanh hơn để bắt kịp chuyển động)

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

        // Dự phòng nếu OverlapSphere bị cản bởi collider cảnh quan
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

        // KHI PHÁT HIỆN PLAYER
        if (foundPlayer != null)
        {
            targetPlayer = foundPlayer.transform;

            // 1. Chỉ phát âm thanh phát hiện 1 lần duy nhất khi vừa phát hiện (có hồi chiêu 10s để chống spam)
            if (Time.time >= lastDetectSoundTime + 10f)
            {
                lastDetectSoundTime = Time.time;
                RPC_PlayDetectSound();
            }

            // 2. Chuyển NGAY LẬP TỨC sang trạng thái Chase để dí player, không đứng 1 chỗ chạy tại chỗ
            CurrentState = EnemyState.Chase;
            stateTimer = 0f;
            updatePathTimer = TickTimer.None;

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

            // TRẢ KINH NGHIỆM CHO NGƯỜI KẾT LIỄU
            GiveExpToKiller(info.Source, expReward);

            // ----> CODE GỌI QUEST <----
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
        }

        if (animator == null) return;

        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);

        switch (CurrentState)
        {
            case EnemyState.Patrol:
            case EnemyState.Return:
                animator.SetBool("isWalking", true); break;
            case EnemyState.Chase:
                animator.SetBool("isRunning", true); break;
            case EnemyState.Scream:
                animator.SetTrigger("scream"); 
                break;
            case EnemyState.Dead:
                animator.SetTrigger("death");
                break;
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
        // Âm thanh phát hiện đã được phát chính xác 1 lần trong code khi phát hiện Player
    }

    public void AnimEvent_PlayDeathSound()
    {
        PlaySound(deathSound);
    }
}
