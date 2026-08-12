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
        [Header("Loại thú")]
        public AnimalType animalType = AnimalType.Herbivore;

        [Header("Chỉ số & EXP")]
        public float maxHealth = 100f;
        [Networked] public float CurrentHealth { get; set; }
        public float expReward = 20f; // CHỈNH EXP RỚT CHO ĐỘNG VẬT Ở ĐÂY

        [Header("Tốc độ & Phạm vi")]
        public float wanderRadius = 10f;
        public float detectionRange = 8f;
        public float attackRange = 2f;
        public float attackDamage = 10f;
        public float attackCooldown = 1.5f;
        public float fleeDistance = 15f;

        [Header("Thời gian")]
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
                    if (target != null) _targetPosition = target.transform.position;
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
            Vector2 axis = new Vector2(Vector3.Dot(direction, _transform.right), Vector3.Dot(direction, _transform.forward));
            Vector3 lookTarget = _transform.position + direction * 5f;
            _mover.SetInput(axis, lookTarget, isRun, false);
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
                GiveExpToKiller(attackerRef, expReward); // CẤP KINH NGHIỆM
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

        // ĐÃ FIX: Dùng playerRef == PlayerRef.None để không bị lỗi CS1061
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
            _targetPosition = _spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
    }
}