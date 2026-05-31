using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIOrc : NetworkBehaviour
{
    // Đã thêm trạng thái Attack cho animation "slash"
    public enum EnemyState { Idle, Patrol, Scream, Chase, Attack, Return, Dead }

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public EnemyState CurrentState { get; set; }

    [Networked]
    public int Health { get; set; }

    [Header("AI Settings")]
    public float patrolRadius = 10f;       
    public float detectionRadius = 8f;     
    public float loseRadius = 15f;         
    public float attackRadius = 2f;        // Khoảng cách để quái chém (slash)
    public float idleWaitTime = 5f;        
    public float screamDuration = 2f;      
    public float attackCooldown = 1.5f;    // Thời gian nghỉ giữa 2 lần chém
    public int maxHealth = 100;

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
            Health = maxHealth;
            CurrentState = EnemyState.Idle;
        }
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
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
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
                    
                    // Nếu lại gần đủ tầm chém -> Chuyển sang Attack
                    if (distanceToPlayer <= attackRadius)
                    {
                        CurrentState = EnemyState.Attack;
                        stateTimer = 0f; // Reset timer để tính cooldown chém
                        agent.ResetPath(); // Dừng chạy để đứng chém
                    }
                    // Nếu đi quá xa -> Bỏ cuộc quay về
                    else if (distanceToPlayer > loseRadius)
                    {
                        targetPlayer = null;
                        CurrentState = EnemyState.Return;
                        agent.SetDestination(startPosition);
                    }
                    else
                    {
                        agent.SetDestination(targetPlayer.position);
                    }
                }
                else
                {
                    CurrentState = EnemyState.Return;
                    agent.SetDestination(startPosition);
                }
                break;

            case EnemyState.Attack:
                if (targetPlayer != null)
                {
                    // Xoay mặt về phía người chơi khi chém
                    transform.LookAt(new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z));
                    
                    stateTimer += Runner.DeltaTime;
                    if (stateTimer >= attackCooldown)
                    {
                        // Chém xong, kiểm tra xem người chơi còn ở gần không, nếu chạy xa thì đổi lại thành Chase
                        float dist = Vector3.Distance(transform.position, targetPlayer.position);
                        if (dist > attackRadius)
                        {
                            CurrentState = EnemyState.Chase;
                        }
                        else
                        {
                            // Nếu vẫn đứng gần, chém tiếp (Trigger lại animation thông qua RPC hoặc OnStateChanged)
                            CurrentState = EnemyState.Idle; // Mẹo nhỏ nhảy về Idle 1 frame để kích hoạt lại Attack
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
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    CurrentState = EnemyState.Idle;
                    stateTimer = 0f;
                }
                DetectPlayer();
                break;
        }
    }

    private void StartPatrol()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
        {
            agent.SetDestination(hit.position);
            CurrentState = EnemyState.Patrol;
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
                agent.ResetPath(); 
                transform.LookAt(new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z));
                break;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (!HasStateAuthority || CurrentState == EnemyState.Dead) return;

        Health -= damage;
        if (Health <= 0)
        {
            CurrentState = EnemyState.Dead;
            agent.ResetPath();
        }
        else
        {
            RPC_PlayTakeDamageAnim();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayTakeDamageAnim()
    {
        // Gọi chính xác tên trigger "takedame"
        animator.SetTrigger("takedame");
    }

    public void OnStateChanged()
    {
        if (animator == null) return;

        // Reset các cờ di chuyển
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
                if(col != null) col.enabled = false; 
                break;
        }
    }
}