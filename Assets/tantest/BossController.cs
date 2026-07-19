using Fusion;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossController : NetworkBehaviour
{
    public enum BossState { Patrol, Chase, Attack }

    [Header("--- Health Settings ---")]
    [SerializeField] private float maxHP = 2000f;
    [Networked] public float CurrentHP { get; set; }
    public float MaxHP => maxHP;

    [Header("--- AI Settings ---")]
    public float patrolRadius = 15f;
    public float aggroRadius = 20f;
    public float attackRange = 3f;
    public float attackDamage = 30f;
    public float attackCooldown = 2f;

    [Header("--- Movement ---")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 6f;

    [Networked] private BossState currentState { get; set; }
    [Networked] private TickTimer attackTimer { get; set; }

    private NavMeshAgent agent;
    private Vector3 startPos;
    private Player_Controller targetPlayer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void Spawned()
    {
        startPos = transform.position;
        if (Object.HasStateAuthority)
        {
            CurrentHP = maxHP;
            currentState = BossState.Patrol;

            if (agent != null && !agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }

            SetRandomPatrolDestination();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || CurrentHP <= 0) return;

        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        switch (currentState)
        {
            case BossState.Patrol:
                PatrolLogic();
                CheckForAggro();
                break;
            case BossState.Chase:
                ChaseLogic();
                break;
            case BossState.Attack:
                AttackLogic();
                break;
        }
    }

    #region AI STATES
    private void PatrolLogic()
    {
        agent.speed = patrolSpeed;
        // Nếu đã đến nơi, chọn điểm mới
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetRandomPatrolDestination();
        }
    }

    private void CheckForAggro()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRadius);
        foreach (var hit in hits)
        {
            Player_Controller player = hit.GetComponentInParent<Player_Controller>();
            if (player != null && player.CurrentHealth > 0)
            {
                targetPlayer = player;
                currentState = BossState.Chase;
                return;
            }
        }
    }

    private void ChaseLogic()
    {
        if (targetPlayer == null || targetPlayer.CurrentHealth <= 0)
        {
            currentState = BossState.Patrol;
            SetRandomPatrolDestination();
            return;
        }

        agent.speed = chaseSpeed;
        agent.SetDestination(targetPlayer.transform.position);

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);

        // Nếu Player chạy khỏi tầm aggro
        if (distanceToPlayer > aggroRadius * 1.5f)
        {
            targetPlayer = null;
            currentState = BossState.Patrol;
            SetRandomPatrolDestination();
        }
        // Nếu đủ gần để đánh
        else if (distanceToPlayer <= attackRange)
        {
            currentState = BossState.Attack;
            agent.isStopped = true;
        }
    }

    private void AttackLogic()
    {
        if (targetPlayer == null || targetPlayer.CurrentHealth <= 0 || Vector3.Distance(transform.position, targetPlayer.transform.position) > attackRange)
        {
            agent.isStopped = false;
            currentState = BossState.Chase;
            return;
        }

        // Nhìn thẳng vào player
        Vector3 dir = (targetPlayer.transform.position - transform.position).normalized;
        dir.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Runner.DeltaTime * 10f);

        if (attackTimer.ExpiredOrNotRunning(Runner))
        {
            // Tấn công Player bằng hàm Server đã tạo ở Bước 1
            targetPlayer.Server_TakeDamageFromBoss(attackDamage);

            // TODO: Bật Animation Attack ở đây (gọi RPC)

            attackTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
        }
    }
    #endregion

    private void SetRandomPatrolDestination()
    {
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
        randomDir += startPos;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // Nhận sát thương từ Player (gọi qua RPC để đẩy từ Client lên Server)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_PlayerHitBoss(float damage)
    {
        if (CurrentHP <= 0) return;

        CurrentHP -= damage;

        // Bị đánh thì quay sang đánh trả ngay lập tức (Truy đuổi)
        if (currentState == BossState.Patrol)
        {
            CheckForAggro();
        }

        if (CurrentHP <= 0)
        {
            if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
            }
            Runner.Despawn(Object); // TODO: Chạy Anim chết, rớt đồ trước khi Despawn
        }
    }
}