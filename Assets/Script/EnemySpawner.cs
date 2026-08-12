using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;
using TMPro; // Dùng để đọc UI Time

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawn Settings (Cài đặt sinh quái)")]
    [Tooltip("Kéo Prefab quái vật vào đây (Prefab phải có NetworkObject)")]
    public NetworkPrefabRef enemyPrefab;

    [Tooltip("Số lượng quái vật tối đa tồn tại trong vùng này")]
    public int maxEnemies = 5;

    [Tooltip("Bán kính vùng spawn")]
    public float spawnRadius = 15f;

    [Tooltip("Thời gian chờ giữa mỗi lần spawn (giây)")]
    public float spawnInterval = 3f;

    [Header("Time Settings (Cài đặt thời gian)")]
    [Tooltip("Kéo object Time chứa component TextMeshPro vào đây")]
    public TextMeshProUGUI uiTimeText;

    [Tooltip("Giờ bắt đầu cho phép sinh quái (VD: 19 = 19h00)")]
    public int startSpawnHour = 19;

    [Tooltip("Giờ kết thúc sinh quái (VD: 6 = 6h00 sáng hôm sau)")]
    public int endSpawnHour = 6;

    // Quản lý thời gian chờ bằng TickTimer của Fusion
    [Networked] private TickTimer spawnTimer { get; set; }

    // Danh sách lưu trữ quái vật đang sống
    private List<NetworkObject> activeEnemies = new List<NetworkObject>();

    private int currentHour = 0;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
        }
    }

    // Dùng Update thường để lấy giờ từ UI liên tục mà không ảnh hưởng tới Network Tick
    private void Update()
    {
        if (uiTimeText != null && !string.IsNullOrEmpty(uiTimeText.text))
        {
            ParseTimeFromUI(uiTimeText.text);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ Server/Host mới có quyền tính toán và Spawn quái
        if (!HasStateAuthority) return;

        // Xóa các quái vật đã bị tiêu diệt (bị Despawn = null) khỏi danh sách
        activeEnemies.RemoveAll(item => item == null);

        // Kiểm tra điều kiện thời gian: Từ 19h tối đến 6h sáng
        bool isNightTime = (currentHour >= startSpawnHour) || (currentHour < endSpawnHour);

        if (isNightTime)
        {
            // Nếu chưa đủ số lượng quái và đã hết thời gian chờ
            if (activeEnemies.Count < maxEnemies && spawnTimer.Expired(Runner))
            {
                SpawnEnemy();
                // Đặt lại đồng hồ đếm ngược cho con quái tiếp theo
                spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
            }
        }
        else
        {
            // Tùy chọn: Nếu bạn muốn sáng ra quái tự chết sạch, có thể xử lý ở đây
            // Nhưng hiện tại mình đang để chúng giữ nguyên, chỉ KHÔNG spawn thêm.
        }
    }

    private void SpawnEnemy()
    {
        // Tìm một điểm ngẫu nhiên trong bán kính
        Vector3 randomPos = transform.position + Random.insideUnitSphere * spawnRadius;
        randomPos.y = transform.position.y; // Giữ nguyên độ cao cơ bản

        // Sử dụng NavMesh để đảm bảo quái vật được rớt trúng mặt đất, không lọt ra ngoài map
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPos, out hit, spawnRadius, NavMesh.AllAreas))
        {
            // Spawn quái qua hệ thống mạng
            NetworkObject spawnedEnemy = Runner.Spawn(enemyPrefab, hit.position, Quaternion.identity);

            if (spawnedEnemy != null)
            {
                activeEnemies.Add(spawnedEnemy);
            }
        }
    }

    // Hàm phân tích chuỗi thời gian từ TextMeshPro (VD: "19:30" hoặc "19:00 PM")
    private void ParseTimeFromUI(string timeString)
    {
        try
        {
            // Tách chuỗi theo dấu ":" để lấy phần số giờ ở đầu tiên
            string[] timeParts = timeString.Split(':');
            if (timeParts.Length > 0)
            {
                // Loại bỏ khoảng trắng và chuyển thành số nguyên
                int.TryParse(timeParts[0].Trim(), out currentHour);
            }
        }
        catch
        {
            Debug.LogWarning("[EnemySpawner] Lỗi đọc định dạng thời gian từ UI.");
        }
    }

    // Vẽ vòng tròn bán kính Spawn trong cửa sổ Scene (để dễ thiết kế map)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}