using UnityEngine;
using Fusion;
using System.Collections;

namespace ithappy.Animals_FREE
{
    public enum AnimalType { Herbivore, Carnivore } // Ăn cỏ / Ăn thịt

    [RequireComponent(typeof(CreatureMover))]
    [RequireComponent(typeof(NetworkObject))]
    public class AnimalAI_Controller : NetworkBehaviour
    {
        [Header("Loại thú")]
        public AnimalType animalType = AnimalType.Herbivore;

        [Header("Chỉ số")]
        public float maxHealth = 100f;
        [Networked] public float CurrentHealth { get; set; }

        [Header("Tốc độ & Phạm vi")]
        public float wanderRadius = 10f;       // Bán kính đi lang thang
        public float detectionRange = 8f;      // Phạm vi phát hiện player
        public float attackRange = 2f;         // Phạm vi tấn công
        public float attackDamage = 10f;
        public float attackCooldown = 1.5f;
        public float fleeDistance = 15f;       // Chạy đến khi cách xa bao nhiêu

        [Header("Thời gian")]
        public float wanderInterval = 3f;      // Cứ bao giây đổi hướng lang thang
        public float thinkInterval = 0.2f;     // Cứ bao giây AI "suy nghĩ" lại

        [Header("Vật phẩm rơi ra (Drop Items)")]
        public NetworkObject meatPrefab;       // Kéo prefab Thịt vào đây trên Inspector
        public NetworkObject skinPrefab;       // Kéo prefab Da vào đây trên Inspector

        // --- Networked State ---
        [Networked] private AnimalState _state { get; set; }
        [Networked] private Vector3 _targetPosition { get; set; }
        [Networked] private PlayerRef _targetPlayerRef { get; set; }

        private enum AnimalState { Idle, Wandering, Fleeing, Chasing, Attacking, Dead }

        // --- Local Cache ---
        private CreatureMover _mover;
        private Transform _transform;
        private Vector3 _spawnPosition;

        private float _attackTimer;
        private float _wanderTimer;
        private float _thinkTimer;

        // --- Debug Double Press Y Variables ---
        private float _lastYKeyPressTime;
        private const float DOUBLE_PRESS_TIME_THRESHOLD = 0.5f; // Thời gian tối đa giữa 2 lần nhấn

        // -------------------------------------------------------
        public override void Spawned()
        {
            _mover = GetComponent<CreatureMover>();
            _transform = transform;
            _spawnPosition = _transform.position;
            CurrentHealth = maxHealth;
            _state = AnimalState.Wandering;
        }

        // --- Thêm hàm Update chạy cục bộ để bắt sự kiện phím của người chơi ---
        private void Update()
        {
            // Bắt sự kiện nhấn phím Y
            if (Input.GetKeyDown(KeyCode.Y))
            {
                float timeSinceLastClick = Time.time - _lastYKeyPressTime;

                if (timeSinceLastClick <= DOUBLE_PRESS_TIME_THRESHOLD)
                {
                    Debug.Log($"[Debug] Đã nhấn Y 2 lần liên tiếp. Kích hoạt tiêu diệt nhanh: {gameObject.name}");
                    RPC_AnimalTakeDamage(maxHealth, Runner.LocalPlayer);
                }

                _lastYKeyPressTime = Time.time;
            }
        }

        // -------------------------------------------------------
        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (_state == AnimalState.Dead) return;

            _thinkTimer -= Runner.DeltaTime;
            _attackTimer -= Runner.DeltaTime;

            if (_thinkTimer <= 0f)
            {
                _thinkTimer = thinkInterval;
                UpdateAIState();
            }

            ExecuteCurrentState();
        }

