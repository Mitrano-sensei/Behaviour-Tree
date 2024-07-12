using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BehaviourTree
{
    #region Base Nodes

    public class Node
    {
        public enum Status
        {
            Success,
            Failure,
            Running
        }

        public readonly string Name;
        public readonly int Priority;

        protected readonly List<Node> Children = new();
        protected int CurrentChildIndex;

        protected Node(string name = "Node", int priority = 0)
        {
            this.Name = name;
            this.Priority = priority;
        }

        public virtual void AddChild(Node child) => Children.Add(child);

        public virtual Status Process()
        {
            return Children[CurrentChildIndex].Process();
        }

        public virtual void Reset()
        {
            CurrentChildIndex = 0;
            foreach (var child in Children)
            {
                child.Reset();
            }
        }
    }

    public class Leaf : Node
    {
        readonly IStrategy _strategy;

        public Leaf(IStrategy strategy, string name = "Leaf", int priority = 0) : base(name, priority)
        {
            _strategy = strategy;
        }

        public override Status Process() => _strategy.Process();
        public override void Reset() => _strategy.Reset();
        
        public override void AddChild(Node child) => throw new Exception(Name + " can't have children");
    }

    public class Decorator : Node
    {
        protected Decorator(string name, int priority = 0) : base(name, priority)
        {
        }

        public override Status Process()
        {
            if (Children.Count == 0) throw new Exception(Name + " must have a child");
            return Children[0].Process();
        }
        
        public override void AddChild(Node child)
        {
            if (Children.Count > 0) throw new System.Exception(Name + " can only have one child");
            base.AddChild(child);
        }
        
        public override void Reset()
        {
            Children[0].Reset();
        }
    }
    
    #endregion

    #region Composite Nodes

    /**
     * Will process each child in order, and return Success if all children returned Success
     *
     * (Seem to act like a Sequence, but will not reset after a Failure or Success)
     */
    public class BehaviourTree : Node
    {
        public BehaviourTree(string name = "BehaviourTree", int priority = 0) : base(name, priority)
        {
        }

        public override Status Process()
        {
            while (CurrentChildIndex < Children.Count)
            {
                var status = Children[CurrentChildIndex].Process();
                if (status != Status.Success) return status;
                CurrentChildIndex++;
            }

            return Status.Success;
        }
    }

    /**
     * Will Process each child in order, and return Success if all children returned Success
     * Will return Failure if one of the children failed
     */
    public class Sequence : Node
    {
        public Sequence(string name, int priority = 0) : base(name, priority)
        {
        }

        public override Status Process()
        {
            if (CurrentChildIndex < Children.Count)
            {
                switch (Children[CurrentChildIndex].Process())
                {
                    case Status.Success:
                        CurrentChildIndex++;
                        if (CurrentChildIndex == Children.Count)
                        {
                            Reset();
                            return Status.Success;
                        }
                        return Status.Running;
                    case Status.Failure:
                        Reset();
                        return Status.Failure;
                    case Status.Running:
                        return Status.Running;
                    default:
                        throw new Exception("Status not recognized, should never happen !");
                }
            }

            Reset();
            return Status.Success;
        }
    }

    /**
     * Will process each Child until one of them returns a Success, and returns Failure if all children failed
     */
    public class Selector : Node
    {
        public Selector(string name, int priority = 0) : base(name, priority)
        {
        }

        public override Status Process()
        {
            if (CurrentChildIndex < Children.Count)
            {
                switch (Children[CurrentChildIndex].Process())
                {
                    case Status.Success:
                        Reset();
                        return Status.Success;
                    case Status.Failure:
                        CurrentChildIndex++;
                        return CurrentChildIndex == Children.Count ? Status.Failure : Status.Running;
                    case Status.Running:
                        return Status.Running;
                    default:
                        throw new Exception("Status not recognized, should never happen !");
                }
            }

            Reset();
            return Status.Failure;
        }
    }

    /**
     * Will act like a Selector, but will process the children in order of their priority
     */
    public class PrioritySelector : Selector
    {
        List<Node> _sortedChildren;
        public List<Node> SortedChildren => _sortedChildren ??= SortChildren();
        protected virtual List<Node> SortChildren() => Children.OrderByDescending(c => c.Priority).ToList();

        public PrioritySelector(string name, int priority = 0) : base(name, priority)
        {
        }

        public override Status Process()
        {
            if (CurrentChildIndex < SortedChildren.Count)
            {
                switch (SortedChildren[CurrentChildIndex].Process())
                {
                    case Status.Success:
                        Reset();
                        return Status.Success;
                    case Status.Failure:
                        CurrentChildIndex++;
                        return CurrentChildIndex == SortedChildren.Count ? Status.Failure : Status.Running;
                    case Status.Running:
                        return Status.Running;
                    default:
                        throw new System.Exception("Status not recognized, should never happen !");
                }
            }

            Reset();
            return Status.Failure;
        }

        public override void Reset()
        {
            base.Reset();
            _sortedChildren = null;
        }
    }

    /**
     * Will act like a selector, but will shuffle the children before processing them
     *
     * Note that this breaks Liskov's principle, as it is not a PrioritySelector, but it is easier to implement this way
     */
    public class RandomSelector : PrioritySelector
    {
        protected override List<Node> SortChildren() => Children.OrderBy(_ => UnityEngine.Random.value).ToList();

        public RandomSelector(string name, int priority = 0) : base(name, priority)
        {
        }
    }

    /**
     * Will return the opposite of the child's status
     */
    public class Inverter : Decorator
    {
        public Inverter(string name, int priority = 0) : base(name, priority)
        {
        }

        public override Status Process()
        {
            switch (Children[0].Process())
            {
                case Status.Success:
                    Reset();
                    return Status.Failure;
                case Status.Failure:
                    Reset();
                    return Status.Success;
                case Status.Running:
                    return Status.Running;
                default:
                    throw new Exception("Status not recognized, should never happen !");
            }
        }
    }

    /**
     * Will repeat until the child returns a failure
     */
    public class UntilFail : Decorator
    {
        public UntilFail(string name, int priority = 0) : base(name, priority)
        {
        }

        public override Status Process()
        {
            if (Children.Count == 0) throw new Exception(Name + " must have a child");
            
            if (Children[0].Process() == Status.Failure)
            {
                Reset();
                return Status.Success;
            }

            return Status.Running;
        }
    }

    /**
     * Will repeat until the child returns a success
     */
    public class UntilSuccess : Decorator
    {
        public UntilSuccess(string name, int priority = 0) : base(name, priority)
        {
        }

        public override Status Process()
        {
            if (Children.Count == 0) throw new Exception(Name + " must have a child");
            
            if (Children[0].Process() == Status.Success)
            {
                Reset();
                return Status.Success;
            }

            return Status.Running;
        }
    }

    /**
     * Will repeat until the condition is true or the child returns failure
     */
    public class RepeatUntil : Decorator
    {
        readonly Func<bool> _condition;

        public RepeatUntil(Func<bool> condition, string name, int priority = 0) : base(name, priority)
        {
            _condition = condition;
        }

        public override Status Process()
        {
            if (Children.Count == 0) throw new Exception(Name + " must have a child");

            if (_condition())
            {
                Reset();
                return Status.Success;
            }

            var status = Children[0].Process();

            if (status == Status.Failure)
            {
                Reset();
                return Status.Failure;
            }

            return Status.Running;
        }
    }

    /**
     * Will repeat a certain amount of times, and stop if the child returns failure
     */
    public class Repeat : Decorator
    {
        private readonly int _times;
        private int _currentTimes;

        public Repeat(int times, string name, int priority = 0) : base(name, priority)
        {
            _times = times;
        }

        public override Status Process()
        {
            if (Children.Count == 0) throw new Exception(Name + " must have a child");

            if (_currentTimes >= _times)
            {
                Reset();
                return Status.Success;
            }

            var status = Children[0].Process();
            if (status == Status.Failure)
            {
                Reset();
                return Status.Failure;
            }

            if (status == Status.Success)
            {
                _currentTimes++;
            }
            return Status.Running;
        }

        public override void Reset()
        {
            base.Reset();
            _currentTimes = 0;
        }
    }

    /**
     * Executes normally, but will return Failure instantly if the condition is met
     */
    public class FailIf : Decorator
    {
        private readonly Func<bool> _condition;

        public FailIf(Func<bool> failIfCondition, string name, int priority = 0) : base(name, priority)
        {
            _condition = failIfCondition;
        }
        
        public override Status Process()
        {
            if (Children.Count == 0) throw new Exception(Name + " must have a child");

            if (_condition())
            {
                Reset();
                return Status.Failure;
            }

            return base.Process();
        }
    }
    
    /**
     * Executes normally, but return Success instantly if the condition is met
     */
    public class SucceedIf : Node
    {
        private readonly Func<bool> _condition;

        public SucceedIf(Func<bool> succeedIfCondition, string name, int priority = 0) : base(name, priority)
        {
            _condition = succeedIfCondition;
        }
        
        public override Status Process()
        {
            if (Children.Count == 0) throw new Exception(Name + " must have a child");

            if (_condition())
            {
                Reset();
                return Status.Success;
            }

            return base.Process();
        }
    }

    /**
     * Will take a condition, and process its first Child if the condition is true, and the second one if it is false
     */
    public class IfOr : Node
    {
        private readonly Func<bool> _condition;

        public IfOr(Func<bool> condition, string name, int priority = 0) : base(name, priority)
        {
            _condition = condition;
        }

        public override Status Process()
        {
            if (Children.Count != 2) throw new Exception(Name + " must have two children");
            
            return _condition() ? Children[0].Process() : Children[1].Process();
        }
    }

    /**
     * Will process child, will return Success if the child succeeded, Failure otherwise
     */
    public class OrFail : Decorator
    {
        public OrFail(string name, int priority = 0) : base(name, priority)
        {
        }

        public override Status Process()
        {
            if (Children.Count == 0) throw new Exception(Name + " must have a child");

            var status = Children[0].Process();
            if (status == Status.Success)
            {
                Reset();
                return Status.Success;
            }

            return Status.Failure;
        }
    }
    
    #endregion
    
    #region Special Leafs

    /**
     * Stays running for a certain amount of time, then return Success
     */
    public class WaitLeaf : Leaf
    {
        private readonly float _duration;
        private float _startTime = -1f;
        
        public WaitLeaf(float durationInSeconds, string name = "Wait", int priority = 0) : base(new ActionStrategy(() => { }), name, priority)
        {
            _duration = durationInSeconds;
        }

        public override Status Process()
        {
            if (_startTime < 0)
                _startTime = Time.time;

            // If the time is not elapsed, return Running
            if (Time.time - _startTime < _duration) 
                return Status.Running;
            
            // If the time is elapsed, return Success
            Reset();
            return Status.Success;

        }

        public override void Reset()
        {
            base.Reset();
            _startTime = -1f;
        }
    }

    /**
     * Will return running until the condition is met, then return Success.
     * Equivalent to a UntilSuccess and a ConditionLeaf
     */
    public class WaitFor : Leaf
    {
        private readonly Func<bool> _condition;
        public WaitFor(Func<bool> condition, string name = "WaitFor", int priority = 0) : base(new ActionStrategy(() => { }), name, priority)
        {
            _condition = condition;
        }

        public override Status Process()
        {
            if (_condition())
            {
                Reset();
                return Status.Success;
            }

            return Status.Running;
        }
    }

    /**
     * Will print a message in the console
     */
    public class DebugLeaf : Leaf
    {
        private readonly string _message;
        public DebugLeaf(string message, string name = "Debug", int priority = 0) : base(new ActionStrategy(() => Debug.Log(message)), name, priority)
        {
            _message = message;
        }
    }
    
    /**
     * Will return Success if the given condition is met, and Failure otherwise
     */
    public class ConditionLeaf : Leaf
    {
        private readonly Func<bool> _condition;
        public ConditionLeaf(Func<bool> condition, string name = "Condition", int priority = 0) : base(new ActionStrategy(() => { }), name, priority)
        {
            _condition = condition;
        }

        public override Status Process()
        {
            return _condition() ? Status.Success : Status.Failure;
        }
    }
    
    #endregion
}