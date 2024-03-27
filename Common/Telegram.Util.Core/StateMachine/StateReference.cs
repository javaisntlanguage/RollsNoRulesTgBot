namespace Telegram.Util.Core.StateMachine
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal class StateReference
        {
            public TState State { get; set; }
        }
    }
}
