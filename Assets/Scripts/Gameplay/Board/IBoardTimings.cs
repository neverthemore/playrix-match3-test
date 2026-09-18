using Match3.Core.Domain;

namespace Match3.Gameplay.Board
{
    public interface IBoardTimings
    {
        float SwapDuration { get; }

        float ClearDuration { get; }

        float GetBoosterDuration(BoosterType booster);

        float GetGravityDuration(GravityResult result);
    }
}
