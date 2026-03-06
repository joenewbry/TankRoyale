using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class TargetCactus : MonoBehaviour
    {
        [SerializeField] private float barYOffset = 2.2f;
        [SerializeField] private Vector2 barSize = new Vector2(84f, 10f);
        [SerializeField] private float defeatPopupHeight = 2.8f;
        [SerializeField] private float defeatPopupDuration = 1.4f;
        [SerializeField] private Color defeatPopupColor = new Color(1f, 0.35f, 0.35f, 1f);

        private int _currentHits;
        private int _maxHits = 5;
        private bool _wasHit;
        private bool _destroyed;
        private float _respawnAt;
        private Collider[] _colliders = new Collider[0];
        private Renderer[] _renderers = new Renderer[0];

        private void Awake()
        {
            _maxHits = Mathf.Max(1, TargetCactusSettings.HitsToDestroy);
            _colliders = GetComponentsInChildren<Collider>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
            SetVisible(true);
        }

        private void Update()
        {
            if (_destroyed && Time.time >= _respawnAt)
            {
                Respawn();
            }
        }

        public void TakeHit(int amount = 1)
        {
            if (_destroyed) return;

            _wasHit = true;
            _maxHits = Mathf.Max(1, TargetCactusSettings.HitsToDestroy);
            _currentHits = Mathf.Clamp(_currentHits + Mathf.Max(1, amount), 0, _maxHits);

            if (_currentHits >= _maxHits)
            {
                DestroyTarget();
            }
        }

        private void DestroyTarget()
        {
            _destroyed = true;
            _respawnAt = Time.time + Mathf.Max(0.1f, TargetCactusSettings.RespawnSeconds);
            ClearAttachedSplatters();
            SpawnDefeatPopup();
            SetVisible(false);
        }

        private void Respawn()
        {
            _destroyed = false;
            _currentHits = 0;
            _maxHits = Mathf.Max(1, TargetCactusSettings.HitsToDestroy);
            _wasHit = false;
            ClearAttachedSplatters();
            SetVisible(true);
        }

        private void SpawnDefeatPopup()
        {
            GameObject popup = new GameObject("DefeatPopup");
            popup.transform.position = transform.position + Vector3.up * defeatPopupHeight;
            TextMesh text = popup.AddComponent<TextMesh>();
            text.text = "DEFEAT";
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = 48;
            text.characterSize = 0.08f;
            text.color = defeatPopupColor;

            MeshRenderer r = popup.GetComponent<MeshRenderer>();
            if (r != null)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            StartCoroutine(AnimateDefeatPopup(popup, Mathf.Max(0.2f, defeatPopupDuration)));
        }

        private System.Collections.IEnumerator AnimateDefeatPopup(GameObject popup, float duration)
        {
            if (popup == null)
            {
                yield break;
            }

            TextMesh tm = popup.GetComponent<TextMesh>();
            Color baseColor = tm != null ? tm.color : defeatPopupColor;
            Vector3 startPos = popup.transform.position;
            Vector3 endPos = startPos + Vector3.up * 1.4f;
            Vector3 startScale = Vector3.one * 0.25f;
            Vector3 endScale = Vector3.one * 1.2f;

            float t = 0f;
            while (t < duration && popup != null)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - a, 3f);

                popup.transform.position = Vector3.Lerp(startPos, endPos, eased);
                popup.transform.localScale = Vector3.Lerp(startScale, endScale, eased);

                if (Camera.main != null)
                {
                    Vector3 lookDir = popup.transform.position - Camera.main.transform.position;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        popup.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                    }
                }

                if (tm != null)
                {
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, a);
                    tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                }

                yield return null;
            }

            if (popup != null)
            {
                Destroy(popup);
            }
        }

        private void ClearAttachedSplatters()
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == transform)
                {
                    continue;
                }

                if (t.name.StartsWith("PaintSplat"))
                {
                    Destroy(t.gameObject);
                }
            }
        }

        private void SetVisible(bool isVisible)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) _renderers[i].enabled = isVisible;
            }

            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null) _colliders[i].enabled = isVisible;
            }
        }

        private void OnGUI()
        {
            if (!_wasHit || _destroyed || Camera.main == null)
            {
                return;
            }

            Vector3 world = transform.position + Vector3.up * barYOffset;
            Vector3 screen = Camera.main.WorldToScreenPoint(world);
            if (screen.z <= 0f)
            {
                return;
            }

            float x = screen.x - barSize.x * 0.5f;
            float y = Screen.height - screen.y - barSize.y * 0.5f;
            Rect bg = new Rect(x, y, barSize.x, barSize.y);
            GUI.color = Color.black;
            GUI.DrawTexture(bg, Texture2D.whiteTexture);

            float remaining = Mathf.Clamp01(1f - (_currentHits / (float)Mathf.Max(1, _maxHits)));
            Rect fill = new Rect(x + 1f, y + 1f, (barSize.x - 2f) * remaining, barSize.y - 2f);
            GUI.color = Color.Lerp(Color.red, Color.green, remaining);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
