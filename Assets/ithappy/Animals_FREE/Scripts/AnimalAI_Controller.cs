using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;

namespace ithappy.Animals_FREE
{
    public enum AnimalType { Herbivore, Carnivore }

    [RequireComponent(typeof(CreatureMover))]
    [RequireComponent(typeof(NetworkObject))]
    public class AnimalAI_Controller : NetworkBehaviour
    {
        [Header("Loáº¡i thÃº")]
        public AnimalType animalType = AnimalType.Herbivore;

        [Header("Chá»‰ sá»‘ & EXP")]
        public float maxHealth = 100f;
        [Networked] public float CurrentHealth { get; set; }
        public float expReward = 20f; // CHá»ˆNH EXP Rá»šT CHO Äá»˜NG Váº¬T á»ž ÄÃ‚Y

        [Header("Tá»‘c Ä‘á»™ & Pháº¡m vi")]
        public float wanderRadius = 10f;
        public float detectionRange = 8f;
        public float attackRange = 2f;
        public float attackDamage = 10f;
        public float attackCooldown = 1.5f;
        public float fleeDistance = 15f;

        [Header("Thá»i gian")]
        public float wanderInterval = 3f;
        public float thinkInterval = 0.2f;

        [Header("Drop Settings")]
        public List<GameObject> dropItems;
        [Range(0f, 100f)] public float dropChance = 100f;

        [Networked] private AnimalState _state { get; set; }
        [Networked] private Vector3 _targetPosition { get; set; }
        [Networked] private PlayerRef _targetPlayerRef { get; set; }

        private enum AnimalState { Idle, Wandering, Fleeing, Chasing, Attacking, Dead }

        private CreatureMover _mover;
        private Transform _transform;
        private Vector3 _spawnPosition;
        private float _attackTimer;
        private float _wanderTimer;
        private float _thinkTimer;

        public override void Spawned()
        {
            _mover = GetComponent<CreatureMover>();
            _transform = transform;
            _spawnPosition = _transform.position;
            CurrentHealth = maxHealth;
            _state = AnimalState.Wandering;
        }

                public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _state == AnimalState.Dead) return;

            _thinkTimer -= Runner.DeltaTime;
            _attackTimer -= Runner.DeltaTime;

            if (_thinkTimer <= 0f)
            {
                _thinkTimer = thinkInterval;
                UpdateAIState();
            }
            ExecuteCurrentState();

