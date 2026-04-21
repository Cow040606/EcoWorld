using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class VillagerAI : NetworkBehaviour
{
    private NavMeshAgent agent;
    private bool isInitialMoving = false;
    private Vector3 initialTarget;

    [Header("AI Settings")]
    public float wanderRadius = 10f;
    public float wanderTimer = 5f;
    private float timer;

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();

        // BƯỚC QUAN TRỌNG: Sửa lỗi chìm dưới đất
        // 1. Tạm thời tắt agent để không bị "giật" vị trí về 0,0,0
        agent.enabled = false;

        // 2. Tìm điểm gần nhất trên NavMesh (bề mặt sàn màu xanh)
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            // Đưa dân làng lên mặt đất
            transform.position = hit.position;
            agent.enabled = true; // Bật lại agent
            agent.Warp(hit.position); // Khóa agent vào lưới NavMesh
            Debug.Log("<color=green>AI:</color> Đã đặt dân làng lên mặt đất thành công.");
        }
        else
        {
            Debug.LogError("<color=red>AI:</color> Không tìm thấy NavMesh bên dưới điểm Spawn!");
            agent.enabled = true;
        }

        timer = wanderTimer;
    }

    // Hàm gọi từ Spawner để đi ra vị trí chỉ định
    public void MoveToInitialPosition(Vector3 exitPos)
    {
        isInitialMoving = true;
        initialTarget = exitPos;
        if (agent != null && agent.enabled)
        {
            agent.SetDestination(exitPos);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ máy có quyền kiểm soát (State Authority) mới chạy logic AI
        if (!HasStateAuthority || agent == null || !agent.enabled) return;

        if (isInitialMoving)
        {
            // Kiểm tra nếu đã đi đến điểm thoát ban đầu
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                isInitialMoving = false;
                Debug.Log("AI: Đã ra tới nơi, bắt đầu đi lang thang.");
            }
            return;
        }

        // Logic đi lang thang ngẫu nhiên
        timer += Runner.DeltaTime;
        if (timer >= wanderTimer)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            timer = 0;
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }
}