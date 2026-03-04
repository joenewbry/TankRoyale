using System;
using System.Collections.Generic;
using UnityEngine;

namespace TankRoyale.Menu
{
    /// <summary>
    /// Minimal key rebinding shell based on legacy Input + PlayerPrefs.
    /// </summary>
    public class InputRebindController : MonoBehaviour
    {
        [Serializable]
        public class BindingSlot
        {
            public string actionId;
            public KeyCode defaultKey = KeyCode.None;
            [HideInInspector] public KeyCode currentKey = KeyCode.None;
        }

        [SerializeField] private List<BindingSlot> bindings = new List<BindingSlot>();

        private bool _isListeningForKey;
        private BindingSlot _pendingBinding;

        private void Awake()
        {
            LoadBindings();
        }

        private void Update()
        {
            if (!_isListeningForKey || _pendingBinding == null)
            {
                return;
            }

            if (!Input.anyKeyDown)
            {
                return;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(keyCode))
                {
                    continue;
                }

                ApplyBinding(_pendingBinding, keyCode);
                break;
            }
        }

        public void BeginRebind(string actionId)
        {
            if (_isListeningForKey)
            {
                Debug.LogWarning("[InputRebindController] Already waiting for key input.");
                return;
            }

            BindingSlot slot = FindSlot(actionId);
            if (slot == null)
            {
                Debug.LogWarning($"[InputRebindController] Unknown action '{actionId}'.");
                return;
            }

            _pendingBinding = slot;
            _isListeningForKey = true;

            // TODO(MENU-101): Show 'press any key' prompt + cancel flow in UI.
        }

        public void CancelRebind()
        {
            _pendingBinding = null;
            _isListeningForKey = false;
        }

        public KeyCode GetBinding(string actionId)
        {
            BindingSlot slot = FindSlot(actionId);
            return slot != null ? slot.currentKey : KeyCode.None;
        }

        public string GetBindingDisplay(string actionId)
        {
            return GetBinding(actionId).ToString();
        }

        public void ResetAllBindings()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                BindingSlot slot = bindings[i];
                slot.currentKey = slot.defaultKey;
                SaveBinding(slot);
            }

            PlayerPrefs.Save();

            // TODO(MENU-101): Broadcast binding-reset event for UI refresh.
        }

        private void ApplyBinding(BindingSlot slot, KeyCode newKey)
        {
            slot.currentKey = newKey;
            SaveBinding(slot);

            _pendingBinding = null;
            _isListeningForKey = false;

            PlayerPrefs.Save();
        }

        private void LoadBindings()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                BindingSlot slot = bindings[i];
                string prefKey = ToPrefKey(slot.actionId);
                string storedValue = PlayerPrefs.GetString(prefKey, slot.defaultKey.ToString());

                KeyCode parsed;
                if (Enum.TryParse(storedValue, true, out parsed))
                {
                    slot.currentKey = parsed;
                }
                else
                {
                    slot.currentKey = slot.defaultKey;
                }
            }

            // TODO(MENU-101): Integrate with Unity Input System actions.
        }

        private void SaveBinding(BindingSlot slot)
        {
            PlayerPrefs.SetString(ToPrefKey(slot.actionId), slot.currentKey.ToString());
        }

        private BindingSlot FindSlot(string actionId)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (string.Equals(bindings[i].actionId, actionId, StringComparison.Ordinal))
                {
                    return bindings[i];
                }
            }

            return null;
        }

        private static string ToPrefKey(string actionId)
        {
            return $"input.binding.{actionId}";
        }
    }
}
