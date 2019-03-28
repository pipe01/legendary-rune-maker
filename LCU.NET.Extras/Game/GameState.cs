using Anotar.Log4Net;
using log4net;
using PiMachine;

namespace Legendary_Rune_Maker.Game
{
    public static class GameState
    {
        public static StateMachine<GameStates, GameTriggers> State { get; } = CreateMachine();

        public static bool CanUpload => State.CurrentState != GameStates.Disconnected && State.CurrentState != GameStates.NotLoggedIn;

        private static StateMachine<GameStates, GameTriggers> CreateMachine()
        {
            LogTo.Debug("Initializing state machine");

            var machine = new StateMachine<GameStates, GameTriggers>();
            machine.PermitFromAny(GameTriggers.CloseGame, GameStates.Disconnected);
            machine.PermitFromAny(GameTriggers.LogOut, GameStates.NotLoggedIn);

            machine.Configure(GameStates.Disconnected)
                .Permit(GameTriggers.OpenGame, GameStates.NotLoggedIn);

            machine.Configure(GameStates.NotLoggedIn)
                .Permit(GameTriggers.LogIn, GameStates.LoggedIn);

            machine.Configure(GameStates.LoggedIn)
                .Permit(GameTriggers.EnterChampSelect, GameStates.InChampSelect);

            machine.Configure(GameStates.InChampSelect)
                .Permit(GameTriggers.ExitChampSelect, GameStates.LoggedIn)
                .Permit(GameTriggers.LockIn, GameStates.LockedIn);

            machine.Configure(GameStates.LockedIn)
                .Permit(GameTriggers.LockIn, GameStates.LockedIn)
                .Permit(GameTriggers.ExitChampSelect, GameStates.LoggedIn);

            machine.EnteredState += Machine_EnteredState;

            return machine;
        }

        private static void Machine_EnteredState(GameStates state)
        {
            LogTo.Debug("Entered UI state " + state);
        }
    }

    public enum GameStates
    {
        Disconnected,
        NotLoggedIn,
        LoggedIn,
        InChampSelect,
        LockedIn
    }

    public enum GameTriggers
    {
        CloseGame,
        OpenGame,
        LogIn,
        LogOut,
        EnterChampSelect,
        ExitChampSelect,
        LockIn
    }
}
