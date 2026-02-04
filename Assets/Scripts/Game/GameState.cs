namespace Reversi.Game
{
    public enum GameState
    {
        Waiting,
        
        BlackTurn,
        
        WhiteTurn,
        
        GameOver
    }

    public static class GameStateExtensions
    {
        public static StoneType GetCurrentStone(this GameState state)
        {
            return state switch
            {
                GameState.BlackTurn => StoneType.Black,
                GameState.WhiteTurn => StoneType.White,
                _ => StoneType.None
            };
        }

        public static GameState GetNextTurnState(this GameState state)
        {
            return state switch
            {
                GameState.BlackTurn => GameState.WhiteTurn,
                GameState.WhiteTurn => GameState.BlackTurn,
                _ => state
            };
        }

        public static bool IsPlaying(this GameState state)
        {
            return state == GameState.BlackTurn || state == GameState.WhiteTurn;
        }
    }
}
