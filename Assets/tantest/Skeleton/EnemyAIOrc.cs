using System.Collections.Generic;
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

    public float attackCooldown = 1.0f;
    public int maxHealth = 100;

    [Header("Drop Settings")]
    [Tooltip("Danh sách các vật phẩm có thể rớt (Bắt buộc phải có component NetworkObject)")]
    public List<GameObject> dropItems;

    [Tooltip("Tỉ lệ rớt đồ (0 đến 100%)")]
    [Range(0f, 100f)]
    public float dropChance = 100f;

    // THÊM: Bộ đếm thời gian dọn dẹp xác chết chuẩn Fusion (Thay thế cho Destroy)
    [Networked] private TickTimer despawnTimer { get; set; }

    // THÊM: Đồng bộ vị trí thủ công cho chế độ Online
    [Networked] private Vector3 NetworkPosition { get; set; }
    [Networked] private Quaternion NetworkRotation { get; set; }

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 startPosition;
    private float stateTimer = 0f;
    private Transform targetPlayer;

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        startPosition = transform.position;

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
            Health = maxHealth;
            CurrentState = EnemyState.Idle;
        }
        else
        {
            // QUAN TRỌNG: Máy con (Proxy) phải TẮT NavMeshAgent để nhường quyền cho NetworkTransform
            if (agent != null) agent.enabled = false;
            
            // Đặt ngay vị trí ban đầu để tránh bị trượt từ tọa độ 0
            transform.position = NetworkPosition;
            transform.rotation = NetworkRotation;
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

        // Xử lý biến mất sau khi chết chuẩn mạng
        if (CurrentState == EnemyState.Dead)
        {
            if (despawnTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
            return; // Khóa toàn bộ AI khi đã chết
        }

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
                        if (IsAgentValid()) agent.isStopped = true; // Dừng lại để chém

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
                            if (IsAgentValid()) agent.isStopped = false; // Cho phép chạy tiếp
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
                // Kiểm tra thêm Player đó có đang chết không (tránh việc đuổi theo xác chết)
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

            // Hẹn giờ Despawn mạng sau 5 giây (thay cho Destroy)
            despawnTimer = TickTimer.CreateFromSeconds(Runner, 5f);
        }
        else
        {
            RPC_PlayTakeDamageAnim();
        }
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
                    else
                    {
                        Debug.LogWarning($"[EnemyAIOrc] GameObject '{itemToDrop.name}' không có component NetworkObject!");
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
                // ĐÃ XÓA LỆNH DESTROY Ở ĐÂY ĐỂ TRÁNH LỖI MẠNG
                break;
        }
    }
}