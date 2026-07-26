using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DialogueEditor;

[RequireComponent(typeof(NavMeshAgent))]
public class NPC_RandomMovement : MonoBehaviour
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

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        if (waypoints.Count > 0)
        {
            MoveToRandomWaypoint();
        }
    }

    void Update()
    {
        // Kiểm tra xem có hệ thống chat đang mở không (sử dụng DialogueEditor)
        bool isChatting = false;
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
            waitTimer = waitTime; // Reset đếm ngược lại 60s để sau khi chat xong sẽ đợi thêm 1 phút
        }
        else
        {
            if (wasChatting)
            {
                // Vừa chat xong, giữ nguyên trạng thái chờ (isWaiting)
                wasChatting = false;
            }
            
            if (isWaiting)
            {
                agent.isStopped = true;
                waitTimer -= Time.deltaTime;
                
                // Đã đợi xong 1 phút
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
        
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (animator != null)
        {
            // NPC được xem là đang đi nếu không bị dừng và có vận tốc di chuyển
            bool isMoving = !agent.isStopped && agent.velocity.sqrMagnitude > 0.01f;
            animator.SetBool(isWalkingParameter, isMoving);
        }
    }

    public void MoveToRandomWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        // Chọn một điểm ngẫu nhiên trong danh sách
        int randomIndex = Random.Range(0, waypoints.Count);
        
        // Di chuyển tới điểm đó
        agent.SetDestination(waypoints[randomIndex].position);
    }
}
