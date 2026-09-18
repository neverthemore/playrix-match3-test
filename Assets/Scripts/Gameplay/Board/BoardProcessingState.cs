namespace Match3.Gameplay.Board
{
    public enum BoardProcessingState
    {
        Idle,
        Swapping,
        ResolvingMatches,
        ApplyingGravity,
        CheckingCascades,
        Shuffling
    }
}
