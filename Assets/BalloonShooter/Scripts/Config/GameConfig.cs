using System.Collections.Generic;
using UnityEngine;

namespace BalloonShooter
{
    [CreateAssetMenu(menuName = "Balloon Shooter/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Round")]
        [Min(10f)] public float gameDurationSeconds = 90f;
        [Min(1)] public int maxAliveBalloons = 22;

        [Header("Spawn Volume (in front of camera)")]
        public Vector2 spawnDistanceRange = new Vector2(10f, 28f);
        public Vector2 spawnHeightRange = new Vector2(0.6f, 6.0f);
        [Min(0f)] public float spawnHorizontalHalfWidth = 8f;
        [Min(0f)] public float despawnHeight = 9.5f;

        [Header("Spawn Rate (spawns/sec over normalized time 0..1)")]
        public AnimationCurve spawnRateOverTime = new AnimationCurve(
            new Keyframe(0f, 0.9f),
            new Keyframe(0.5f, 1.6f),
            new Keyframe(1f, 2.4f)
        );

        [Header("Wind")]
        [Min(0f)] public float windChangeIntervalSeconds = 2.5f;
        [Min(0f)] public float windStrength = 0.75f;

        [Header("Combo")]
        [Min(0.1f)] public float comboWindowSeconds = 1.25f;
        [Min(0f)] public float comboMultiplierStep = 0.15f;
        [Min(1f)] public float comboMultiplierMax = 3.0f;
        [Min(0)] public int missComboPenalty = 999;

        [Header("Balloon Types")]
        public List<BalloonType> balloonTypes = new List<BalloonType>();

        [Header("Balloon Dashing (physics)")]
        public bool enableBalloonDashing = true;
        [Min(0f)] public float dashTargetRange = 4.5f;
        [Min(0f)] public float dashSpeed = 4.0f;
        [Min(0f)] public float dashDurationSeconds = 0.2f;
        public Vector2 dashIntervalSecondsRange = new Vector2(1.25f, 2.5f);
    }
}
