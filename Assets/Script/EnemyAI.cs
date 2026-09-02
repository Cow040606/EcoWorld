using UnityEngine;
using UnityEngine.AI;
using Fusion;

public class EnemyAI : NetworkBehaviour
{
    [Header("Movement & Range")]
    public NavMeshAgent agent;
    public float chaseRange = 14f;
    public Transform[] patrolPoints;

    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    [Networked] private int currentPatrolIndex { get; set; }
    [Networked] private NetworkObject targetPlayer { get; set; }
    [Networked] private TickTimer attackTimer { get; set; }

    private Animator animator;

    public override void Spawned()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (Object.HasStateAuthority)
        {
            if (agent != null)
            {
                agent.enabled = true;
                if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }
        }
        else
        {
            // Tắt NavMeshAgent ở các máy Client để NetworkTransform toàn quyền điều khiển vị trí
            if (agent != null)
            {
                agent.enabled = false;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Trong Shared Mode, chỉ người có quyền điều khiển (StateAuthority) mới tính toán AI
        if (Object.HasStateAuthority)
        {
            FindNearestPlayer();

            if (targetPlayer != null)
            {
                float dist = Vector3.Distance(transform.position, targetPlayer.transform.position);
                if (dist <= attackRange)
                {
                    AttackPlayer();
                }
                else if (dist <= chaseRange)
                {
                    ChasePlayer();
                }
                else
                {
                    Patrol();
                }
            }
            else
            {
                Patrol();
            }
        }
    }

    void FindNearestPlayer()
    {
        Player_Controller closest = null;
        float minDistance = float.MaxValue;

        // Tìm tất cả Player_Controller hoạt động trong phòng
        Player_Controller[] players = FindObjectsOfType<Player_Controller>();
        foreach (var player in players)
        {
            if (player != null && !player.isDead)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = player;
                }
            }
        }
        targetPlayer = closest != null ? closest.Object : null;
    }

    void ChasePlayer()
    {
        if (targetPlayer != null && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(targetPlayer.transform.position);
        }
    }

    void AttackPlayer()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        if (targetPlayer != null)
        {
            Vector3 look = new Vector3(targetPlayer.transform.position.x, transform.position.y, targetPlayer.transform.position.z);
            if (look != transform.position) transform.LookAt(look);

            if (attackTimer.ExpiredOrNotRunning(Runner))
            {
                attackTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
                Player_Controller player = targetPlayer.GetComponent<Player_Controller>();
                if (player != null && !player.isDead)
                {
                    player.RPC_TakeDame(attackDamage);
                }
                RPC_PlayAttackAnim();
            }
        }
    }

    void Patrol()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAttackAnim()
    {
        if (animator != null)
        {
            animator.SetTrigger("slash");
        }
    }
}
