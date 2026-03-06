using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class AnimationShowcaseManager : MonoBehaviour
    {
        [SerializeField] private bool spawnShowcase = true;
        [SerializeField] private float rowSpacing = 3.2f;
        [SerializeField] private Vector3 rowOffsetFromCactus = new Vector3(5f, 0f, 0f);

        private static readonly string[] ClipPaths =
        {
            "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Animation/Props_Badge_Defeat_01_Defeat.anim",
            "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Animation/Props_Tank_Spawn_01_Spawn.anim",
            "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Animation/Props_Army_Radar_02_Animated_Scanning.anim",
            "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Animation/Props_Badge_Victory_01_Victory.anim",
            "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Animation/Props_Pumpjack_01_Pumping.anim"
        };

        private static readonly string[] ModelPaths =
        {
            "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Source File/Prop_Badge_Defeat_01.fbx",
            "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Source File/Prop_Tank_Spawn_01.fbx",
            "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Source File/Prop_Army_Radar_02_Animated.fbx",
            "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Source File/Prop_Badge_Victory_01.fbx",
            "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Source File/Prop_Pumpjack_01.fbx"
        };

        private void Start()
        {
            if (!spawnShowcase)
            {
                return;
            }

            if (GameObject.Find("AnimationShowcaseRow") != null)
            {
                return;
            }

            SpawnShowcaseRow();
        }

        private void SpawnShowcaseRow()
        {
            Transform cactus = ResolveCactus();
            Vector3 basePos = cactus != null ? cactus.position + rowOffsetFromCactus : Vector3.zero;
            Vector3 right = Vector3.right;

            GameObject root = new GameObject("AnimationShowcaseRow");
            root.transform.position = basePos;

            for (int i = 0; i < ClipPaths.Length; i++)
            {
                Vector3 slot = basePos + right * (i * rowSpacing);
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "AnimTile_" + i;
                tile.transform.SetParent(root.transform, true);
                tile.transform.position = slot;
                tile.transform.localScale = new Vector3(2.4f, 0.25f, 2.4f);
                tile.tag = "Block";

                Renderer tileRenderer = tile.GetComponent<Renderer>();
                if (tileRenderer != null && tileRenderer.sharedMaterial != null)
                {
                    Material m = new Material(tileRenderer.sharedMaterial);
                    m.color = new Color(0.65f, 0.6f, 0.52f, 1f);
                    tileRenderer.material = m;
                }

                SpawnAnimatedProp(root.transform, slot + Vector3.up * 0.15f, ClipPaths[i], ModelPaths[i], i);
            }
        }

        private void SpawnAnimatedProp(Transform parent, Vector3 position, string clipPath, string modelPath, int index)
        {
            GameObject holder = new GameObject("AnimShowcase_" + index);
            holder.transform.SetParent(parent, true);
            holder.transform.position = position;

#if UNITY_EDITOR
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (clip != null && modelPrefab != null)
            {
                GameObject instance = Instantiate(modelPrefab, holder.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                ShowcaseClipPlayer clipPlayer = instance.GetComponent<ShowcaseClipPlayer>();
                if (clipPlayer == null)
                {
                    clipPlayer = instance.AddComponent<ShowcaseClipPlayer>();
                }

                clipPlayer.Play(clip);
                return;
            }
#endif

            // Fallback if assets/clips are unavailable (build runtime).
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fallback.transform.SetParent(holder.transform, false);
            fallback.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
            fallback.name = "AnimFallback";
            holder.AddComponent<ShowcaseFallbackMotion>();
        }

        private static Transform ResolveCactus()
        {
            TargetCactus cactus = Object.FindFirstObjectByType<TargetCactus>();
            return cactus != null ? cactus.transform : null;
        }

        private class ShowcaseFallbackMotion : MonoBehaviour
        {
            private Vector3 _start;

            private void Start()
            {
                _start = transform.position;
            }

            private void Update()
            {
                float t = Time.time;
                transform.position = _start + Vector3.up * (Mathf.Sin(t * 2f) * 0.2f);
                transform.Rotate(0f, 65f * Time.deltaTime, 0f, Space.World);
            }
        }

        private sealed class ShowcaseClipPlayer : MonoBehaviour
        {
            private PlayableGraph _graph;

            public void Play(AnimationClip clip)
            {
                if (clip == null)
                {
                    return;
                }

                StopGraph();

                Animator animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = gameObject.AddComponent<Animator>();
                }

                _graph = PlayableGraph.Create("AnimationShowcaseGraph");
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
                AnimationClipPlayable playable = AnimationClipPlayable.Create(_graph, clip);
                playable.SetApplyFootIK(false);
                playable.SetDuration(double.IsFinite(clip.length) && clip.length > 0.01f ? clip.length : 1d);
                output.SetSourcePlayable(playable);
                _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                _graph.Play();
            }

            private void OnDisable()
            {
                StopGraph();
            }

            private void OnDestroy()
            {
                StopGraph();
            }

            private void StopGraph()
            {
                if (_graph.IsValid())
                {
                    _graph.Destroy();
                }
            }
        }
    }
}
