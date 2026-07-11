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

    // =========================================================================
    // HÀM GÂY SÁT THƯƠNG TỪ ANIMATION EVENT CỦA QUÁI VẬT
    // =========================================================================
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
    // =========================================================================

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
        }
        else
        {
            RPC_PlayTakeDamageAnim();
        }
    }

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
                Destroy (gameObject, 5f);
                break;
        }
    }
}