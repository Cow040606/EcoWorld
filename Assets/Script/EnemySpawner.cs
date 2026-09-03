using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

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
    [Tooltip("Chỉ sinh quái vào ban đêm (19h - 6h sáng). Bỏ chọn để luôn luôn sinh quái bất kể ngày đêm.")]
    public bool spawnOnlyAtNight = false;

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
            spawnTimer = TickTimer.CreateFromSeconds(Runner, 1f);
        }
    }

    private float _uiCheckTimer = 0f;
    private void Update()
    {
        if (TimeController.Instance != null)
        {
            float totalSeconds = TimeController.Instance.CurrentTimeInSeconds;
            currentHour = Mathf.FloorToInt(totalSeconds / 3600f) % 24;
        }
        else if (uiTimeText != null && !string.IsNullOrEmpty(uiTimeText.text))
        {
            _uiCheckTimer -= Time.deltaTime;
            if (_uiCheckTimer <= 0f)
            {
                ParseTimeFromUI(uiTimeText.text);
                _uiCheckTimer = 1f;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (TimeController.Instance != null)
        {
            float totalSeconds = TimeController.Instance.CurrentTimeInSeconds;
            currentHour = Mathf.FloorToInt(totalSeconds / 3600f) % 24;
        }

        activeEnemies.RemoveAll(item => item == null);

        bool isNightTime = (currentHour >= startSpawnHour) || (currentHour < endSpawnHour);
        bool canSpawn = !spawnOnlyAtNight || isNightTime;

        if (canSpawn)
        {
            if (activeEnemies.Count < maxEnemies && spawnTimer.ExpiredOrNotRunning(Runner))
            {
                SpawnEnemy();
                spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
            }
        }
    }

    private void SpawnEnemy()
    {
        if (!enemyPrefab.IsValid)
        {
            Debug.LogWarning("[EnemySpawner] enemyPrefab chưa được gán hoặc không hợp lệ!");
            return;
        }

        Vector3 spawnPos = Vector3.zero;
        bool foundValidPos = false;

        // Thử 10 lần tìm vị trí ngẫu nhiên không bị đè lên quái đã sinh trước đó
        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            // Tránh tập trung quá sát tâm
            if (circle.magnitude < 2.5f) circle = circle.normalized * 3f;

            Vector3 searchPos = transform.position + new Vector3(circle.x, 0f, circle.y);

            // 1. Thử lấy vị trí trên NavMesh
            if (NavMesh.SamplePosition(searchPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                bool tooClose = false;
                foreach (var active in activeEnemies)
                {
                    if (active != null && Vector3.Distance(active.transform.position, hit.position) < 2f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    spawnPos = hit.position;
                    foundValidPos = true;
                    break;
                }
                else if (spawnPos == Vector3.zero)
                {
                    spawnPos = hit.position;
                }
            }
            // 2. Dự phòng: Raycast dò mặt đất nếu spawner bị lệch trục Y
            else if (Physics.Raycast(searchPos + Vector3.up * 30f, Vector3.down, out RaycastHit groundHit, 60f))
            {
                if (NavMesh.SamplePosition(groundHit.point, out hit, 10f, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                    foundValidPos = true;
                    break;
                }
            }
        }

        if (foundValidPos || spawnPos != Vector3.zero)
        {
            NetworkObject spawnedEnemy = Runner.Spawn(enemyPrefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 360), 0));
            if (spawnedEnemy != null)
            {
                activeEnemies.Add(spawnedEnemy);

                EnemyAIOrc orcAI = spawnedEnemy.GetComponent<EnemyAIOrc>();
                if (orcAI != null)
                {
                    orcAI.SetSpawnPosition(spawnPos);
                }

                BossController bossCtrl = spawnedEnemy.GetComponent<BossController>();
                if (bossCtrl != null)
                {
                    bossCtrl.SetViTriGoc(spawnPos);
                }
                Debug.Log($"[EnemySpawner] Đã spawn quái tại {spawnPos} ({activeEnemies.Count}/{maxEnemies})");
            }
        }
        else
        {
            Debug.LogWarning($"[EnemySpawner] Không tìm thấy điểm NavMesh thích hợp quanh {transform.position} để spawn quái!");
        }
    }

    private void ParseTimeFromUI(string timeString)
    {
        try
        {
            string[] timeParts = timeString.Split(':');
            if (timeParts.Length > 0)
            {
                int.TryParse(timeParts[0].Trim(), out currentHour);
            }
        }
        catch
        {
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
