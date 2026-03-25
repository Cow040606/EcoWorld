// using UnityEngine;
// using Unity.Netcode; // Đã cài package xong nên dòng này sẽ hết lỗi

// public class EnemyAINetwork : NetworkBehaviour
// {
//     [Header("Movement Settings")]
//     public float moveSpeed = 3f;

//     [Header("Combat Settings")]
//     public float detectionRange = 5f;
//     public float chaseRange = 8f;

//     [Header("Patrol Settings")]
//     public float patrolRadius = 5f;
//     public float waitTime = 2f;

//     private Vector3 spawnPoint;
//     private bool isChasing = false;
//     private Transform targetPlayer;

//     private Vector3 currentPatrolPoint;
//     private bool isWaiting = false;
//     private float waitCounter = 0f;

//     void Start()
//     {
//         spawnPoint = transform.position;
//         GetNewPatrolPoint();
//     }

//     void Update()
//     {
//         // QUAN TRỌNG: Chỉ Server mới tính toán AI
//         if (!IsServer) return;

//         FindClosestPlayer();

//         Vector3 enemyPosXZ = new Vector3(transform.position.x, 0, transform.position.z);

//         if (isChasing && targetPlayer != null)
//         {
//             float distanceToPlayer = Vector3.Distance(enemyPosXZ, new Vector3(targetPlayer.position.x, 0, targetPlayer.position.z));
//             ChasePlayer(distanceToPlayer);
//         }
//         else
//         {
//             if (targetPlayer != null)
//             {
//                 float distanceToPlayer = Vector3.Distance(enemyPosXZ, new Vector3(targetPlayer.position.x, 0, targetPlayer.position.z));
//                 CheckForPlayer(distanceToPlayer);
//             }

//             if (!isChasing)
//             {
//                 Patrol(enemyPosXZ);
//             }
//         }
//     }

//     void FindClosestPlayer()
//     {
//         // Tìm tất cả đối tượng có tag "Player" trong game
//         GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
//         float closestDistance = Mathf.Infinity;
//         Transform closestPlayer = null;

//         foreach (GameObject p in players)
//         {
//             float distance = Vector3.Distance(transform.position, p.transform.position);
//             if (distance < closestDistance)
//             {
//                 closestDistance = distance;
//                 closestPlayer = p.transform;
//             }
//         }
//         targetPlayer = closestPlayer;
//     }

//     void CheckForPlayer(float distance)
//     {
//         if (distance < detectionRange) isChasing = true;
//     }

//     void ChasePlayer(float distance)
//     {
//         if (distance > chaseRange)
//         {
//             isChasing = false;
//             GetNewPatrolPoint();
//             return;
//         }
//         MoveTowards(targetPlayer.position);
//     }

//     void Patrol(Vector3 enemyPosXZ)
//     {
//         float distanceToPoint = Vector3.Distance(enemyPosXZ, new Vector3(currentPatrolPoint.x, 0, currentPatrolPoint.z));

//         if (distanceToPoint < 0.2f) isWaiting = true;

//         if (isWaiting)
//         {
//             waitCounter += Time.deltaTime;
//             if (waitCounter >= waitTime)
//             {
//                 isWaiting = false;
//                 waitCounter = 0f;
//                 GetNewPatrolPoint();
//             }
//         }
//         else
//         {
//             MoveTowards(currentPatrolPoint);
//         }
//     }

//     void GetNewPatrolPoint()
//     {
//         Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
//         currentPatrolPoint = new Vector3(spawnPoint.x + randomCircle.x, transform.position.y, spawnPoint.z + randomCircle.y);
//     }

//     void MoveTowards(Vector3 target)
//     {
//         Vector3 direction = target - transform.position;
//         direction.y = 0;
//         if (direction != Vector3.zero)
//         {
//             direction = direction.normalized;
//             transform.position += direction * moveSpeed * Time.deltaTime;
//             transform.forward = direction;
//         }
//     }
// }