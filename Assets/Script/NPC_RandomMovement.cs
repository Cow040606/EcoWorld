using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DialogueEditor;
using Fusion;

[RequireComponent(typeof(NavMeshAgent))]
public class NPC_RandomMovement : NetworkBehaviour
{
    [Header("Danh sách các điểm di chuyển")]
    public List<Transform> waypoints;

    [Header("Thời gian đứng yên (giây)")]
    public float waitTime = 60f; // 1 phút = 60 giây

    [Header("Animation")]
    public Animator animator;
    public string isWalkingParameter = "isWalking"; // Tên biến Bool trong Animator

    private NavMeshAgent agent;
    private float waitTimer;
    private bool isWaiting;
    private bool wasChatting;

    [Networked] public NetworkBool isMovingNetworked { get; set; }

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        if (!Object.HasStateAuthority)
        {
            // Tắt NavMeshAgent ở các máy Client để NetworkTransform toàn quyền điều khiển vị trí
            if (agent != null) agent.enabled = false;
        }
        else
        {
            if (waypoints.Count > 0)
            {
                MoveToRandomWaypoint();
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ Host (Máy chủ) mới tính toán AI di chuyển
        if (!Object.HasStateAuthority) return;

        bool isChatting = false;
        // Chú ý: Đoạn code cũ này chỉ kiểm tra xem MÁY HOST có đang mở khung Chat hay không.
        // Nếu một người chơi ở máy Client chat với NPC, Host sẽ không biết.
        // Để khắc phục triệt để, bạn cần dùng RPC để báo cho Host biết "có người đang chat với NPC này".
        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
        {
            isChatting = true;
        }

        if (isChatting)
        {
            if (!wasChatting)
            {
                wasChatting = true;
            }
            
            // Dừng di chuyển ngay lập tức khi đang chat
            agent.isStopped = true;
            isWaiting = true;
            waitTimer = waitTime;
        }
        else
        {
            if (wasChatting)
            {
                wasChatting = false;
            }
            
            if (isWaiting)
            {
                agent.isStopped = true;
                waitTimer -= Runner.DeltaTime;
                
                // Đã đợi xong
                if (waitTimer <= 0)
                {
                    isWaiting = false;
                    agent.isStopped = false;
                    MoveToRandomWaypoint();
                }
            }
            else
            {
                // Đang di chuyển bình thường
                agent.isStopped = false;
                
                // Kiểm tra xem đã tới vị trí chưa
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        // Đã đến vị trí, chuyển sang trạng thái đứng yên
                        isWaiting = true;
                        waitTimer = waitTime;
                    }
                }
            }
        }
        
        // Đồng bộ biến di chuyển lên mạng cho tất cả mọi người cùng thấy
        isMovingNetworked = !agent.isStopped && agent.velocity.sqrMagnitude > 0.01f;
    }

    public override void Render()
    {
        // Cập nhật Animation ở tất cả các máy dựa trên cờ trạng thái mạng
        if (animator != null)
        {
            animator.SetBool(isWalkingParameter, isMovingNetworked);
        }
    }

    public void MoveToRandomWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        int randomIndex = Random.Range(0, waypoints.Count);
        agent.SetDestination(waypoints[randomIndex].position);
    }
}
