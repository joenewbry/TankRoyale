using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        private const int TopDownMode = 0;
        private const int InTankMode = 1;

        [Header("References")]
        [SerializeField] private Transform playerTank;
        [SerializeField] private Transform playerTurret;

        [Header("Top Down")]
        [SerializeField] private float topDownHeight = 20f;
        [SerializeField] private float orthoSize = 12f;

        [Header("Follow")]
        [SerializeField] private float followSpeed = 8f;
        [SerializeField] private KeyCode switchKey = KeyCode.Tab;

        private Camera _camera;
        private int _mode = TopDownMode;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            ResolveReferences();
            ApplyModeSettings();
            SnapToCurrentModeTarget();
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(switchKey))
            {
                _mode = _mode == TopDownMode ? InTankMode : TopDownMode;
                ApplyModeSettings();
            }

            if (playerTank == null || (_mode == InTankMode && playerTurret == null))
            {
                ResolveReferences();
            }

            Vector3 targetPosition = GetTargetPosition();
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

            if (_mode == TopDownMode)
            {
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else if (playerTurret != null)
            {
                transform.LookAt(playerTurret.position);
            }
        }

        private void ResolveReferences()
        {
            if (playerTank == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerTank = playerObject.transform;
                }
            }

            if (playerTurret == null && playerTank != null)
            {
                Transform foundTurret = playerTank.Find("Turret");
                if (foundTurret == null)
                {
                    foundTurret = playerTank.Find("turret");
                }

                if (foundTurret == null)
                {
                    Transform[] children = playerTank.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < children.Length; i++)
                    {
                        string childName = children[i].name;
                        if (childName == "Turret" || childName == "turret")
                        {
                            foundTurret = children[i];
                            break;
                        }
                    }
                }

                playerTurret = foundTurret;
            }
        }

        private void ApplyModeSettings()
        {
            if (_camera == null)
            {
                return;
            }

            if (_mode == TopDownMode)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = orthoSize;
            }
            else
            {
                _camera.orthographic = false;
                _camera.fieldOfView = 60f;
            }
        }

        private Vector3 GetTargetPosition()
        {
            if (_mode == TopDownMode)
            {
                if (playerTank != null)
                {
                    return playerTank.position + new Vector3(0f, topDownHeight, 0f);
                }

                return transform.position;
            }

            if (playerTurret != null)
            {
                return playerTurret.position + (playerTurret.forward * -4f) + Vector3.up * 2.5f;
            }

            return transform.position;
        }

        private void SnapToCurrentModeTarget()
        {
            transform.position = GetTargetPosition();

            if (_mode == TopDownMode)
            {
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else if (playerTurret != null)
            {
                transform.LookAt(playerTurret.position);
            }
        }
    }
}