        // -------------------------------------------------------
        // CẬP NHẬT TRẠNG THÁI AI (Bộ não)
        // -------------------------------------------------------
        private void UpdateAIState()
        {
            if (_state == AnimalState.Dead) return;

            Player_Controller nearestPlayer = FindNearestPlayer(out float distToPlayer);

            switch (animalType)
            {
                case AnimalType.Herbivore:
                    UpdateHerbivoreState(nearestPlayer, distToPlayer);
                    break;

                case AnimalType.Carnivore:
                    UpdateCarnivoreState(nearestPlayer, distToPlayer);
                    break;
            }
        }

        private void UpdateHerbivoreState(Player_Controller player, float dist)
        {
            if (_state == AnimalState.Fleeing)
            {
                if (dist > fleeDistance || player == null)
                {
                    _state = AnimalState.Wandering;
                }
                return;
            }

            if (_state != AnimalState.Fleeing)
            {
                _wanderTimer -= thinkInterval;
                if (_wanderTimer <= 0f)
                {
                    SetNewWanderTarget();
                    _wanderTimer = wanderInterval;
                    _state = AnimalState.Wandering;
                }
            }
        }

        private void UpdateCarnivoreState(Player_Controller player, float dist)
        {
            if (player != null && dist <= detectionRange)
            {
                if (dist <= attackRange)
                {
                    _state = AnimalState.Attacking;
                    _targetPosition = player.transform.position;
                }
                else
                {
                    _state = AnimalState.Chasing;
                    _targetPosition = player.transform.position;
                    _targetPlayerRef = player.Object.InputAuthority;
                }
            }
            else
            {
                _wanderTimer -= thinkInterval;
                if (_wanderTimer <= 0f || _state == AnimalState.Chasing)
                {
                    SetNewWanderTarget();
                    _wanderTimer = wanderInterval;
                    _state = AnimalState.Wandering;
                }
            }
        }

        // -------------------------------------------------------
        // THỰC THI HÀNH ĐỘNG (Cơ thể)
        // -------------------------------------------------------
        private void ExecuteCurrentState()
        {
            switch (_state)
            {
                case AnimalState.Idle:
                    MoveAnimal(Vector2.zero, false);
                    break;

                case AnimalState.Wandering:
                    MoveTowards(_targetPosition, false);
                    if (Vector3.Distance(_transform.position, _targetPosition) < 1f)
                        _state = AnimalState.Idle;
                    break;

                case AnimalState.Fleeing:
                    MoveTowards(_targetPosition, true);
                    break;

                case AnimalState.Chasing:
                    Player_Controller target = GetTargetPlayer();
                    if (target != null) _targetPosition = target.transform.position;
                    MoveTowards(_targetPosition, true);
                    break;

                case AnimalState.Attacking:
                    MoveAnimal(Vector2.zero, false);
                    TryAttack();
                    break;
            }
        }

        // -------------------------------------------------------
        // GỌI CreatureMover để di chuyển
        // -------------------------------------------------------
        private void MoveTowards(Vector3 destination, bool isRun)
        {
            Vector3 direction = (destination - _transform.position);
            direction.y = 0;

            if (direction.sqrMagnitude < 0.1f)
            {
                MoveAnimal(Vector2.zero, false);
                return;
            }

            direction.Normalize();
            Vector2 axis = new Vector2(
                Vector3.Dot(direction, _transform.right),
                Vector3.Dot(direction, _transform.forward)
            );

            Vector3 lookTarget = _transform.position + direction * 5f;
            _mover.SetInput(axis, lookTarget, isRun, false);
        }

        private void MoveAnimal(Vector2 axis, bool isRun)
        {
            _mover.SetInput(axis, _transform.position + _transform.forward, isRun, false);
        }

        // -------------------------------------------------------
        // TẤN CÔNG
        // -------------------------------------------------------
        private void TryAttack()
        {
            if (_attackTimer > 0f) return;

            Player_Controller target = GetTargetPlayer();
            if (target == null) return;

            float dist = Vector3.Distance(_transform.position, target.transform.position);
            if (dist <= attackRange * 1.2f)
            {
                target.RPC_TakeDame(attackDamage);
                _attackTimer = attackCooldown;
                Debug.Log($"[Animal] {gameObject.name} tấn công player gây {attackDamage} dame!");
            }
            else
            {
                _state = AnimalState.Chasing;
            }
        }

