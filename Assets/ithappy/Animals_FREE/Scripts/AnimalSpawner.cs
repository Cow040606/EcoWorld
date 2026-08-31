using UnityEngine;
using Fusion;
using System.Collections;

namespace ithappy.Animals_FREE
{
    public class AnimalSpawner : NetworkBehaviour
    {
        [Header("Prefabs")]
        public NetworkObject herbivore_Prefab;  // ThÃº Äƒn cá» (VD: Deer, Rabbit)
        public NetworkObject carnivore_Prefab;  // ThÃº Äƒn thá»‹t (VD: Wolf, Tiger)

        [Header("VÃ¹ng Spawn")]
        public Transform spawnCenter;           // TÃ¢m vÃ¹ng spawn
        public float spawnRadius = 30f;         // BÃ¡n kÃ­nh vÃ¹ng spawn

        [Header("Sá»‘ lÆ°á»£ng")]
        public int maxHerbivores = 5;
        public int maxCarnivores = 3;
        public float respawnDelay = 10f;        // Sau bao giÃ¢y spawn láº¡i

        private int _currentHerbivores = 0;
        private int _currentCarnivores = 0;

        public override void Spawned()
        {
            if (!HasStateAuthority) return;
            // Spawn láº§n Ä‘áº§u xen káº½
            StartCoroutine(InitialSpawn());
        }

        private IEnumerator InitialSpawn()
        {
            yield return new WaitForSeconds(1f); // Chá» scene load xong

            // Spawn xen káº½: 1 Äƒn cá», 1 Äƒn thá»‹t, 1 Äƒn cá»...
            int total = maxHerbivores + maxCarnivores;
            for (int i = 0; i < total; i++)
            {
                // Xen káº½: cháºµn = Äƒn cá», láº» = Äƒn thá»‹t
                if (i % 2 == 0 && _currentHerbivores < maxHerbivores)
                    SpawnAnimal(AnimalType.Herbivore);
                else if (_currentCarnivores < maxCarnivores)
                    SpawnAnimal(AnimalType.Carnivore);

                yield return new WaitForSeconds(0.2f); // TrÃ¡nh spam
            }

            // Báº¯t Ä‘áº§u vÃ²ng láº·p kiá»ƒm tra respawn
            StartCoroutine(RespawnLoop());
        }

        private IEnumerator RespawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(respawnDelay);

                // Spawn thÃªm náº¿u thiáº¿u, xen káº½
                bool needHerb = _currentHerbivores < maxHerbivores;
                bool needCarn = _currentCarnivores < maxCarnivores;

                if (needHerb) SpawnAnimal(AnimalType.Herbivore);
                if (needCarn) SpawnAnimal(AnimalType.Carnivore);
            }
        }

        private void SpawnAnimal(AnimalType type)
        {
            if (!HasStateAuthority) return;

            // Láº¥y prefab Ä‘Ãºng loáº¡i
            NetworkObject prefab = (type == AnimalType.Herbivore)
                ? herbivore_Prefab
                : carnivore_Prefab;

            if (prefab == null)
            {
                //Debug.LogWarning($"[Spawner] ChÆ°a gÃ¡n prefab cho {type}!");
                return;
            }

            // Random vá»‹ trÃ­ trong vÃ¹ng spawn
            Vector3 spawnPos = GetRandomSpawnPosition();

            // Fusion spawn qua máº¡ng
            NetworkObject spawnedObj = Runner.Spawn(
                prefab,
                spawnPos,
                Quaternion.Euler(0, Random.Range(0f, 360f), 0)
            );

            // Cáº­p nháº­t bá»™ Ä‘áº¿m
            AnimalAI_Controller ai = spawnedObj.GetComponent<AnimalAI_Controller>();
            if (ai != null)
            {
                ai.animalType = type;
                if (type == AnimalType.Herbivore) _currentHerbivores++;
                else _currentCarnivores++;
            }

            //Debug.Log($"[Spawner] Spawned {type} táº¡i {spawnPos}");
        }

                private Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = spawnCenter != null ? spawnCenter.position : Vector3.zero;
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = center + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Sử dụng NavMesh để đảm bảo vị trí spawn nằm trên đất liền có thể đi lại được
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out UnityEngine.AI.NavMeshHit hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }

            // Snap xuống mặt đất (Fallback)
            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit rHit, 100f))
                pos.y = rHit.point.y;

            return pos;
        }

        // Gá»i hÃ m nÃ y khi 1 con thÃº cháº¿t Ä‘á»ƒ giáº£m bá»™ Ä‘áº¿m
        public void OnAnimalDied(AnimalType type)
        {
            if (type == AnimalType.Herbivore) _currentHerbivores = Mathf.Max(0, _currentHerbivores - 1);
            else _currentCarnivores = Mathf.Max(0, _currentCarnivores - 1);
        }
    }
}
