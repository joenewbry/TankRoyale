using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Spawns a procedural particle explosion at a world position using Unity's built-in ParticleSystem.
    /// No external assets required — all parameters are set via code.
    /// Usage: ExplosionEffect.Spawn(position);
    /// </summary>
    public static class ExplosionEffect
    {
        private static GameObject _prefab;

        /// <summary>Spawn an explosion at the given world position. Auto-destructs after 2s.</summary>
        public static void Spawn(Vector3 position, float scale = 1f)
        {
            if (_prefab == null) _prefab = BuildPrefab();

            var go = Object.Instantiate(_prefab, position, Quaternion.identity);
            go.transform.localScale = Vector3.one * scale;

            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            Object.Destroy(go, 2.5f);
        }

        private static GameObject BuildPrefab()
        {
            var root = new GameObject("ExplosionFX");
            root.SetActive(false); // stay inactive until Spawn

            // ── Main burst ─────────────────────────────────────────────────
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.5f, 0f),   // orange
                new Color(1f, 0.9f, 0.1f)  // yellow
            );
            main.gravityModifier = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            // Color over lifetime — fade out
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(new Color(0.4f, 0.2f, 0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            // ── Smoke sub-emitter ───────────────────────────────────────────
            var smokeGO = new GameObject("Smoke");
            smokeGO.transform.SetParent(root.transform, false);
            var smokePS = smokeGO.AddComponent<ParticleSystem>();
            var smokeMain = smokePS.main;
            smokeMain.duration = 0.6f;
            smokeMain.loop = false;
            smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            smokeMain.startSpeed    = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            smokeMain.startSize     = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            smokeMain.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(0.25f, 0.25f, 0.25f, 0.7f),
                new Color(0.15f, 0.15f, 0.15f, 0.5f)
            );
            smokeMain.gravityModifier = -0.05f;
            smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var smokeEmit = smokePS.emission;
            smokeEmit.rateOverTime = 0;
            smokeEmit.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 12) });

            var smokeShape = smokePS.shape;
            smokeShape.enabled = true;
            smokeShape.shapeType = ParticleSystemShapeType.Sphere;
            smokeShape.radius = 0.2f;

            var smokeColorLife = smokePS.colorOverLifetime;
            smokeColorLife.enabled = true;
            var smokeGrad = new Gradient();
            smokeGrad.SetKeys(
                new[] { new GradientColorKey(Color.gray, 0f), new GradientColorKey(Color.gray, 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            smokeColorLife.color = new ParticleSystem.MinMaxGradient(smokeGrad);

            root.SetActive(true);
            return root;
        }
    }
}
