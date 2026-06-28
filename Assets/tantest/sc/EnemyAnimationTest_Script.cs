using UnityEngine;
using Fusion;

namespace ithappy.Animals_FREE
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NetworkObject))]
    public class EnemyAnimationTest_Script : NetworkBehaviour
    {
        [Header("Animation Test Settings")]
        public float interval = 3.0f;

        [Header("Component Reference")]
        public Animator animator;

        private float _testTimer;
        private int _currentTestState = 0; // 0: chatcay, 1: dapda

        public override void Spawned()
        {
            base.Spawned();
            
            if (animator == null)
                animator = GetComponent<Animator>();

            _testTimer = interval;
            
            if (animator != null)
            {
                // Gọi state 0 khi mới spawn
                animator.SetInteger("AnimState", 0); 
                _currentTestState = 1;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (animator == null) return;

            _testTimer -= Runner.DeltaTime;

            if (_testTimer <= 0f)
            {
                _testTimer = interval;

                if (_currentTestState == 0)
                {
                    animator.SetInteger("AnimState", 0); // Kích hoạt dây về chatcay
                    _currentTestState = 1; 
                }
                else
                {
                    animator.SetInteger("AnimState", 1); // Kích hoạt dây sang dapda
                    _currentTestState = 0; 
                }
            }
        }
    }
}