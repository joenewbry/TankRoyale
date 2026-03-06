using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class TargetCactus : MonoBehaviour
    {
        [SerializeField] private float barYOffset = 2.2f;
        [SerializeField] private Vector2 barSize = new Vector2(84f, 10f);

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
            SetVisible(false);
        }

        private void Respawn()
        {
            _destroyed = false;
            _currentHits = 0;
            _maxHits = Mathf.Max(1, TargetCactusSettings.HitsToDestroy);
            _wasHit = false;
            SetVisible(true);
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
