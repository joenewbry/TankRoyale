using UnityEngine;

namespace TankRoyale.Gameplay
{
    public enum PowerupType
    {
        Ricochet,       // Bullets bounce off walls
        Armor,          // Absorbs one hit
        BlockBreaker,   // Bullets destroy terrain
        Heal            // Restores 1 HP (gear icon)
    }

    /// <summary>
    /// Attaches to a collectible prefab. On trigger, applies its effect to the
    /// collecting tank and destroys itself.
    /// </summary>
    public class PowerupPickup : MonoBehaviour
    {
        [Header("Type")]
        [SerializeField] public PowerupType powerupType = PowerupType.Ricochet;

        [Header("Visuals")]
        [SerializeField] private float bobAmplitude  = 0.15f;
        [SerializeField] private float bobSpeed      = 2f;
        [SerializeField] private float rotateSpeed   = 90f;

        [Header("Loot Magnet")]
        [SerializeField] private float magnetRadius = 9f;
        [SerializeField] private float magnetPullSpeed = 12f;

        private Vector3 _startPos;
        private TankController _playerTank;

        private void Start()
        {
            _startPos = transform.position;
            CachePlayerTank();
        }

        private void Update()
        {
            // Bob and rotate so pickups are easy to spot
            float y = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            TryApplyLootMagnet();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var tank = other.GetComponentInParent<TankController>();
            if (tank == null) return;

            string playerId = tank.PlayerId;

            switch (powerupType)
            {
                case PowerupType.Ricochet:
                    PowerupManager.Instance?.ApplyPowerup(playerId, PowerupManager.RicochetPowerup);
                    break;

                case PowerupType.Armor:
                    PowerupManager.Instance?.ApplyPowerup(playerId, PowerupManager.ArmorPowerup);
                    break;

                case PowerupType.BlockBreaker:
                    PowerupManager.Instance?.ApplyPowerup(playerId, PowerupManager.BlockbreakerPowerup);
                    break;

                case PowerupType.Heal:
                    // Heal restores 1 HP — applied directly to TankController
                    tank.Heal(1);
                    break;
            }

            Debug.Log($"[PowerupPickup] {powerupType} collected by {playerId}");
            TankRoyale.Audio.AudioManager.Instance?.PlayPowerupPickupSFX();
            Destroy(gameObject);
        }

        private void TryApplyLootMagnet()
        {
            if (PowerupManager.Instance == null) return;
            if (_playerTank == null || !_playerTank.gameObject.activeInHierarchy)
            {
                CachePlayerTank();
                if (_playerTank == null) return;
            }

            if (!PowerupManager.Instance.IsPowerupActive(_playerTank.PlayerId, PowerupManager.LootMagnetPowerup))
            {
                return;
            }

            Vector3 playerPos = _playerTank.transform.position;
            Vector3 delta = playerPos - transform.position;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist > magnetRadius || dist < 0.05f) return;

            Vector3 dir = delta / dist;
            Vector3 next = transform.position + dir * (magnetPullSpeed * Time.deltaTime);
            next.y = transform.position.y;
            transform.position = next;
        }

        private void CachePlayerTank()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            _playerTank = player != null ? player.GetComponentInParent<TankController>() : null;
        }
    }
}
