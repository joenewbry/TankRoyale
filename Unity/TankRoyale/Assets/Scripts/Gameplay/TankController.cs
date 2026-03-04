using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// 3D top-down tank controller with independent body and turret rotation.
    /// Movement occurs on XZ plane (Y up).
    /// </summary>
    [DisallowMultipleComponent]
    public class TankController : MonoBehaviour
    {
        private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        [Header("Identity")]
        [SerializeField] private string playerId;

        [Header("References")]
        [SerializeField] private Transform tankBody;
        [SerializeField] private Transform turret;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Camera aimCamera;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private float turretRotationSpeed = 540f;

        [Header("Health")]
        [SerializeField] private int maxHealth = 3;

        // Requires Rigidbody with: Freeze Rotation X, Y, Z; Use Gravity unchecked; Collision Detection: Continuous
        private Rigidbody _rigidbody;
        private Camera _cachedCamera;
        private Vector2 _moveInput;
        private WeaponController _weaponController;
        private int currentHealth;

        public Transform FirePoint => firePoint;
        public string PlayerId => string.IsNullOrWhiteSpace(playerId) ? gameObject.name : playerId;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
                _rigidbody.useGravity = false;
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }
            _weaponController = GetComponent<WeaponController>();
            _cachedCamera = aimCamera != null ? aimCamera : Camera.main;

            currentHealth = Mathf.Max(1, maxHealth);

            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = gameObject.name;
            }

            if (tankBody == null)
            {
                Transform foundBody = transform.Find("TankBody");
                tankBody = foundBody != null ? foundBody : transform;
            }

            if (turret == null)
            {
                Transform foundTurret = transform.Find("Turret");
                turret = foundTurret != null ? foundTurret : transform;
            }

            if (firePoint == null && turret != null)
            {
                Transform foundFirePoint = turret.Find("FirePoint");
                if (foundFirePoint != null)
                {
                    firePoint = foundFirePoint;
                }
            }
        }

        private void Update()
        {
            ReadMovementInput();
            RotateTurretToMouse();
            HandleFireInput();
        }

        private void FixedUpdate()
        {
            MoveTank();
            RotateBodyToMovement();
        }

        public void TakeDamage(int amount)
        {
            if (!gameObject.activeInHierarchy || amount <= 0) return;
            currentHealth = Mathf.Max(0, currentHealth - amount);
            Debug.Log($"[{name}] took {amount} damage — health {currentHealth}/{maxHealth}");
            if (currentHealth <= 0)
            {
                Debug.Log($"[{name}] destroyed");
                gameObject.SetActive(false);
            }
        }

        public void Heal(int amount)
        {
            if (!gameObject.activeInHierarchy || amount <= 0) return;
            int prev = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Debug.Log($"[{name}] healed {currentHealth - prev} HP — health {currentHealth}/{maxHealth}");
        }

        private void HandleFireInput()
        {
            if (_weaponController == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                _weaponController.Fire();
            }
        }

        private void ReadMovementInput()
        {
            _moveInput.x = Input.GetAxisRaw("Horizontal");
            _moveInput.y = Input.GetAxisRaw("Vertical");

            if (_moveInput.sqrMagnitude > 1f)
            {
                _moveInput.Normalize();
            }
        }

        private void MoveTank()
        {
            Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
            Vector3 targetPosition = _rigidbody.position + moveDirection * (moveSpeed * Time.fixedDeltaTime);
            _rigidbody.MovePosition(targetPosition);
        }

        private void RotateBodyToMovement()
        {
            Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            tankBody.rotation = Quaternion.RotateTowards(
                tankBody.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);
        }

        private void RotateTurretToMouse()
        {
            if (_cachedCamera == null || turret == null)
            {
                return;
            }

            Ray mouseRay = _cachedCamera.ScreenPointToRay(Input.mousePosition);
            if (!GroundPlane.Raycast(mouseRay, out float hitDistance))
            {
                return;
            }

            Vector3 hitPoint = mouseRay.GetPoint(hitDistance);
            Vector3 aimDirection = hitPoint - turret.position;
            aimDirection.y = 0f;

            if (aimDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            turret.rotation = Quaternion.RotateTowards(
                turret.rotation,
                targetRotation,
                turretRotationSpeed * Time.deltaTime);
        }
    }
}
