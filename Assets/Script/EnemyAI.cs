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

    public override void Spawned()
    {
        // Tắt NavMeshAgent ở các máy Client để NetworkTransform toàn quyền điều khiển vị trí
        if (!Object.HasStateAuthority && agent != null)
        {
            agent.enabled = false;
        }
    }

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