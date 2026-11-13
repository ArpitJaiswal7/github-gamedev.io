namespace Blockade2D.Core.Model
{
    /// <summary>
    /// Identifies the game ruleset and layout.
    /// Classic: Standard 2-player Quoridor across opposing baselines.
    /// BullsEye: 4-player battle positioned at the 4 borders racing to the center cell.
    /// </summary>
    public enum GameMode
    {
        Classic = 0,
        BullsEye = 1
    }
}
