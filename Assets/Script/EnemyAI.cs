using UnityEngine;
using UnityEngine.AI;
using Fusion;

public class EnemyAI : NetworkBehaviour
{
    public NavMeshAgent agent;
    public float chaseRange = 10f;
    public Transform[] patrolPoints;
    
    [Networked] private int currentPatrolIndex { get; set; }
    [Networked] private NetworkObject targetPlayer { get; set; }

    public override void FixedUpdateNetwork()
    {
        // Trong Shared Mode, chỉ người có quyền điều khiển (thường là Master/Host) mới tính toán AI
        if (Object.HasStateAuthority)
        {
            FindNearestPlayer();

            if (targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) <= chaseRange)
            {
                ChasePlayer();
            }
            else
            {
                Patrol();
            }
        }
    }

    void FindNearestPlayer()
    {
        NetworkObject closest = null;
        float minDistance = float.MaxValue;

        // Tìm tất cả Player trong phòng
        foreach (var player in Runner.ActivePlayers)
        {
            if (Runner.TryGetPlayerObject(player, out var pObj))
            {
                float dist = Vector3.Distance(transform.position, pObj.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = pObj;
                }
            }
        }
        targetPlayer = closest;
    }

    void ChasePlayer()
    {
        if (targetPlayer != null)
        {
            agent.SetDestination(targetPlayer.transform.position);
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }
}