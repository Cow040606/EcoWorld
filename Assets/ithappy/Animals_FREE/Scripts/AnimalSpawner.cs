using UnityEngine;
using Fusion;
using System.Collections;

namespace ithappy.Animals_FREE
{
    public class AnimalSpawner : NetworkBehaviour
    {
        [Header("Prefabs")]
        public NetworkObject herbivore_Prefab;  // Thú ăn cỏ (VD: Deer, Rabbit)
        public NetworkObject carnivore_Prefab;  // Thú ăn thịt (VD: Wolf, Tiger)

        [Header("Vùng Spawn")]
        public Transform spawnCenter;           // Tâm vùng spawn
        public float spawnRadius = 30f;         // Bán kính vùng spawn

        [Header("Số lượng")]
        public int maxHerbivores = 5;
        public int maxCarnivores = 3;
        public float respawnDelay = 10f;        // Sau bao giây spawn lại

        private int _currentHerbivores = 0;
        private int _currentCarnivores = 0;

        public override void Spawned()
        {
            if (!HasStateAuthority) return;
            // Spawn lần đầu xen kẽ
            StartCoroutine(InitialSpawn());
        }

        private IEnumerator InitialSpawn()
        {
            yield return new WaitForSeconds(1f); // Chờ scene load xong

            // Spawn xen kẽ: 1 ăn cỏ, 1 ăn thịt, 1 ăn cỏ...
            int total = maxHerbivores + maxCarnivores;
            for (int i = 0; i < total; i++)
            {
                // Xen kẽ: chẵn = ăn cỏ, lẻ = ăn thịt
                if (i % 2 == 0 && _currentHerbivores < maxHerbivores)
                    SpawnAnimal(AnimalType.Herbivore);
                else if (_currentCarnivores < maxCarnivores)
                    SpawnAnimal(AnimalType.Carnivore);

                yield return new WaitForSeconds(0.2f); // Tránh spam
            }

            // Bắt đầu vòng lặp kiểm tra respawn
            StartCoroutine(RespawnLoop());
        }

        private IEnumerator RespawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(respawnDelay);

                // Spawn thêm nếu thiếu, xen kẽ
                bool needHerb = _currentHerbivores < maxHerbivores;
                bool needCarn = _currentCarnivores < maxCarnivores;

                if (needHerb) SpawnAnimal(AnimalType.Herbivore);
                if (needCarn) SpawnAnimal(AnimalType.Carnivore);
            }
        }

        private void SpawnAnimal(AnimalType type)
        {
            if (!HasStateAuthority) return;

            // Lấy prefab đúng loại
            NetworkObject prefab = (type == AnimalType.Herbivore)
                ? herbivore_Prefab
                : carnivore_Prefab;

            if (prefab == null)
            {
                //Debug.LogWarning($"[Spawner] Chưa gán prefab cho {type}!");
                return;
            }

            // Random vị trí trong vùng spawn
            Vector3 spawnPos = GetRandomSpawnPosition();

            // Fusion spawn qua mạng
            NetworkObject spawnedObj = Runner.Spawn(
                prefab,
                spawnPos,
                Quaternion.Euler(0, Random.Range(0f, 360f), 0)
            );

            // Cập nhật bộ đếm
            AnimalAI_Controller ai = spawnedObj.GetComponent<AnimalAI_Controller>();
            if (ai != null)
            {
                ai.animalType = type;
                if (type == AnimalType.Herbivore) _currentHerbivores++;
                else _currentCarnivores++;
            }

            //Debug.Log($"[Spawner] Spawned {type} tại {spawnPos}");
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = spawnCenter != null ? spawnCenter.position : Vector3.zero;
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = center + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Snap xuống mặt đất
            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
                pos.y = hit.point.y;

            return pos;
        }

        // Gọi hàm này khi 1 con thú chết để giảm bộ đếm
        public void OnAnimalDied(AnimalType type)
        {
            if (type == AnimalType.Herbivore) _currentHerbivores = Mathf.Max(0, _currentHerbivores - 1);
            else _currentCarnivores = Mathf.Max(0, _currentCarnivores - 1);
        }
    }
}