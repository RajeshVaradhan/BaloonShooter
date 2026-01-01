using UnityEngine;

namespace BalloonShooter
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig config;

        [Header("Display (4K)")]
        [SerializeField] private bool applyNativeResolutionOnStart = true;
        [SerializeField] private bool preferNativeDisplayResolution = true;
        [SerializeField] private int fallbackWidth = 3840;
        [SerializeField] private int fallbackHeight = 2160;
        [SerializeField] private FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;

        [Header("Systems")]
        [SerializeField] private BalloonSpawner spawner;
        [SerializeField] private Shooter shooter;
        [SerializeField] private ScoreManager scoreManager;

        public GameConfig Config => config;
        public ScoreManager Score => scoreManager;

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public float TimeRemainingSeconds { get; private set; }
        public float TimeElapsedSeconds => config == null ? 0f : Mathf.Clamp(config.gameDurationSeconds - TimeRemainingSeconds, 0f, config.gameDurationSeconds);

        private Vector3 _wind;
        private float _nextWindChangeAt;

        private void Awake()
        {
            if (spawner == null) spawner = FindFirstObjectByType<BalloonSpawner>();
            if (shooter == null) shooter = FindFirstObjectByType<Shooter>();
            if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();

            if (scoreManager != null) scoreManager.SetConfig(config);
            if (spawner != null) spawner.SetConfig(config);
            if (spawner != null) spawner.SetManager(this);
            if (shooter != null) shooter.SetManager(this);
        }

        private void Start()
        {
            ApplyResolutionIfConfigured();
            StartNewRound();
        }

        private void Update()
        {
            if (config == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();

            if (Input.GetKeyDown(KeyCode.R))
                StartNewRound();

            if (!IsRunning || IsPaused) return;

            TimeRemainingSeconds -= Time.deltaTime;
            if (TimeRemainingSeconds <= 0f)
            {
                TimeRemainingSeconds = 0f;
                EndRound();
                return;
            }

            UpdateWind();
        }

        public Vector3 GetWind3D() => _wind;

        public void AddTime(float seconds)
        {
            if (config == null) return;
            TimeRemainingSeconds = Mathf.Clamp(TimeRemainingSeconds + seconds, 0f, config.gameDurationSeconds + 999f);
        }

        public void StartNewRound()
        {
            if (config == null) return;

            Time.timeScale = 1f;
            IsPaused = false;
            IsRunning = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            TimeRemainingSeconds = config.gameDurationSeconds;
            scoreManager?.ResetRun();
            spawner?.ResetRun();

            _wind = Vector3.zero;
            _nextWindChangeAt = Time.time;
            UpdateWind(force: true);
        }

        public void EndRound()
        {
            IsRunning = false;
            spawner?.StopSpawning();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void UpdateWind(bool force = false)
        {
            if (config == null) return;

            if (!force && Time.time < _nextWindChangeAt) return;
            _nextWindChangeAt = Time.time + Mathf.Max(0.1f, config.windChangeIntervalSeconds);

            float strength = config.windStrength * Random.Range(0.4f, 1.15f);
            float x = Random.Range(-1f, 1f);
            _wind = new Vector3(x, 0f, 0f).normalized * strength;
        }

        private void ApplyResolutionIfConfigured()
        {
#if UNITY_EDITOR
            _ = applyNativeResolutionOnStart;
            _ = preferNativeDisplayResolution;
            _ = fallbackWidth;
            _ = fallbackHeight;
            _ = fullScreenMode;
            return;
#else
            if (!applyNativeResolutionOnStart) return;

            int width = preferNativeDisplayResolution ? Display.main.systemWidth : fallbackWidth;
            int height = preferNativeDisplayResolution ? Display.main.systemHeight : fallbackHeight;
            if (width <= 0 || height <= 0) return;

            Screen.SetResolution(width, height, fullScreenMode);
#endif
        }
    }
}
