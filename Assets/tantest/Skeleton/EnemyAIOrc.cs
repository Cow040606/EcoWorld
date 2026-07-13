using System.Collections.Generic; // Bắt buộc phải có để dùng List
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIOrc : NetworkBehaviour
{
    public enum EnemyState { Idle, Patrol, Scream, Chase, Attack, Return, Dead }

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public EnemyState CurrentState { get; set; }

    [Networked]
    public int Health { get; set; }

    [Header("AI Settings")]
    public float patrolRadius = 10f;
    public float detectionRadius = 8f;
    public float loseRadius = 15f;
    public float attackRadius = 2f;
    public float idleWaitTime = 5f;
    public float screamDuration = 2f;
    public float attackCooldown = 1.5f;
    public int maxHealth = 100;

    [Header("Drop Settings")]
    [Tooltip("Danh sách các vật phẩm có thể rớt (Bắt buộc phải có component NetworkObject)")]
    public List<GameObject> dropItems; // Đã đổi sang List

    [Tooltip("Tỉ lệ rớt đồ (0 đến 100%)")]
    [Range(0f, 100f)]
    public float dropChance = 100f;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 startPosition;
    private float stateTimer = 0f;
    private Transform targetPlayer;

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // =========================================================
        // SỬA LỖI VĂNG RA RÌA MAP KHI SPAWN
        // Ép NavMeshAgent cập nhật đúng vị trí do hệ thống mạng chỉ định
        if (agent != null)
        {
            agent.Warp(transform.position);
            agent.enabled = true; // Bật lại Agent sau khi đã bế nó tới đúng chỗ
        }
        // =========================================================

        startPosition = transform.position;

        if (HasStateAuthority)
        {
            Health = maxHealth;
            CurrentState = EnemyState.Idle;
        }
    }

    private bool IsAgentValid()
    {
        return agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || CurrentState == EnemyState.Dead) return;

        switch (CurrentState)
        {
            case EnemyState.Idle:
                stateTimer += Runner.DeltaTime;
                if (stateTimer >= idleWaitTime)
                {
                    StartPatrol();
                }
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
                if (stateTimer >= screamDuration)
                {
                    CurrentState = EnemyState.Chase;
                }
                break;

            case EnemyState.Chase:
                if (targetPlayer != null)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

                    if (distanceToPlayer <= attackRadius)
                    {
                        CurrentState = EnemyState.Attack;
                        stateTimer = 0f;
                        if (IsAgentValid()) agent.ResetPath();
                    }
                    else if (distanceToPlayer > loseRadius)
                    {
                        targetPlayer = null;
                        CurrentState = EnemyState.Return;
                        if (IsAgentValid()) agent.SetDestination(startPosition);
                    }
                    else
                    {
                        if (IsAgentValid()) agent.SetDestination(targetPlayer.position);
                    }
                }
                else
                {
                    CurrentState = EnemyState.Return;
                    if (IsAgentValid()) agent.SetDestination(startPosition);
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
                        }
                        else
                        {
                            CurrentState = EnemyState.Idle;
                            CurrentState = EnemyState.Attack;
                            stateTimer = 0f;
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
                    player.RPC_TakeDame(15f);
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
                targetPlayer = hit.transform;
                CurrentState = EnemyState.Scream;
                stateTimer = 0f;
                if (IsAgentValid()) agent.ResetPath();
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
            if (IsAgentValid()) agent.ResetPath();

            DropItem();
        }
        else
        {
            RPC_PlayTakeDamageAnim();
        }
    }

    // =========================================================================
    // HÀM XỬ LÝ RỚT ĐỒ TỪ LIST
    // =========================================================================
    private void DropItem()
    {
        // Kiểm tra List có tồn tại và có phần tử nào không
        if (dropItems != null && dropItems.Count > 0)
        {
            float randomValue = Random.Range(0f, 100f);

            // Nếu trúng tỉ lệ rớt
            if (randomValue <= dropChance)
            {
                // Chọn ngẫu nhiên 1 index trong List
                int randomIndex = Random.Range(0, dropItems.Count);
                GameObject itemToDrop = dropItems[randomIndex];

                // Kiểm tra xem item được chọn có null không
                if (itemToDrop != null)
                {
                    // Lấy component NetworkObject để Spawn qua Fusion
                    NetworkObject netObj = itemToDrop.GetComponent<NetworkObject>();

                    if (netObj != null)
                    {
                        Vector3 spawnPosition = transform.position + Vector3.up * 1f;
                        Runner.Spawn(netObj, spawnPosition, Quaternion.identity);
                    }
                    else
                    {
                        Debug.LogWarning($"[EnemyAIOrc] GameObject '{itemToDrop.name}' trong danh sách dropItems không có component NetworkObject! Không thể spawn qua mạng.");
                    }
                }
            }
        }
    }
    // =========================================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayTakeDamageAnim()
    {
        animator.SetTrigger("takedame");
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
            case EnemyState.Attack:
                animator.SetTrigger("slash");
                break;
            case EnemyState.Dead:
                animator.SetTrigger("death");
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                Destroy(gameObject, 5f);
                break;
        }
    }
}