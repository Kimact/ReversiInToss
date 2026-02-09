namespace Reversi.Domain
{
    /// <summary>
    /// 게임 상태 열거형
    /// </summary>
    public enum GameState
    {
        /// <summary>대기 중 (게임 시작 전)</summary>
        Waiting,
        
        /// <summary>흑돌 차례</summary>
        BlackTurn,
        
        /// <summary>백돌 차례</summary>
        WhiteTurn,
        
        /// <summary>게임 종료</summary>
        GameOver
    }

    /// <summary>
    /// 게임 상태 확장 메서드
    /// </summary>
    public static class GameStateExtensions
    {
        /// <summary>
        /// 현재 턴의 돌 타입 반환
        /// </summary>
        public static StoneType GetCurrentStone(this GameState state)
        {
            return state switch
            {
                GameState.BlackTurn => StoneType.Black,
                GameState.WhiteTurn => StoneType.White,
                _ => StoneType.None
            };
        }

        /// <summary>
        /// 다음 턴 상태 반환
        /// </summary>
        public static GameState GetNextTurnState(this GameState state)
        {
            return state switch
            {
                GameState.BlackTurn => GameState.WhiteTurn,
                GameState.WhiteTurn => GameState.BlackTurn,
                _ => state
            };
        }

        /// <summary>
        /// 게임 진행 중인지 확인
        /// </summary>
        public static bool IsPlaying(this GameState state)
        {
            return state == GameState.BlackTurn || state == GameState.WhiteTurn;
        }
    }
}
