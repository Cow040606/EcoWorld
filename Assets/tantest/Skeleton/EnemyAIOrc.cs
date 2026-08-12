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

    // THÊM MỚI: Biến mạng đồng bộ Level giữa các người chơi
    [Networked, OnChangedRender(nameof(OnLevelChanged))]
    public int NetworkedLevel { get; set; }

    [Header("Enemy Info")]
    public string enemyName = "Skeleton";

    [Header("Level & Stats Settings (Cài đặt cấp độ và chỉ số)")]
    public int minLevel = 1;             // Cấp độ nhỏ nhất khi spawn
    public int maxLevel = 5;             // Cấp độ lớn nhất khi spawn

    public int baseHealth = 100;         // Máu cơ bản ở cấp 1
    public int healthPerLevel = 20;      // Mỗi cấp độ tăng thêm bao nhiêu máu?

    public float baseDamage = 15f;       // Sát thương cơ bản ở cấp 1
    public float damagePerLevel = 3f;    // Mỗi cấp độ tăng thêm bao nhiêu sát thương?

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

    [Range(0f, 100f)]
    public float dropChance = 100f;

    [Networked] private TickTimer despawnTimer { get; set; }

    // THÊM: Đồng bộ vị trí thủ công cho chế độ Online
    [Networked] private Vector3 NetworkPosition { get; set; }
    [Networked] private Quaternion NetworkRotation { get; set; }

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 startPosition;
    private float stateTimer = 0f;
    private Transform targetPlayer;
    private Camera mainCamera;

    // Các hàm tính toán chỉ số theo Level
    public int GetMaxHealth(int level) => baseHealth + ((Mathf.Max(1, level) - 1) * healthPerLevel);
    public float GetDamage(int level) => baseDamage + ((Mathf.Max(1, level) - 1) * damagePerLevel);

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        mainCamera = Camera.main;

        if (HasStateAuthority)
        {

            NetworkPosition = transform.position;
            NetworkRotation = transform.rotation;

            // Chỉ BẬT NavMeshAgent trên máy chủ (State Authority)


            if (agent != null)
            {
                agent.Warp(transform.position);
                agent.enabled = true;
            }

            // Random cấp độ và lưu vào biến mạng
            NetworkedLevel = Random.Range(minLevel, maxLevel + 1);

            // Tính toán lượng máu tối đa dựa trên cấp độ và gán máu hiện tại
            Health = GetMaxHealth(NetworkedLevel);

            CurrentState = EnemyState.Idle;
        }
        else
        {
            if (agent != null) agent.enabled = false;
            
            // Đặt ngay vị trí ban đầu để tránh bị trượt từ tọa độ 0
            transform.position = NetworkPosition;
            transform.rotation = NetworkRotation;
        }

        // --- KHỞI TẠO UI CHO MỌI NGƯỜI CHƠI ---
        if (nameText != null) nameText.text = enemyName;

        // Gọi cập nhật UI ban đầu
        OnLevelChanged();
        OnHealthChanged();
    }

    private void LateUpdate()
    {
        if (healthCanvas != null && mainCamera != null && CurrentState != EnemyState.Dead)
        {
            // SỬA LẠI: Đồng bộ vector hướng nhìn của UI trùng khớp tuyệt đối với hướng nhìn của Camera.
            // Cách này đảm bảo UI không bao giờ bị ngược chữ và bỏ qua mọi góc nghiêng do xương đầu của quái vật tạo ra.
            healthCanvas.transform.forward = mainCamera.transform.forward;
        }
    }

    private bool IsAgentValid()
    {
        return agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            NetworkPosition = transform.position;
            NetworkRotation = transform.rotation;
        }

        if (!HasStateAuthority) return;

        if (CurrentState == EnemyState.Dead)
        {
            if (despawnTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
            return;
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
                else
                {
                    CurrentState = EnemyState.Return;
                }
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
                if (player != null)
                {
                    // Lấy sát thương chuẩn được tính dựa trên Cấp độ (Level)
                    float damageToDeal = GetDamage(NetworkedLevel);
                    player.RPC_TakeDame(damageToDeal);
                }
            }
        }
    }

    private void StartPatrol()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
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
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
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
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamageFromPlayer(int damage)
    {
        if (CurrentState == EnemyState.Dead) return;

        Health -= damage;

        if (Health <= 0)
        {
            CurrentState = EnemyState.Dead;
            if (IsAgentValid()) agent.isStopped = true;
            DropItem();
            despawnTimer = TickTimer.CreateFromSeconds(Runner, 5f);
        }
        else
        {
            RPC_PlayTakeDamageAnim();
        }
    }

    // Callback cập nhật thanh máu UI
    public void OnHealthChanged()
    {
        if (healthSlider != null)
        {
            healthSlider.value = Health;
        }
    }

    // THÊM MỚI: Callback cập nhật Level UI và thay đổi giới hạn của thanh máu
    public void OnLevelChanged()
    {
        int safeLevel = Mathf.Max(1, NetworkedLevel);
        if (levelText != null) levelText.text = safeLevel.ToString();

        // Điều chỉnh lại MaxValue của Slider máu cho khớp với Level
        if (healthSlider != null) healthSlider.maxValue = GetMaxHealth(safeLevel);
    }

    private void DropItem()
    {
        if (dropItems != null && dropItems.Count > 0)
        {
            float randomValue = Random.Range(0f, 100f);
            if (randomValue <= dropChance)
            {
                int randomIndex = Random.Range(0, dropItems.Count);
                GameObject itemToDrop = dropItems[randomIndex];

                if (itemToDrop != null)
                {
                    NetworkObject netObj = itemToDrop.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        Vector3 spawnPosition = transform.position + Vector3.up * 1f;
                        Runner.Spawn(netObj, spawnPosition, Quaternion.identity);
                    }
                }
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
            // Nội suy (Lerp) vị trí mượt mà trên máy khách
            transform.position = Vector3.Lerp(transform.position, NetworkPosition, Runner.DeltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, NetworkRotation, Runner.DeltaTime * 15f);
        }
    }

    public void OnStateChanged()
    {
        if (animator == null) return;

        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);

        switch (CurrentState)
        {
            case EnemyState.Patrol:
            case EnemyState.Return:
                animator.SetBool("isWalking", true);
                break;
            case EnemyState.Chase:
                animator.SetBool("isRunning", true);
                break;
            case EnemyState.Scream:
                animator.SetTrigger("scream");
                break;
            case EnemyState.Dead:
                animator.SetTrigger("death");
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;

                if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);
                break;
        }
    }
}