using TMPro;
using UnityEngine;

namespace BalloonShooter
{
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private GameManager manager;
        [SerializeField] private ScoreManager score;

        [Header("HUD Text")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text accuracyText;

        [Header("Crosshair")]
        [SerializeField] private RectTransform crosshairRect;

        [Header("End Panel")]
        [SerializeField] private CanvasGroup endPanel;
        [SerializeField] private TMP_Text endSummaryText;

        private void Awake()
        {
            if (manager == null) manager = FindFirstObjectByType<GameManager>();
            if (score == null) score = manager != null ? manager.Score : FindFirstObjectByType<ScoreManager>();

            if (crosshairRect == null)
            {
                Transform t = transform.Find("Crosshair");
                if (t != null) crosshairRect = t as RectTransform;
            }

            ApplyEndPanel(visible: false);
        }

        private void Update()
        {
            if (manager == null || score == null) return;

            if (crosshairRect != null)
                crosshairRect.position = Input.mousePosition;

            if (scoreText != null) scoreText.text = $"Score: {score.Score}";
            if (highScoreText != null) highScoreText.text = $"High: {score.HighScore}";
            if (timeText != null) timeText.text = $"Time: {manager.TimeRemainingSeconds:0.0}s";
            if (comboText != null)
            {
                string pause = manager.IsPaused ? " (Paused)" : string.Empty;
                comboText.text = score.ComboCount <= 0
                    ? $"Combo: -{pause}"
                    : $"Combo: {score.ComboCount}  x{score.ComboMultiplier:0.00}{pause}";
            }

            if (accuracyText != null)
                accuracyText.text = $"Acc: {score.Accuracy01 * 100f:0}%  ({score.Hits}/{score.Shots})";

            bool showEnd = !manager.IsRunning && !manager.IsPaused;
            ApplyEndPanel(showEnd);
            if (showEnd && endSummaryText != null)
            {
                endSummaryText.text =
                    $"Round Over\n\nScore: {score.Score}\nHigh Score: {score.HighScore}\nAccuracy: {score.Accuracy01 * 100f:0}%\nBest Combo: {score.BestCombo}\n\nPress R to Restart";
            }
        }

        private void ApplyEndPanel(bool visible)
        {
            if (endPanel == null) return;
            endPanel.alpha = visible ? 1f : 0f;
            endPanel.interactable = visible;
            endPanel.blocksRaycasts = visible;
        }
    }
}
