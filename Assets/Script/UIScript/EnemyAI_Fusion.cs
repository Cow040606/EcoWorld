using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI_Fusion : NetworkBehaviour
{
    [Header("Cài đặt Tuần Tra")]
    public NavMeshAgent agent;
    public Transform[] diemTuanTra;
    private int diemHienTai = 0;

    [Header("Cài đặt Đuổi Bắt & Tấn Công")]
    public float banKinhPhatHien = 14f;
    public float banKinhTanCong = 2f;
    public float satThuong = 10f;
    public float thoiGianHoiDon = 1.5f;
    public LayerMask layerPlayer;
    private Transform mucTieuCuaQuai;

    [Networked] private TickTimer attackTimer { get; set; }
    [Networked] public float CurrentMoveSpeed { get; set; }

    // Biến nội bộ
    private Vector3 viTriBanDau;
    private bool dangDuoiTheo = false;
    private Collider[] nguoiChoiResults = new Collider[16];
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
        viTriBanDau = transform.position;
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.angularSpeed = 720f;
            agent.acceleration = 45f;
            agent.baseOffset = 0.05f;
            agent.autoRepath = true;
        }

        if (HasStateAuthority)
        {
            if (agent != null)
            {
                agent.enabled = true;
                if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }

            if (diemTuanTra != null && diemTuanTra.Length > 0 && agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(diemTuanTra[diemHienTai].position);
            }
        }
        else
        {
            if (agent != null) agent.enabled = false;
        }
    }

    public override void Render()
    {
        if (animator == null) return;
        float speed = HasStateAuthority && agent != null && agent.enabled ? agent.velocity.magnitude : CurrentMoveSpeed;
        bool isMoving = speed > 0.2f;
        animator.SetBool("isWalking", isMoving && !dangDuoiTheo);
        animator.SetBool("isRunning", isMoving && dangDuoiTheo);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        CurrentMoveSpeed = agent.velocity.magnitude;

        if (Runner.Tick % 5 != 0) return;

        LayerMask effectiveLayer = layerPlayer == 0 ? ~0 : layerPlayer;
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, banKinhPhatHien, nguoiChoiResults, effectiveLayer);

        Player_Controller playerTarget = null;
        for (int i = 0; i < numHits; i++)
        {
            if (nguoiChoiResults[i] != null)
            {
                Player_Controller p = nguoiChoiResults[i].GetComponentInParent<Player_Controller>();
                if (p != null && !p.isDead)
                {
                    playerTarget = p;
                    break;
                }
            }
        }
        System.Array.Clear(nguoiChoiResults, 0, numHits);

        if (playerTarget == null)
        {
            Player_Controller[] players = FindObjectsOfType<Player_Controller>();
            foreach (var p in players)
            {
                if (p != null && !p.isDead && Vector3.Distance(transform.position, p.transform.position) <= banKinhPhatHien)
                {
                    playerTarget = p;
                    break;
                }
            }
        }

        if (playerTarget != null)
        {
            dangDuoiTheo = true;
            mucTieuCuaQuai = playerTarget.transform;
            float dist = Vector3.Distance(transform.position, mucTieuCuaQuai.position);

            if (dist <= banKinhTanCong)
            {
                agent.isStopped = true;
                Vector3 look = new Vector3(mucTieuCuaQuai.position.x, transform.position.y, mucTieuCuaQuai.position.z);
                if (look != transform.position) transform.LookAt(look);

                if (attackTimer.ExpiredOrNotRunning(Runner))
                {
                    attackTimer = TickTimer.CreateFromSeconds(Runner, thoiGianHoiDon);
                    playerTarget.RPC_TakeDame(satThuong);
                    RPC_PlayAttack();
                }
            }
            else
            {
                agent.isStopped = false;
                Vector3 targetPos = mucTieuCuaQuai.position;
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    targetPos = hit.position;
                }
                agent.SetDestination(targetPos);
            }
        }
        else
        {
            agent.isStopped = false;
            if (dangDuoiTheo)
            {
                dangDuoiTheo = false;
                agent.SetDestination(viTriBanDau);
            }
            else
            {
                if (diemTuanTra != null && diemTuanTra.Length > 0 && agent.remainingDistance < 0.6f && !agent.pathPending)
                {
                    diemHienTai = (diemHienTai + 1) % diemTuanTra.Length;
                    agent.SetDestination(diemTuanTra[diemHienTai].position);
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAttack()
    {
        if (animator != null) animator.SetTrigger("slash");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, banKinhPhatHien);
    }
}