        // -------------------------------------------------------
        // NHẬN SÁT THƯƠNG
        // -------------------------------------------------------
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_AnimalTakeDamage(float damage, PlayerRef attackerRef)
        {
            if (_state == AnimalState.Dead) return;

            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

            if (CurrentHealth <= 0)
            {
                Die();
                return;
            }

            if (animalType == AnimalType.Herbivore)
            {
                Player_Controller attacker = FindPlayerByRef(attackerRef);
                if (attacker != null)
                {
                    Vector3 fleeDir = (_transform.position - attacker.transform.position).normalized;
                    _targetPosition = _transform.position + fleeDir * fleeDistance;
                    _state = AnimalState.Fleeing;
                }
            }
            else // Carnivore
            {
                Player_Controller attacker = FindPlayerByRef(attackerRef);
                if (attacker != null)
                {
                    _targetPlayerRef = attackerRef;
                    _targetPosition = attacker.transform.position;
                    _state = AnimalState.Chasing;
                }
            }
        }

       private void Die()
{
    _state = AnimalState.Dead;
    MoveAnimal(Vector2.zero, false);
    
    DropItems();
    Debug.Log($"[Animal] {gameObject.name} đã chết và biến mất!");

    if (HasStateAuthority)
    {
        // TÌM SPAWNER VÀ THÔNG BÁO THÚ ĐÃ CHẾT
        AnimalSpawner spawner = FindObjectOfType<AnimalSpawner>();
        if (spawner != null)
        {
            spawner.OnAnimalDied(animalType);
        }

        Runner.Despawn(Object);
    }
}

        // -------------------------------------------------------
        // LOGIC RỚT ĐỒ (THỊT VÀ DA)
        // -------------------------------------------------------
        private void DropItems()
        {
            if (!HasStateAuthority) return;

            int meatCount = Random.Range(1, 4); 
            int skinCount = Random.Range(0, 3); 

            SpawnItemNet(meatPrefab, meatCount);
            SpawnItemNet(skinPrefab, skinCount);
        }

        private void SpawnItemNet(NetworkObject itemPrefab, int count)
        {
            if (itemPrefab == null || count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 randomOffset = new Vector3(Random.Range(-0.8f, 0.8f), 0.5f, Random.Range(-0.8f, 0.8f));
                Vector3 spawnPosition = _transform.position + randomOffset;

                Runner.Spawn(itemPrefab, spawnPosition, Quaternion.identity);
            }
        }

        // -------------------------------------------------------
        // TÌM PLAYER GẦN NHẤT
        // -------------------------------------------------------
        private Player_Controller FindNearestPlayer(out float distance)
        {
            Player_Controller nearest = null;
            distance = float.MaxValue;

            foreach (var playerObj in Runner.ActivePlayers)
            {
                NetworkObject netObj = Runner.GetPlayerObject(playerObj);
                if (netObj == null) continue;

                Player_Controller pc = netObj.GetComponent<Player_Controller>();
                if (pc == null) continue;

                float dist = Vector3.Distance(_transform.position, pc.transform.position);
                if (dist < distance)
                {
                    distance = dist;
                    nearest = pc;
                }
            }
            return nearest;
        }

        private Player_Controller GetTargetPlayer()
        {
            NetworkObject netObj = Runner.GetPlayerObject(_targetPlayerRef);
            return netObj?.GetComponent<Player_Controller>();
        }

        private Player_Controller FindPlayerByRef(PlayerRef pRef)
        {
            NetworkObject netObj = Runner.GetPlayerObject(pRef);
            return netObj?.GetComponent<Player_Controller>();
        }

        // -------------------------------------------------------
        // ĐẶT ĐIỂM LANG THANG NGẪU NHIÊN
        // -------------------------------------------------------
        private void SetNewWanderTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            _targetPosition = _spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
    }
}