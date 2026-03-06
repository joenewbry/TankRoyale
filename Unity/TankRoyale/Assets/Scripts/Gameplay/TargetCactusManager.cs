using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class TargetCactusManager : MonoBehaviour
    {
        [SerializeField] private KeyCode balanceToggleKey = KeyCode.T;
        [SerializeField] private float spawnDistanceAhead = 14f;
        [SerializeField] private float spawnHeightProbe = 8f;
        [SerializeField] private Rect panelRect = new Rect(18f, 380f, 320f, 170f);

        private TargetCactus _cactus;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureManager()
        {
            if (Object.FindFirstObjectByType<TargetCactusManager>() != null)
            {
                return;
            }

            GameObject go = new GameObject("TargetCactusManager");
            go.AddComponent<TargetCactusManager>();
        }

        private void Start()
        {
            EnsureTargetCactus();
        }

        private void Update()
        {
            if (Input.GetKeyDown(balanceToggleKey))
            {
                TargetCactusSettings.BalanceModeEnabled = !TargetCactusSettings.BalanceModeEnabled;
            }

            if (_cactus == null)
            {
                EnsureTargetCactus();
            }
        }

        private void EnsureTargetCactus()
        {
            if (_cactus != null)
            {
                return;
            }

            Transform player = ResolvePlayerTransform();
            if (player == null)
            {
                return;
            }

            GameObject cactus = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cactus.name = "TargetCactus";
            cactus.tag = "Block";
            cactus.transform.position = GetSpawnPosition(player);
            cactus.transform.localScale = new Vector3(1.1f, 2.3f, 1.1f);

            Renderer r = cactus.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                Material m = new Material(r.sharedMaterial);
                m.color = new Color(0.24f, 0.64f, 0.24f, 1f);
                r.material = m;
            }

            _cactus = cactus.AddComponent<TargetCactus>();
        }

        private static Transform ResolvePlayerTransform()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.transform : null;
        }

        private Vector3 GetSpawnPosition(Transform player)
        {
            Vector3 basePos = player.position + player.forward * spawnDistanceAhead;
            Vector3 rayOrigin = basePos + Vector3.up * spawnHeightProbe;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, spawnHeightProbe * 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * 1.15f;
            }

            return basePos + Vector3.up * 1.15f;
        }

        private void OnGUI()
        {
            if (!TargetCactusSettings.BalanceModeEnabled)
            {
                return;
            }

            panelRect = GUILayout.Window(63542, panelRect, DrawPanel, "Target Balance (T)");
        }

        private static void DrawPanel(int id)
        {
            GUILayout.Label("Target Cactus Tuning");

            GUILayout.Label("Hits To Destroy: " + TargetCactusSettings.HitsToDestroy);
            int hits = Mathf.RoundToInt(GUILayout.HorizontalSlider(TargetCactusSettings.HitsToDestroy, 1f, 20f));
            TargetCactusSettings.HitsToDestroy = Mathf.Clamp(hits, 1, 20);

            GUILayout.Label("Respawn Seconds: " + TargetCactusSettings.RespawnSeconds.ToString("0.0"));
            float respawn = GUILayout.HorizontalSlider(TargetCactusSettings.RespawnSeconds, 0.5f, 10f);
            TargetCactusSettings.RespawnSeconds = Mathf.Clamp(respawn, 0.5f, 10f);

            GUI.DragWindow(new Rect(0f, 0f, 5000f, 22f));
        }
    }
}
