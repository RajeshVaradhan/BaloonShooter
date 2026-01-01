using UnityEngine;

namespace BalloonShooter
{
    public sealed class Balloon : MonoBehaviour
    {
        public BalloonType Type { get; private set; }
        public float Radius { get; private set; }
        public int HitPointsRemaining { get; private set; }

        private GameManager _manager;
        private BalloonSpawner _spawner;
        private Vector3 _velocity;
        private Rigidbody _rb;
        private Vector3 _cameraAtSpawn;
        private bool _isChild;
        private BalloonPopCause _spawnCause;
        private float _pointsMultiplier;
        private bool _popped;
        private Vector3 _baseScale;
        private float _despawnY;

        private float _dashEndTime = -999f;
        private float _nextDashTime = -999f;
        private Vector3 _dashDirection = Vector3.zero;

        public void Initialize(
            GameManager manager,
            BalloonSpawner spawner,
            BalloonType type,
            float radius,
            Vector3 initialVelocity,
            Vector3 cameraPositionAtSpawn,
            bool isChild,
            BalloonPopCause spawnCause,
            float pointsMultiplier
        )
        {
            _manager = manager;
            _spawner = spawner;
            Type = type;
            Radius = radius;
            _velocity = initialVelocity;
            _cameraAtSpawn = cameraPositionAtSpawn;
            _isChild = isChild;
            _spawnCause = spawnCause;
            _pointsMultiplier = Mathf.Max(0.01f, pointsMultiplier);

            HitPointsRemaining = Mathf.Max(1, type != null ? type.hitPoints : 1);
            _baseScale = transform.localScale;
            _rb = GetComponent<Rigidbody>();

            float camY = Camera.main != null ? Camera.main.transform.position.y : _cameraAtSpawn.y;
            _despawnY = camY - (_manager != null && _manager.Config != null ? _manager.Config.despawnHeight : 9.5f);

            ScheduleNextDash(initial: true);
        }

        private void Update()
        {
            if (_popped || _manager == null || !_manager.IsRunning || _manager.IsPaused) return;

            TryStartDash();

            Vector3 wind = _manager.GetWind3D();
            Vector3 dash = Time.time < _dashEndTime ? _dashDirection : Vector3.zero;
            Vector3 desiredVelocity = _velocity + wind + dash;

            if (_rb != null)
            {
                _rb.velocity = desiredVelocity;
                _rb.position = new Vector3(_rb.position.x, _rb.position.y, 0f);
            }
            else
            {
                transform.position += desiredVelocity * Time.deltaTime;
                Vector3 pos = transform.position;
                pos.z = 0f;
                transform.position = pos;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 10f * Time.deltaTime);

            if (transform.position.y <= _despawnY)
                Despawn();
        }

        public bool TryHit(out BalloonPopResult popResult)
        {
            popResult = default;
            if (_popped || Type == null) return false;

            HitPointsRemaining = Mathf.Max(0, HitPointsRemaining - 1);
            if (HitPointsRemaining > 0)
            {
                Vector3 camPos = Camera.main != null ? Camera.main.transform.position : _cameraAtSpawn;
                float distance = Vector3.Distance(camPos, transform.position);
                popResult = new BalloonPopResult(
                    popped: false,
                    type: Type,
                    radius: Radius,
                    distanceFromCamera: distance,
                    cause: BalloonPopCause.DirectHit,
                    pointsMultiplier: _pointsMultiplier,
                    popPosition: transform.position
                );
                PulseFeedback();
                return true;
            }

            popResult = Pop(BalloonPopCause.DirectHit, pointsMultiplierOverride: 1f);
            return true;
        }

        public BalloonPopResult PopFromExplosion(float pointsMultiplier)
        {
            if (_popped || Type == null) return default;
            return Pop(BalloonPopCause.BombChain, pointsMultiplierOverride: pointsMultiplier);
        }

        private void Despawn()
        {
            if (_popped) return;
            _popped = true;
            _manager?.Score?.RegisterEscape();
            _spawner?.NotifyBalloonDestroyed(this);
            Destroy(gameObject);
        }

        private BalloonPopResult Pop(BalloonPopCause cause, float pointsMultiplierOverride)
        {
            if (_popped) return default;
            _popped = true;

            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : _cameraAtSpawn;
            float distance = Vector3.Distance(camPos, transform.position);

            float finalMultiplier = _pointsMultiplier * Mathf.Max(0.01f, pointsMultiplierOverride);
            var result = new BalloonPopResult(
                popped: true,
                type: Type,
                radius: Radius,
                distanceFromCamera: distance,
                cause: cause,
                pointsMultiplier: finalMultiplier,
                popPosition: transform.position
            );

            SpawnPopVfx(cause);
            ApplySpecialOnPop(cause);

            _spawner?.NotifyBalloonDestroyed(this);
            Destroy(gameObject);
            return result;
        }

