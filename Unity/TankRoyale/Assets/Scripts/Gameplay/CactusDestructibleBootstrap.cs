using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class CactusDestructibleBootstrap : MonoBehaviour
    {
        [SerializeField] private bool enableAutoAttach = true;
        [SerializeField] private bool includeInactive = true;

        private void Start()
        {
            AttachToCactusProps();
        }

        private void AttachToCactusProps()
        {
            if (!enableAutoAttach)
            {
                return;
            }

            Transform[] all = FindObjectsByType<Transform>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null)
                {
                    continue;
                }

                string n = t.name;
                if (string.IsNullOrEmpty(n) || !n.ToLowerInvariant().Contains("cactus"))
                {
                    continue;
                }

                if (t.GetComponentInParent<TargetCactus>() != null)
                {
                    continue;
                }

                Transform root = t;
                if (t.parent != null && t.parent.name.ToLowerInvariant().Contains("cactus"))
                {
                    root = t.parent;
                }

                if (root.GetComponent<DestructibleProp>() == null)
                {
                    root.gameObject.AddComponent<DestructibleProp>();
                }
            }
        }
    }
}
