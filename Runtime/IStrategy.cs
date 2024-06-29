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
        readonly Func<bool> condition;

        public Condition(Func<bool> condition)
        {
            this.condition = condition;
        }

        public Node.Status Process()
        {
            return condition() ? Node.Status.Success : Node.Status.Failure;
        }
    }

    public class ActionStrategy : IStrategy
    {
        readonly Action action;
        public ActionStrategy(Action action)
        {
            this.action = action;
        }
        public Node.Status Process()
        {
            action();
            return Node.Status.Success;
        }
    }
}
