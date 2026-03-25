    using UnityEngine;

    public class EnemyAI : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 3f;

        [Header("Combat Settings")]
        public float detectionRange = 5f;
        public float chaseRange = 8f;
        public Transform player;

        [Header("Patrol Settings")]
        public float patrolRadius = 5f; // Bán kính khu vực tuần tra
        public float waitTime = 2f;     // Thời gian đứng nghỉ trước khi đi điểm khác

        private Vector3 spawnPoint;
        private bool isChasing = false;

        // Các biến hỗ trợ tuần tra
        private Vector3 currentPatrolPoint;
        private bool isWaiting = false;
        private float waitCounter = 0f;

        void Start()
        {
            spawnPoint = transform.position;
            GetNewPatrolPoint(); // Lấy điểm tuần tra đầu tiên ngay khi bắt đầu
        }

        void Update()
        {
            // Tính vị trí trên mặt phẳng 2D (bỏ qua trục Y)
            Vector3 enemyPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 playerPosXZ = new Vector3(player.position.x, 0, player.position.z);

            float distanceToPlayer = Vector3.Distance(enemyPosXZ, playerPosXZ);

            if (isChasing)
            {
                ChasePlayer(distanceToPlayer);
            }
            else
            {
                CheckForPlayer(distanceToPlayer);

                // Nếu sau khi kiểm tra mà vẫn không thấy Player, thì tiếp tục tuần tra
                if (!isChasing)
                {
                    Patrol(enemyPosXZ);
                }
            }
        }

        void CheckForPlayer(float distance)
        {
            if (distance < detectionRange)
            {
                isChasing = true;
                isWaiting = false; // Hủy trạng thái đứng nghỉ nếu phát hiện người chơi
            }
        }

        void ChasePlayer(float distance)
        {
            if (distance > chaseRange)
            {
                // Mất dấu Player, quay lại trạng thái tuần tra
                isChasing = false;
                GetNewPatrolPoint();
                return;
            }

            MoveTowards(player.position);
        }

        void Patrol(Vector3 enemyPosXZ)
        {
            Vector3 patrolPosXZ = new Vector3(currentPatrolPoint.x, 0, currentPatrolPoint.z);
            float distanceToPatrolPoint = Vector3.Distance(enemyPosXZ, patrolPosXZ);

            // Nếu đã đến điểm tuần tra
            if (distanceToPatrolPoint < 0.2f)
            {
                isWaiting = true;
            }

            if (isWaiting)
            {
                // Đứng nghỉ ngơi
                waitCounter += Time.deltaTime;
                if (waitCounter >= waitTime)
                {
                    // Hết thời gian nghỉ, tìm điểm mới và đi tiếp
                    isWaiting = false;
                    waitCounter = 0f;
                    GetNewPatrolPoint();
                }
            }
            else
            {
                // Di chuyển đến điểm tuần tra
                MoveTowards(currentPatrolPoint);
            }
        }

        void GetNewPatrolPoint()
        {
            // Random một điểm trong vòng tròn 2D xung quanh vị trí spawn ban đầu
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            currentPatrolPoint = new Vector3(spawnPoint.x + randomCircle.x, transform.position.y, spawnPoint.z + randomCircle.y);
        }

        void MoveTowards(Vector3 target)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                direction = direction.normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                transform.forward = direction;
            }
        }

        void OnDrawGizmosSelected()
        {
            // Vẽ tầm nhìn và tầm bám đuôi
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, chaseRange);

            // Vẽ khu vực tuần tra (màu xanh lá)
            Gizmos.color = Color.green;
            // Nếu game đang chạy, vẽ từ spawnPoint, nếu chưa chạy thì vẽ từ vị trí hiện tại
            Vector3 center = Application.isPlaying ? spawnPoint : transform.position;
            Gizmos.DrawWireSphere(center, patrolRadius);
        }
    }