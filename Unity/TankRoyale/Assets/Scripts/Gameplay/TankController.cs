using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// 3D top-down tank controller with independent body and turret rotation.
    /// Movement occurs on XZ plane (Y up).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class TankController : MonoBehaviour
    {
        private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        [Header("References")]
        [SerializeField] private Transform tankBody;
        [SerializeField] private Transform turret;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Camera aimCamera;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private float turretRotationSpeed = 540f;

        private Rigidbody _rigidbody;
        private Camera _cachedCamera;
        private Vector2 _moveInput;

        public Transform FirePoint => firePoint;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _cachedCamera = aimCamera != null ? aimCamera : Camera.main;

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
        }

        private void FixedUpdate()
        {
            MoveTank();
            RotateBodyToMovement();
        }

        /// <summary>
        /// Intended to be called by WeaponController.
        /// </summary>
        public GameObject FireProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[TankController] Cannot fire: projectilePrefab is not assigned.");
                return null;
            }

            if (firePoint == null)
            {
                Debug.LogWarning("[TankController] Cannot fire: firePoint is not assigned.");
                return null;
            }

            return Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
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
