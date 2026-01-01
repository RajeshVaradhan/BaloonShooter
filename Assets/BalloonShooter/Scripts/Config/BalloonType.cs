using UnityEngine;

namespace BalloonShooter
{
    [CreateAssetMenu(menuName = "Balloon Shooter/Balloon Type", fileName = "BalloonType_")]
    public sealed class BalloonType : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Balloon";
        public BalloonSpecial special = BalloonSpecial.Normal;

        [Header("Look")]
        public Material material;
        public Gradient colorGradient = new Gradient();

        [Header("Gameplay")]
        [Min(0f)] public float basePoints = 50f;
        [Min(1)] public int hitPoints = 1;
        [Min(0.01f)] public float rarityWeight = 1f;

        [Header("Spawn Ranges")]
        public Vector2 radiusRange = new Vector2(0.25f, 0.6f);
        public Vector2 ascentSpeedRange = new Vector2(0.6f, 1.4f);
        public Vector2 lateralSpeedRange = new Vector2(0.0f, 0.7f);

        [Header("Special")]
        [Min(0f)] public float bombRadius = 2.5f;
        [Min(0f)] public float chainPopPointMultiplier = 0.5f;
        [Min(0)] public int splitterChildCount = 2;
        [Min(0f)] public float goldenTimeBonusSeconds = 3f;
    }
}

