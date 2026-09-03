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
    [Networked] public float CurrentMoveSpeed { get; set; }

    private Animator animator;

    private void Awake()
    {
        Physics.IgnoreLayerCollision(13, 13, true);
        Physics.IgnoreLayerCollision(13, 14, true);
        Physics.IgnoreLayerCollision(13, 0, true);

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
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (agent != null)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.angularSpeed = 720f;
            agent.acceleration = 45f;
            agent.baseOffset = 0.05f;
            agent.autoRepath = true;
        }

        if (Object.HasStateAuthority)
        {
            if (agent != null)
            {
                agent.enabled = true;
                if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }
        }
        else
        {
            if (agent != null)
            {
                agent.enabled = false;
            }
        }
    }

    public override void Render()
    {
        if (animator == null) return;
        float speed = Object.HasStateAuthority && agent != null && agent.enabled ? agent.velocity.magnitude : CurrentMoveSpeed;
        bool isMoving = speed > 0.2f;
        animator.SetBool("isWalking", isMoving && targetPlayer == null);
        animator.SetBool("isRunning", isMoving && targetPlayer != null);
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            CurrentMoveSpeed = (agent != null && agent.enabled) ? agent.velocity.magnitude : 0f;

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
            Vector3 targetPos = targetPlayer.transform.position;
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                targetPos = hit.position;
            }
            agent.SetDestination(targetPos);
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

            if (!agent.pathPending && agent.remainingDistance < 0.6f)
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
