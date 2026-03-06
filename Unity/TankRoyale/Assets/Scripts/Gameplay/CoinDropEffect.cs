using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Lightweight coin popup used for destroyable world props.
    /// </summary>
    [DisallowMultipleComponent]
    public class CoinDropEffect : MonoBehaviour
    {
        private const string CoinPrefabPath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Collectible/Collectible_Coin_01.prefab";

        [SerializeField] private float lifetime = 1.8f;
        [SerializeField] private float spinSpeed = 420f;
        [SerializeField] private float bobAmplitude = 0.14f;
        [SerializeField] private float bobSpeed = 5.5f;

        private float _spawnTime;
        private Vector3 _startPosition;
        private Renderer[] _renderers = new Renderer[0];

        public static void SpawnAt(Vector3 position)
        {
            GameObject root = TryInstantiateCoinPrefab();
            if (root == null)
            {
                root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                root.transform.localScale = new Vector3(0.25f, 0.03f, 0.25f);
                Renderer fallbackRenderer = root.GetComponent<Renderer>();
                if (fallbackRenderer != null)
                {
                    Material m = new Material(fallbackRenderer.sharedMaterial);
                    m.color = new Color(1f, 0.82f, 0.15f, 1f);
                    fallbackRenderer.material = m;
                }

                Collider fallbackCollider = root.GetComponent<Collider>();
                if (fallbackCollider != null)
                {
                    Destroy(fallbackCollider);
                }
            }

            root.name = "CoinDrop";
            root.transform.position = position + Vector3.up * 0.35f;
            root.AddComponent<CoinDropEffect>();
        }

        private static GameObject TryInstantiateCoinPrefab()
        {
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
            if (prefab != null)
            {
                return Instantiate(prefab);
            }
#endif
            return null;
        }

        private void Awake()
        {
            _spawnTime = Time.time;
            _startPosition = transform.position;
            _renderers = GetComponentsInChildren<Renderer>(true);

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        private void Update()
        {
            float elapsed = Time.time - _spawnTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            float bob = Mathf.Sin(elapsed * bobSpeed) * bobAmplitude;
            transform.position = _startPosition + Vector3.up * bob;

            float fade = 1f - Mathf.Clamp01(elapsed / lifetime);
            ApplyAlpha(fade);
        }

        private void ApplyAlpha(float alpha)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null || r.sharedMaterial == null)
                {
                    continue;
                }

                Material m = r.material;
                if (!m.HasProperty("_Color"))
                {
                    continue;
                }

                Color c = m.color;
                c.a = alpha;
                m.color = c;

                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
            }
        }
    }
}
