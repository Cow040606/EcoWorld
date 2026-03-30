using Fusion;
using UnityEngine;
using UnityEngine.AI; // Bắt buộc phải có thư viện này để xài NavMesh

public class EnemyAI_Fusion : NetworkBehaviour
{
    [Header("Cài đặt Tuần Tra")]
    public NavMeshAgent agent;
    public Transform[] diemTuanTra; // Bò kéo 3 cái Transform vị trí vào đây
    private int diemHienTai = 0;

    [Header("Cài đặt Đuổi Bắt")]
    public float banKinhPhatHien = 10f;
    public LayerMask layerPlayer; // Chọn Layer của nhân vật Bò
    private Transform mucTieuCuaQuai;
    
    // Biến nội bộ
    private Vector3 viTriBanDau;
    private bool dangDuoiTheo = false;

    public override void Spawned()
    {
        // Lưu lại quê hương để lát chạy về
        viTriBanDau = transform.position;

        if (HasStateAuthority)
        {
            // Nếu là máy Host -> Cho phép quái tư duy và tìm đường
            agent = GetComponent<NavMeshAgent>();
            if (diemTuanTra.Length > 0)
            {
                agent.SetDestination(diemTuanTra[diemHienTai].position);
            }
        }
        else
        {
            // BÍ QUYẾT: Máy Client KHÔNG được phép có NavMeshAgent, 
            // Nếu không nó sẽ đánh nhau với NetworkTransform gây giật lag quái!
            NavMeshAgent clientAgent = GetComponent<NavMeshAgent>();
            if (clientAgent != null) clientAgent.enabled = false;
        }
    }

    // FixedUpdateNetwork là nhịp tim của game mạng
    public override void FixedUpdateNetwork()
    {
        // CHỈ CÓ HOST MỚI XỬ LÝ LOGIC NÀY
        if (!HasStateAuthority) return;

        // 1. Bật Radar hình cầu quét xem có Player nào lọt vào vùng không
        Collider[] nguoiChoiGoc = Physics.OverlapSphere(transform.position, banKinhPhatHien, layerPlayer);

        if (nguoiChoiGoc.Length > 0)
        {
            // TRẠNG THÁI: ĐUỔI THEO PLAYER
            dangDuoiTheo = true;
            mucTieuCuaQuai = nguoiChoiGoc[0].transform;
            agent.SetDestination(mucTieuCuaQuai.position); // Chỉ điểm cho NavMesh đuổi
        }
        else
        {
            // TRẠNG THÁI: TUẦN TRA HOẶC QUAY VỀ
            if (dangDuoiTheo)
            {
                // Vừa mất dấu Player -> Chạy về chỗ cũ
                dangDuoiTheo = false;
                agent.SetDestination(viTriBanDau); 
            }
            else
            {
                // Đang rảnh rỗi -> Đi tuần tra quanh 3 điểm
                if (diemTuanTra.Length > 0 && agent.remainingDistance < 0.5f && !agent.pathPending)
                {
                    diemHienTai++;
                    if (diemHienTai >= diemTuanTra.Length) diemHienTai = 0; // Đi hết thì quay lại điểm số 0
                    
                    agent.SetDestination(diemTuanTra[diemHienTai].position);
                }
            }
        }
    }

    // Hàm này giúp Bò nhìn thấy cái vòng tròn phát hiện trong màn hình Scene
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, banKinhPhatHien);
    }
}