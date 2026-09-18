namespace Match3.Gameplay
{
    public static class GameFlow
    {
        public static IGameFlowController Controller { get; private set; }

        public static void Register(IGameFlowController controller)
        {
            Controller = controller;
        }

        public static void Unregister(IGameFlowController controller)
        {
            if (Controller == controller)
            {
                Controller = null;
            }
        }
    }
}
