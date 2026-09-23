namespace Asteroids.Core
{
    using Unity.Entities;

    /// <summary>
    /// Managed bridge system in PresentationSystemGroup that reads the GameScore singleton
    /// and updates the uGUI Canvas score text only when the score value actually changes.
    /// Uses dirty checking (_lastDisplayedScore) to avoid per-frame string allocations.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ScoreUIBridgeSystem : SystemBase
    {
        private int _lastDisplayedScore = -1;

        protected override void OnCreate()
        {
            RequireForUpdate<GameScore>();
        }

        protected override void OnUpdate()
        {
            // Null safety: early exit if the UI view has not been instantiated yet
            // (e.g., during scene transitions or editor assembly reloads)
            if (ScoreDisplayView.Instance == null)
                return;

            var score = SystemAPI.GetSingleton<GameScore>();

            // Dirty checking: only update UI when the score value has actually changed.
            // This prevents per-frame string heap garbage allocations on unchanged frames.
            if (score.CurrentScore != _lastDisplayedScore)
            {
                _lastDisplayedScore = score.CurrentScore;
                ScoreDisplayView.Instance.SetScore(_lastDisplayedScore);
            }
        }
    }
}
