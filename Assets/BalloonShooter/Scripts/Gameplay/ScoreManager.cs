using System;
using UnityEngine;

namespace BalloonShooter
{
    public sealed class ScoreManager : MonoBehaviour
    {
        private const string HighScoreKey = "BalloonShooter.HighScore";

        [SerializeField] private GameConfig config;

        public event Action OnScoreChanged;
        public event Action OnStatsChanged;
        public event Action OnHighScoreChanged;

        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public int BestCombo { get; private set; }
        public int ComboCount { get; private set; }

        private float _lastHitTime = -999f;

        public float Accuracy01 => Shots <= 0 ? 0f : (float)Hits / Shots;

        private void Awake()
        {
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        public float ComboMultiplier
        {
            get
            {
                if (config == null) return 1f;
                float multiplier = 1f + ComboCount * config.comboMultiplierStep;
                return Mathf.Clamp(multiplier, 1f, config.comboMultiplierMax);
            }
        }

        public void SetConfig(GameConfig newConfig) => config = newConfig;

        public void ResetRun()
        {
            Score = 0;
            Shots = 0;
            Hits = 0;
            BestCombo = 0;
            ComboCount = 0;
            _lastHitTime = -999f;
            OnScoreChanged?.Invoke();
            OnStatsChanged?.Invoke();
        }

        public void ResetHighScore()
        {
            HighScore = 0;
            PlayerPrefs.SetInt(HighScoreKey, 0);
            PlayerPrefs.Save();
            OnHighScoreChanged?.Invoke();
        }

        public void RegisterShot(bool hit)
        {
            Shots++;
            if (!hit) RegisterMiss();
        }

        public void RegisterPop(BalloonPopResult pop)
        {
            if (!pop.popped || pop.type == null || config == null) return;

            float sizeFactor = ComputeSizeFactor(pop.radius, pop.type.radiusRange);
            float distanceFactor = ComputeDistanceFactor(pop.distanceFromCamera, config.spawnDistanceRange);
            float specialFactor = ComputeSpecialFactor(pop.type.special, pop.cause);

            float rawPoints = pop.type.basePoints * sizeFactor * distanceFactor * specialFactor * pop.pointsMultiplier;
            int points = Mathf.RoundToInt(rawPoints * ComboMultiplier);

            Score = Mathf.Max(0, Score + points);
            OnScoreChanged?.Invoke();
            TryUpdateHighScore(Score);
        }

        public void RegisterDirectHit(BalloonPopResult pop)
        {
            Hits++;

            float now = Time.time;
            if (config != null && now - _lastHitTime <= config.comboWindowSeconds)
                ComboCount++;
            else
                ComboCount = 1;

            _lastHitTime = now;
            BestCombo = Mathf.Max(BestCombo, ComboCount);
            OnStatsChanged?.Invoke();
        }

        public void RegisterMiss()
        {
            if (config == null) return;

            if (config.missComboPenalty >= ComboCount)
                ComboCount = 0;
            else
                ComboCount = Mathf.Max(0, ComboCount - config.missComboPenalty);

            OnStatsChanged?.Invoke();
        }

        public void RegisterEscape()
        {
            RegisterMiss();
        }

        private void TryUpdateHighScore(int newScore)
        {
            if (newScore <= HighScore) return;
            HighScore = newScore;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
            OnHighScoreChanged?.Invoke();
        }

        private static float ComputeSizeFactor(float radius, Vector2 radiusRange)
        {
            float t = Mathf.InverseLerp(radiusRange.y, radiusRange.x, radius);
            return Mathf.Lerp(1.0f, 2.2f, t);
        }

        private static float ComputeDistanceFactor(float distance, Vector2 distanceRange)
        {
            float t = Mathf.InverseLerp(distanceRange.x, distanceRange.y, distance);
            return Mathf.Lerp(1.0f, 1.8f, t);
        }

        private static float ComputeSpecialFactor(BalloonSpecial special, BalloonPopCause cause)
        {
            if (cause == BalloonPopCause.BombChain) return 0.9f;

            return special switch
            {
                BalloonSpecial.Metallic => 1.25f,
                BalloonSpecial.Glass => 1.35f,
                BalloonSpecial.Bomb => 1.05f,
                BalloonSpecial.Splitter => 1.1f,
                BalloonSpecial.Golden => 2.0f,
                _ => 1.0f,
            };
        }
    }
}