        private void ApplySpecialOnPop(BalloonPopCause cause)
        {
            if (Type == null) return;

            switch (Type.special)
            {
                case BalloonSpecial.Bomb:
                    Explode();
                    break;
                case BalloonSpecial.Splitter:
                    Split();
                    break;
                case BalloonSpecial.Golden:
                    _manager?.AddTime(Type.goldenTimeBonusSeconds);
                    break;
            }
        }

        private void Explode()
        {
            if (Type == null) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, Type.bombRadius, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;
                Balloon other = hits[i].GetComponent<Balloon>();
                if (other == null) other = hits[i].GetComponentInParent<Balloon>();
                if (other == null) continue;
                if (other == this) continue;
                if (other.Type == null) continue;

                BalloonPopResult pop = other.PopFromExplosion(Type.chainPopPointMultiplier);
                if (!pop.popped) continue;

                _manager?.Score?.RegisterPop(pop);
            }
        }

        private void Split()
        {
            if (_spawner == null || Type == null) return;
            if (Type.splitterChildCount <= 0) return;

            float childRadius = Mathf.Max(0.08f, Radius * 0.65f);
            float childMultiplier = Mathf.Clamp01(0.6f * _pointsMultiplier);

            for (int i = 0; i < Type.splitterChildCount; i++)
            {
                Vector2 offset2 = Random.insideUnitCircle * (childRadius * 0.75f);
                offset2.y = Mathf.Abs(offset2.y) * 0.25f;
                Vector3 offset = new Vector3(offset2.x, offset2.y, 0f);

                Vector3 velocity = new Vector3(_velocity.x, _velocity.y, 0f);
                velocity.x += Random.Range(-0.5f, 0.5f);
                velocity.y *= Random.Range(0.85f, 1.1f);

                _spawner.SpawnChildBalloon(transform.position + offset, childRadius, velocity, Type, childMultiplier);
            }
        }

        private void PulseFeedback()
        {
            transform.localScale = _baseScale * 1.08f;
        }

        private void TryStartDash()
        {
            if (_manager == null || _manager.Config == null) return;
            if (!_manager.Config.enableBalloonDashing) return;
            if (Time.time < _nextDashTime) return;

            float range = Mathf.Max(0.01f, _manager.Config.dashTargetRange);
            if (!HasNearbyBalloon(range))
            {
                ScheduleNextDash(initial: false);
                return;
            }

            // Dash is random (so balloons don't "home in" and clump/merge).
            Vector2 dash2 = Random.insideUnitCircle;
            dash2.y *= 0.25f; // mostly sideways so falling stays readable
            if (dash2.sqrMagnitude < 0.0001f) dash2 = Vector2.right;
            Vector3 dashDir = new Vector3(dash2.x, dash2.y, 0f).normalized;
            _dashDirection = dashDir * Mathf.Max(0f, _manager.Config.dashSpeed);
            _dashEndTime = Time.time + Mathf.Max(0.01f, _manager.Config.dashDurationSeconds);
            ScheduleNextDash(initial: false);
        }

        private bool HasNearbyBalloon(float range)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, range, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;
                Balloon other = hits[i].GetComponentInParent<Balloon>();
                if (other == null || other == this) continue;
                if (other._popped) continue;
                return true;
            }

            return false;
        }

        private void ScheduleNextDash(bool initial)
        {
            if (_manager == null || _manager.Config == null)
            {
                _nextDashTime = float.PositiveInfinity;
                return;
            }

            Vector2 range = _manager.Config.dashIntervalSecondsRange;
            float min = Mathf.Max(0.05f, Mathf.Min(range.x, range.y));
            float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
            float delay = Random.Range(min, max);

            _nextDashTime = Time.time + (initial ? Random.Range(0f, delay) : delay);
        }

        private void SpawnPopVfx(BalloonPopCause cause)
        {
            var vfx = new GameObject("PopVFX");
            vfx.transform.position = transform.position;

            var ps = vfx.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(Radius * 0.12f, Radius * 0.22f);
            main.gravityModifier = 0.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Random.Range(14, 26)) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Radius * 0.3f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            Color baseColor = Type != null ? Type.colorGradient.Evaluate(Random.value) : Color.white;
            if (cause == BalloonPopCause.BombChain) baseColor = Color.Lerp(baseColor, Color.black, 0.35f);
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(baseColor, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            ps.Play();
            Destroy(vfx, 1.25f);
        }
    }
}
