namespace Asteroids.Core
{
    using TMPro;
    using UnityEngine;

    /// <summary>
    /// Managed MonoBehaviour attached to a screen-space Canvas GameObject.
    /// Provides a thread-safe static singleton accessor and exposes SetScore
    /// to format and display the player score as uGUI text.
    /// </summary>
    public class ScoreDisplayView : MonoBehaviour
    {
        public static ScoreDisplayView Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _scoreText;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Formats the score as "SCORE: {score:D6}" and writes to the TMP uGUI text component.
        /// Null-safe: checks _scoreText before assigning to prevent NullReferenceException.
        /// </summary>
        public void SetScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"SCORE: {score:D6}";
            }
        }
    }
}