            ClampPositionToNavMesh();
        }

                private void ClampPositionToNavMesh()
        {
            Vector3 currentPos = _transform.position;
            if (UnityEngine.AI.NavMesh.SamplePosition(currentPos, out UnityEngine.AI.NavMeshHit hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                // Chỉ snap tọa độ X và Z, giữ nguyên Y để CharacterController xử lý trọng lực và bám đất
                Vector3 targetPos = new Vector3(hit.position.x, currentPos.y, hit.position.z);
                
                // Tính khoảng cách trên mặt phẳng ngang (X-Z)
                float flatDistance = Vector2.Distance(new Vector2(currentPos.x, currentPos.z), new Vector2(hit.position.x, hit.position.z));
                if (flatDistance > 0.05f)
                {
                    _transform.position = targetPos;
                }
            }
            else
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(_spawnPosition, out UnityEngine.AI.NavMeshHit spawnHit, 100f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    _transform.position = spawnHit.position;
                    _targetPosition = spawnHit.position;
                    _state = AnimalState.Idle;
                    // Bỏ comment dòng dưới để tránh spam Console gây lag tụt FPS
                    // Debug.LogWarning("[AnimalAI] Animal " + gameObject.name + " fell off NavMesh! Teleported back to spawn position.");
                }
            }
        }

        private void UpdateAIState()
        {
            if (_state == AnimalState.Dead) return;
            Player_Controller nearestPlayer = FindNearestPlayer(out float distToPlayer);

            switch (animalType)
            {
                case AnimalType.Herbivore: UpdateHerbivoreState(nearestPlayer, distToPlayer); break;
                case AnimalType.Carnivore: UpdateCarnivoreState(nearestPlayer, distToPlayer); break;
            }
        }

                private void UpdateHerbivoreState(Player_Controller player, float dist)
        {
            if (Vector3.Distance(_transform.position, _spawnPosition) > wanderRadius * 3f)
            {
                _targetPosition = _spawnPosition;
                _state = AnimalState.Wandering;
                return;
            }

            if (_state == AnimalState.Fleeing)
            {
                if (dist > fleeDistance || player == null) _state = AnimalState.Wandering;
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
            if (Vector3.Distance(_transform.position, _spawnPosition) > wanderRadius * 3f)
            {
                SetNewWanderTarget();
                _wanderTimer = wanderInterval;
                _state = AnimalState.Wandering;
                return;
            }

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

        private void ExecuteCurrentState()
        {
            switch (_state)
            {
                case AnimalState.Idle: MoveAnimal(Vector2.zero, false); break;
                case AnimalState.Wandering:
                    MoveTowards(_targetPosition, false);
                    if (Vector3.Distance(_transform.position, _targetPosition) < 1f) _state = AnimalState.Idle;
                    break;
                case AnimalState.Fleeing: MoveTowards(_targetPosition, true); break;
                                case AnimalState.Chasing:
                    Player_Controller target = GetTargetPlayer();
                    if (target != null)
                    {
                        Vector3 playerPos = target.transform.position;
                        if (UnityEngine.AI.NavMesh.SamplePosition(playerPos, out UnityEngine.AI.NavMeshHit hit, detectionRange, UnityEngine.AI.NavMesh.AllAreas))
                        {
                            _targetPosition = hit.position;
                        }
                        else
                        {
                            _targetPosition = playerPos;
                        }
                    }
                    MoveTowards(_targetPosition, true);
                    break;
                case AnimalState.Attacking:
                    MoveAnimal(Vector2.zero, false);
                    TryAttack();
                    break;
            }
        }

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

            // Tính toán lực tránh nhau để không đè lên nhau khi đuổi player
            Vector3 separation = CalculateSeparationForce();
            Vector3 finalDirection = direction + separation;
            if (finalDirection.sqrMagnitude > 0.01f)
            {
                finalDirection.Normalize();
            }
            else
            {
                finalDirection = direction;
            }

            // Đã sửa: Truyền vector tịnh tiến thẳng phía trước (0, 1) để tránh việc bị nhân lệch góc quay hai lần
            Vector2 axis = new Vector2(0f, 1f);
            Vector3 lookTarget = _transform.position + finalDirection * 5f;
            _mover.SetInput(axis, lookTarget, isRun, false);
        }

        private Vector3 CalculateSeparationForce()
        {
            Vector3 separation = Vector3.zero;
            float personalSpace = 2.0f; // Khoảng cách tối thiểu giãn cách giữa các thú
            
            // Tối ưu hóa cực độ: Dùng Physics.OverlapSphere thay vì FindObjectsByType để tránh lag
            Collider[] colliders = Physics.OverlapSphere(_transform.position, personalSpace);
            int neighborsCount = 0;
            
            foreach (var col in colliders)
            {
                AnimalAI_Controller other = col.GetComponent<AnimalAI_Controller>();
                if (other == null || other == this || other._state == AnimalState.Dead) continue;
                
                float distance = Vector3.Distance(_transform.position, other.transform.position);
                if (distance < personalSpace && distance > 0.05f)
                {
                    Vector3 pushDir = (_transform.position - other.transform.position).normalized;
                    separation += pushDir * (personalSpace - distance);
                    neighborsCount++;
                }
            }
            
            if (neighborsCount > 0)
            {
                separation /= neighborsCount;
                separation = Vector3.ClampMagnitude(separation, 1.5f);
            }
            
            return separation;
        }

        private void MoveAnimal(Vector2 axis, bool isRun)
        {
            _mover.SetInput(axis, _transform.position + _transform.forward, isRun, false);
        }

        private void TryAttack()
        {
            if (_attackTimer > 0f) return;

            Player_Controller target = GetTargetPlayer();
            if (target == null) return;

            if (Vector3.Distance(_transform.position, target.transform.position) <= attackRange * 1.2f)
            {
                target.RPC_TakeDame(attackDamage);
                _attackTimer = attackCooldown;
            }
            else _state = AnimalState.Chasing;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_AnimalTakeDamage(float damage, PlayerRef attackerRef)
        {
            if (_state == AnimalState.Dead) return;

            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

            if (CurrentHealth <= 0)
            {
                Die();
                GiveExpToKiller(attackerRef, expReward); // Cáº¤P KINH NGHIá»†M
                return;
            }

                        if (animalType == AnimalType.Herbivore)
            {
                Player_Controller attacker = FindPlayerByRef(attackerRef);
                if (attacker != null)
                {
                    Vector3 fleeDir = (_transform.position - attacker.transform.position).normalized;
                    Vector3 targetFlee = _transform.position + fleeDir * fleeDistance;
                    if (UnityEngine.AI.NavMesh.SamplePosition(targetFlee, out UnityEngine.AI.NavMeshHit hit, fleeDistance, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        _targetPosition = hit.position;
                    }
                    else
                    {
                        _targetPosition = _transform.position;
                    }
                    _state = AnimalState.Fleeing;
                }
            }
            else
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

        // ÄÃƒ FIX: DÃ¹ng playerRef == PlayerRef.None Ä‘á»ƒ khÃ´ng bá»‹ lá»—i CS1061
        private void GiveExpToKiller(PlayerRef playerRef, float expAmount)
        {
            if (!HasStateAuthority || playerRef == PlayerRef.None) return;

            NetworkObject playerObj = Runner.GetPlayerObject(playerRef);
            if (playerObj != null)
            {
                Player_Controller player = playerObj.GetComponent<Player_Controller>();
                if (player != null) player.Server_AddExp(expAmount);
            }
        }

        private void Die()
        {
            _state = AnimalState.Dead;
            MoveAnimal(Vector2.zero, false);
            DropItem();

            if (HasStateAuthority) Runner.Despawn(Object);
        }

        private void DropItem()
        {
            if (!HasStateAuthority) return;

            if (dropItems != null && dropItems.Count > 0 && Random.Range(0f, 100f) <= dropChance)
            {
                GameObject itemToDrop = dropItems[Random.Range(0, dropItems.Count)];
                if (itemToDrop != null)
                {
                    NetworkObject netObj = itemToDrop.GetComponent<NetworkObject>();
                    if (netObj != null) Runner.Spawn(netObj, transform.position + Vector3.up * 1f, Quaternion.identity);
                }
            }
        }

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

                private void SetNewWanderTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 randomPos = _spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            if (UnityEngine.AI.NavMesh.SamplePosition(randomPos, out UnityEngine.AI.NavMeshHit hit, wanderRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                _targetPosition = hit.position;
            }
            else
            {
                _targetPosition = _spawnPosition;
            }
        }
    }
}



