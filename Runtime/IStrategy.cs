using System;

namespace BehaviourTree
{
    public interface IStrategy
    {
        Node.Status Process();
        void Reset() {
            // Noop
        }
    }

    public class Condition : IStrategy
    {
        readonly Func<bool> _condition;

        public Condition(Func<bool> condition)
        {
            this._condition = condition;
        }

        public Node.Status Process()
        {
            return _condition() ? Node.Status.Success : Node.Status.Failure;
        }
    }

    public class ActionStrategy : IStrategy
    {
        readonly Action _action;
        public ActionStrategy(Action action)
        {
            this._action = action;
        }
        public Node.Status Process()
        {
            _action();
            return Node.Status.Success;
        }
    }
    
    public class NothingStrategy : ActionStrategy
    {
        public NothingStrategy() : base(() => {})
        {
        }
    }
}
