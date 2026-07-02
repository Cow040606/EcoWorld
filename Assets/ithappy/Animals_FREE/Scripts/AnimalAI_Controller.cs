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

        // -------------------------------------------------------
        public override void Spawned()
        {
            _mover = GetComponent<CreatureMover>();
            _transform = transform;
            _spawnPosition = _transform.position;
            CurrentHealth = maxHealth;
            _state = AnimalState.Wandering;

            // Chỉ Server/StateAuthority mới chạy AI logic
        }

        // -------------------------------------------------------
        public override void FixedUpdateNetwork()
        {
            // Chỉ máy có StateAuthority mới tính toán AI
            if (!HasStateAuthority) return;
            if (_state == AnimalState.Dead) return;

            _thinkTimer -= Runner.DeltaTime;
            _attackTimer -= Runner.DeltaTime;

            // Cứ thinkInterval giây mới "suy nghĩ" lại 1 lần (tiết kiệm CPU)
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

        // THÚ ĂN CỎ: Bình thường lang thang, bị đánh thì CHẠY
        private void UpdateHerbivoreState(Player_Controller player, float dist)
        {
            if (_state == AnimalState.Fleeing)
            {
                // Đang chạy: kiểm tra đã đủ xa chưa
                if (dist > fleeDistance || player == null)
                {
                    _state = AnimalState.Wandering;
                }
                return; // Không đổi state khi đang chạy
            }

            // Trạng thái bình thường: lang thang
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

        // THÚ ĂN THỊT: Phát hiện player trong range thì ĐUỔI & TẤN CÔNG
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
                // Không thấy player: lang thang
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
                    // Nếu đến nơi rồi thì Idle chờ đổi hướng
                    if (Vector3.Distance(_transform.position, _targetPosition) < 1f)
                        _state = AnimalState.Idle;
                    break;

                case AnimalState.Fleeing:
                    MoveTowards(_targetPosition, true); // Chạy = true (isRun)
                    break;

                case AnimalState.Chasing:
                    // Cập nhật vị trí player liên tục khi đuổi
                    Player_Controller target = GetTargetPlayer();
                    if (target != null) _targetPosition = target.transform.position;
                    MoveTowards(_targetPosition, true);
                    break;

                case AnimalState.Attacking:
                    MoveAnimal(Vector2.zero, false); // Đứng yên đánh
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
            // Chuyển sang tọa độ local của con vật
            Vector2 axis = new Vector2(
                Vector3.Dot(direction, _transform.right),
                Vector3.Dot(direction, _transform.forward)
            );

            // Target nhìn về phía đang đi
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
                // Player chạy ra ngoài tầm, đuổi tiếp
                _state = AnimalState.Chasing;
            }
        }

        // -------------------------------------------------------
        // NHẬN SÁT THƯƠNG (Được gọi từ bên ngoài khi player đánh)
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

            // PHẢN ỨNG KHI BỊ ĐÁNH
            if (animalType == AnimalType.Herbivore)
            {
                // Chạy ngược hướng với player tấn công
                Player_Controller attacker = FindPlayerByRef(attackerRef);
                if (attacker != null)
                {
                    Vector3 fleeDir = (_transform.position - attacker.transform.position).normalized;
                    _targetPosition = _transform.position + fleeDir * fleeDistance;
                    _state = AnimalState.Fleeing;
                    Debug.Log($"[Animal] {gameObject.name} (Ăn cỏ) bị đánh → CHẠY!");
                }
            }
            else // Carnivore
            {
                // Đuổi theo kẻ tấn công
                Player_Controller attacker = FindPlayerByRef(attackerRef);
                if (attacker != null)
                {
                    _targetPlayerRef = attackerRef;
                    _targetPosition = attacker.transform.position;
                    _state = AnimalState.Chasing;
                    Debug.Log($"[Animal] {gameObject.name} (Ăn thịt) bị đánh → ĐUỔI!");
                }
            }
        }

        private void Die()
        {
            _state = AnimalState.Dead;
            MoveAnimal(Vector2.zero, false);
            // TODO: Phát animation chết, drop item, v.v.
            // Despawn sau 3 giây
            StartCoroutine(DespawnAfterDelay(3f));
            Debug.Log($"[Animal] {gameObject.name} đã chết!");
        }

        private IEnumerator DespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (HasStateAuthority) Runner.Despawn(Object);
        }

        // -------------------------------------------------------
        // TÌM PLAYER GẦN NHẤT
        // -------------------------------------------------------
        private Player_Controller FindNearestPlayer(out float distance)
        {
            Player_Controller nearest = null;
            distance = float.MaxValue;

            // Tìm tất cả player trong scene
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