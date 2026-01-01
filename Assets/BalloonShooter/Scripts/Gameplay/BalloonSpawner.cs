using System.Collections.Generic;
using UnityEngine;

namespace BalloonShooter
{
    public sealed class BalloonSpawner : MonoBehaviour
    {
        private static PhysicMaterial _balloonPhysicMaterial;

        [SerializeField] private GameConfig config;
        [SerializeField] private Camera spawnCamera;
        [SerializeField] private Transform balloonParent;

        private GameManager _manager;
        private readonly List<Balloon> _alive = new List<Balloon>(128);

        private float _spawnBudget;
        private float _elapsed;
        private bool _spawningEnabled = true;

        public void SetConfig(GameConfig newConfig) => config = newConfig;
        public void SetManager(GameManager manager) => _manager = manager;

        private void Awake()
        {
            if (spawnCamera == null) spawnCamera = Camera.main;
            if (balloonParent == null) balloonParent = transform;
        }

        private void Update()
        {
            if (!_spawningEnabled || _manager == null || !_manager.IsRunning || _manager.IsPaused || config == null) return;
            if (_alive.Count >= config.maxAliveBalloons) return;

            _elapsed += Time.deltaTime;
            float normalized = config.gameDurationSeconds <= 0f ? 0f : Mathf.Clamp01(_elapsed / config.gameDurationSeconds);
            float spawnRate = Mathf.Max(0f, config.spawnRateOverTime.Evaluate(normalized));
            _spawnBudget += spawnRate * Time.deltaTime;

            int safety = 0;
            while (_spawnBudget >= 1f && _alive.Count < config.maxAliveBalloons && safety++ < 10)
            {
                _spawnBudget -= 1f;
                SpawnBalloonInternal(isChild: false, cause: BalloonPopCause.DirectHit, pointsMultiplier: 1f, forcedType: null, forcedPosition: null, forcedRadius: null);
            }
        }

        public void ResetRun()
        {
            _elapsed = 0f;
            _spawnBudget = 0f;
            _spawningEnabled = true;

            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (_alive[i] != null) Destroy(_alive[i].gameObject);
            }
            _alive.Clear();
        }

        public void StopSpawning() => _spawningEnabled = false;

        public void NotifyBalloonDestroyed(Balloon balloon)
        {
            if (balloon == null) return;
            _alive.Remove(balloon);
        }

        public Balloon SpawnChildBalloon(Vector3 position, float radius, Vector3 initialVelocity, BalloonType forcedType, float pointsMultiplier)
        {
            return SpawnBalloonInternal(
                isChild: true,
                cause: BalloonPopCause.SplitterChild,
                pointsMultiplier: pointsMultiplier,
                forcedType: forcedType,
                forcedPosition: position,
                forcedRadius: radius,
                forcedVelocity: initialVelocity
            );
        }

        private Balloon SpawnBalloonInternal(
            bool isChild,
            BalloonPopCause cause,
            float pointsMultiplier,
            BalloonType forcedType,
            Vector3? forcedPosition,
            float? forcedRadius,
            Vector3? forcedVelocity = null
        )
        {
            if (config == null || spawnCamera == null) return null;

            BalloonType type = forcedType != null ? forcedType : PickBalloonType();
            if (type == null) return null;

            float radius = forcedRadius ?? Random.Range(type.radiusRange.x, type.radiusRange.y);
            Vector3 position = forcedPosition ?? ComputeSpawnPosition();
            position.z = 0f;
            Vector3 velocity = forcedVelocity ?? ComputeInitialVelocity(type);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"{type.displayName} ({type.special})";
            go.transform.SetParent(balloonParent, worldPositionStays: true);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (radius * 2f);

            var sphere = go.GetComponent<SphereCollider>();
            if (sphere != null) sphere.isTrigger = false;
            if (sphere != null) sphere.material = GetBalloonPhysicsMaterial();

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = Mathf.Max(0.05f, radius * 0.75f);
            rb.drag = 0.15f;
            rb.angularDrag = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            var balloon = go.AddComponent<Balloon>();
            balloon.Initialize(_manager, this, type, radius, velocity, spawnCamera.transform.position, isChild, cause, pointsMultiplier);
            _alive.Add(balloon);

            ApplyMaterial(go, type);
            ApplyColor(go, type);

            return balloon;
        }

        private static PhysicMaterial GetBalloonPhysicsMaterial()
        {
            if (_balloonPhysicMaterial != null) return _balloonPhysicMaterial;

            _balloonPhysicMaterial = new PhysicMaterial("Balloon_Bouncy")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0.9f,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Maximum,
            };
            return _balloonPhysicMaterial;
        }

        private Vector3 ComputeSpawnPosition()
        {
            const float spawnPlaneZ = 0f;
            float padding = 0.5f;

            Transform cam = spawnCamera.transform;
            float distance = Mathf.Max(0.1f, Mathf.Abs(spawnPlaneZ - cam.position.z));

            float halfHeight = Mathf.Tan(spawnCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
            float halfWidth = halfHeight * spawnCamera.aspect;

            float maxX = Mathf.Max(0.01f, Mathf.Min(config.spawnHorizontalHalfWidth, halfWidth - padding));
            float maxYOffset = Mathf.Max(0.01f, halfHeight - padding);

            float x = Random.Range(-maxX, maxX);
            float yOffset = Mathf.Clamp(Random.Range(config.spawnHeightRange.x, config.spawnHeightRange.y), -maxYOffset, maxYOffset);

            Vector3 pos = cam.position + cam.right * x + cam.up * yOffset;
            pos.z = spawnPlaneZ;
            return pos;
        }

        private Vector3 ComputeInitialVelocity(BalloonType type)
        {
            // Game is configured for "balloons fall down" instead of "rise up".
            float down = Random.Range(type.ascentSpeedRange.x, type.ascentSpeedRange.y);
            Vector2 lateral2 = Random.insideUnitCircle.normalized * Random.Range(type.lateralSpeedRange.x, type.lateralSpeedRange.y);
            return new Vector3(lateral2.x, -down, 0f);
        }

        private BalloonType PickBalloonType()
        {
            if (config == null || config.balloonTypes == null || config.balloonTypes.Count == 0) return null;

            float normalized = config.gameDurationSeconds <= 0f ? 0f : Mathf.Clamp01(_elapsed / config.gameDurationSeconds);
            float lateGameBoost = Mathf.Lerp(1f, 2f, normalized);

            float total = 0f;
            for (int i = 0; i < config.balloonTypes.Count; i++)
            {
                BalloonType type = config.balloonTypes[i];
                if (type == null) continue;

                float w = Mathf.Max(0f, type.rarityWeight);
                if (type.special != BalloonSpecial.Normal) w *= lateGameBoost;
                total += w;
            }

            if (total <= 0f) return config.balloonTypes[0];

            float r = Random.value * total;
            for (int i = 0; i < config.balloonTypes.Count; i++)
            {
                BalloonType type = config.balloonTypes[i];
                if (type == null) continue;

                float w = Mathf.Max(0f, type.rarityWeight);
                if (type.special != BalloonSpecial.Normal) w *= lateGameBoost;
                r -= w;
                if (r <= 0f) return type;
            }

            return config.balloonTypes[config.balloonTypes.Count - 1];
        }

        private static void ApplyMaterial(GameObject go, BalloonType type)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            if (type.material != null) renderer.sharedMaterial = type.material;
        }

        private static void ApplyColor(GameObject go, BalloonType type)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", type.colorGradient.Evaluate(Random.value));
            renderer.SetPropertyBlock(block);
        }
    }
}
