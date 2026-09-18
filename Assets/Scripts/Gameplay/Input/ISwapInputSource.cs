using System;
using UnityEngine;

namespace Match3.Gameplay.Input
{
    public interface ISwapInputSource
    {
        event Action<Vector2Int, Vector2Int> SwapRequested;
    }
}
