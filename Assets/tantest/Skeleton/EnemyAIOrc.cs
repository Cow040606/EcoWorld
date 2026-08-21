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
    public int enemyID = 1;
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
    public float detectionRadius = 8f;
    public float loseRadius = 15f;
    public float attackRadius = 2f;
    public float idleWaitTime = 5f;
    public float screamDuration = 2f;
    public float attackCooldown = 1.0f;

    [Header("UI Component Settings")]
    public Canvas healthCanvas;
    public Slider healthSlider;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;

    [Header("Drop Settings")]
    public List<GameObject> dropItems;
    [Range(0f, 100f)] public float dropChance = 100f;

    [Networked] private TickTimer despawnTimer { get; set; }

    [Networked] private Vector3 NetworkPosition { get; set; }
    [Networked] private Quaternion NetworkRotation { get; set; }

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 startPosition;
    private float stateTimer = 0f;
    private Transform targetPlayer;
    private Camera mainCamera;
    private Collider[] detectionResults = new Collider[8];

    public int GetMaxHealth(int level) => baseHealth + ((Mathf.Max(1, level) - 1) * healthPerLevel);
    public float GetDamage(int level) => baseDamage + ((Mathf.Max(1, level) - 1) * damagePerLevel);

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();

        // FIX 1: Tìm Animator ở cả object cha lẫn con để tránh bị null
        animator = GetComponentInChildren<Animator>();

        startPosition = transform.position;
        mainCamera = Camera.main;

        // Tự động tìm hoặc thêm AudioSource nếu chưa kéo thả
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // Âm thanh 3D
            audioSource.minDistance = 1f;
            audioSource.maxDistance = loseRadius > 0 ? loseRadius : 15f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        if (HasStateAuthority)
        {
            NetworkPosition = transform.position;
            NetworkRotation = transform.rotation;

            if (agent != null)
            {
                agent.Warp(transform.position);
                agent.enabled = true;
            }
            NetworkedLevel = Random.Range(minLevel, maxLevel + 1);
            Health = GetMaxHealth(NetworkedLevel);
            CurrentState = EnemyState.Idle;
        }
        else
        {
            if (agent != null) agent.enabled = false;
            transform.position = NetworkPosition;
            transform.rotation = NetworkRotation;
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
        if (HasStateAuthority)
        {
            NetworkPosition = transform.position;
            NetworkRotation = transform.rotation;
        }

        if (!HasStateAuthority) return;

        // FIX 2: Khởi tạo TickTimer trong FUN (Simulation) thay vì RPC để đảm bảo luôn chạy
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
            return; // Đảm bảo quái chết không chạy các logic AI bên dưới
        }

        switch (CurrentState)
        {
            case EnemyState.Idle:
                stateTimer += Runner.DeltaTime;
                if (stateTimer >= idleWaitTime) StartPatrol();
                DetectPlayer();
                break;
            case EnemyState.Patrol:
                if (IsAgentValid() && !agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    CurrentState = EnemyState.Idle;
                    stateTimer = 0f;
                }
                DetectPlayer();
                break;
            case EnemyState.Scream:
                stateTimer += Runner.DeltaTime;
                if (stateTimer >= screamDuration) CurrentState = EnemyState.Chase;
                break;
            case EnemyState.Chase:
                if (targetPlayer != null)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
                    if (distanceToPlayer <= attackRadius)
                    {
                        CurrentState = EnemyState.Attack;
                        stateTimer = 0f;
                        if (IsAgentValid()) agent.isStopped = true;
                        RPC_PlayAttackAnim();
                    }
                    else if (distanceToPlayer > loseRadius)
                    {
                        targetPlayer = null;
                        CurrentState = EnemyState.Return;
                        if (IsAgentValid())
                        {
                            agent.isStopped = false;
                            agent.SetDestination(startPosition);
                        }
                    }
                    else
                    {
                        if (IsAgentValid())
                        {
                            agent.isStopped = false;
                            agent.SetDestination(targetPlayer.position);
                        }
                    }
                }
                else
                {
                    CurrentState = EnemyState.Return;
                    if (IsAgentValid())
                    {
                        agent.isStopped = false;
                        agent.SetDestination(startPosition);
                    }
                }
                break;
            case EnemyState.Attack:
                if (targetPlayer != null)
                {
                    transform.LookAt(new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z));
                    stateTimer += Runner.DeltaTime;
                    if (stateTimer >= attackCooldown)
                    {
                        float dist = Vector3.Distance(transform.position, targetPlayer.position);
                        if (dist > attackRadius)
                        {
                            CurrentState = EnemyState.Chase;
                            if (IsAgentValid()) agent.isStopped = false;
                        }
                        else
                        {
                            stateTimer = 0f;
                            RPC_PlayAttackAnim();
                        }
                    }
                }
                else CurrentState = EnemyState.Return;
                break;
            case EnemyState.Return:
                if (IsAgentValid() && !agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    CurrentState = EnemyState.Idle;
                    stateTimer = 0f;
                }
                DetectPlayer();
                break;
        }
    }

    public void EnemyDoDamage()
    {
        if (targetPlayer != null)
        {
            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            if (dist <= attackRadius + 1f)
            {
                Player_Controller player = targetPlayer.GetComponent<Player_Controller>();
                if (player != null) player.RPC_TakeDame(GetDamage(NetworkedLevel));
            }
        }
    }

    private void StartPatrol()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, 1))
        {
            if (IsAgentValid())
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                CurrentState = EnemyState.Patrol;
            }
        }
    }

    private void DetectPlayer()
    {
        // Phân chia tần suất (Throttling): chỉ quét mỗi 10 ticks (khoảng 6 lần/giây) để giảm tải CPU
        if (Runner.Tick % 10 != 0) return;

        // Quét không cấp phát bộ nhớ (NonAlloc) để triệt tiêu hoàn toàn rác thải bộ nhớ GC
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, detectionResults);
        for (int i = 0; i < numHits; i++)
        {
            Collider hit = detectionResults[i];
            if (hit != null && hit.CompareTag("Player"))
            {
                Player_Controller player = hit.GetComponent<Player_Controller>();
                if (player != null && player.isDead) continue;
                targetPlayer = hit.transform;
                CurrentState = EnemyState.Scream;
                stateTimer = 0f;
                if (IsAgentValid()) agent.isStopped = true;
                transform.LookAt(new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z));
                break;
            }
        }
        // Dọn dẹp mảng để tránh giữ tham chiếu đối tượng
        System.Array.Clear(detectionResults, 0, numHits);
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

            // ----> CODE GỌI QUEST VỪA THÊM VÀO ĐÂY <----
            // Truy cập thẳng vào localQuest và gọi hàm tăng tiến độ
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

    public override void Render()
    {
        if (!HasStateAuthority)
        {
            transform.position = Vector3.Lerp(transform.position, NetworkPosition, Runner.DeltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, NetworkRotation, Runner.DeltaTime * 15f);
        }
    }

    public void OnStateChanged()
    {
        // FIX 3: Đưa logic tắt Collider và Thanh máu lên đầu để luôn được chạy
        if (CurrentState == EnemyState.Dead)
        {
            Collider[] colliders = GetComponents<Collider>();
            foreach (var col in colliders) col.enabled = false;

            if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);
        }

        // Nếu không có animator thì thoát ngang tại đây, đảm bảo quái vẫn vô hình/mất va chạm
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
    // ANIMATION EVENTS (GỌI TỪ KHUNG HÌNH HOẠT ẢNH TRONG ANIMATION WINDOW)
    // HƯỚNG DẪN: Mở Animation Window của Unity, chọn frame tương ứng và tạo Event gọi các hàm này.
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
        PlaySound(screamSound);
    }

    public void AnimEvent_PlayDeathSound()
    {
        PlaySound(deathSound);
    }
}