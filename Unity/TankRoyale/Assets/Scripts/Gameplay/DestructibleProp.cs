using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Generic 1-hit world prop destruction with optional coin drop.
    /// </summary>
    [DisallowMultipleComponent]
    public class DestructibleProp : MonoBehaviour
    {
        [SerializeField] private int hitPoints = 1;
        [SerializeField] private bool dropCoinOnDestroy = true;

        private bool _destroyed;

        public void ApplyHit(int damage = 1)
        {
            if (_destroyed)
            {
                return;
            }

            hitPoints = Mathf.Max(0, hitPoints - Mathf.Max(1, damage));
            if (hitPoints > 0)
            {
                return;
            }

            _destroyed = true;
            if (dropCoinOnDestroy)
            {
                CoinDropEffect.SpawnAt(transform.position + Vector3.up * 0.25f);
            }

            Destroy(gameObject);
        }
    }
}
