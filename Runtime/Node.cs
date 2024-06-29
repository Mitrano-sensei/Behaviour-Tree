using Codice.Client.BaseCommands.Differences;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.YamlDotNet.Serialization;
using UnityEngine;

namespace BehaviourTree
{
    #region Base Nodes
    public class Node
    {
        public enum Status { Success, Failure, Running }

        public readonly string name;
        public readonly int priority;

        public readonly List<Node> children = new();
        protected int currentChildIndex;

        public Node(string name = "Node", int priority = 0)
        {
            this.name = name;
            this.priority = priority;
        }

        public virtual void AddChild(Node child) => children.Add(child);

        public virtual Status Process()
        {
            return children[currentChildIndex].Process();
        }

        public virtual void Reset()
        {
            currentChildIndex = 0;
            foreach (var child in children)
            {
                child.Reset();
            }
        }
    }

    public class Leaf : Node
    {
        readonly IStrategy strategy;
        public Leaf(IStrategy strategy, string name = "Leaf", int priority = 0) : base(name, priority)
        {
            this.strategy = strategy;
        }

        public override Status Process() => strategy.Process();
        public override void Reset() => strategy.Reset();
    }
    #endregion

    #region Composite Nodes

    public class BehaviourTree : Node
    {
        public BehaviourTree(string name = "BehaviourTree", int priority = 0) : base(name, priority)
        {
        }
        public override Status Process()
        {
            while (currentChildIndex < children.Count)
            {
                var status = children[currentChildIndex].Process();
                if (status != Status.Success) return status;
                currentChildIndex++;
            }
            return Status.Success;
        }
        public override void Reset()
        {
            base.Reset();
        }
    }

    public class Sequence : Node
    {
        public Sequence(string name, int priority = 0) : base(name, priority) { }

        public override Status Process()
        {
            if (currentChildIndex < children.Count)
            {
                switch (children[currentChildIndex].Process())
                {
                    case Status.Success:
                        currentChildIndex++;
                        return currentChildIndex == children.Count ? Status.Success : Status.Running;
                    case Status.Failure:
                        Reset();
                        return Status.Failure;
                    case Status.Running:
                        return Status.Running;
                    default:
                        throw new System.Exception("Status not recognized, should never happen !");
                }
            }

            Reset();
            return Status.Success;
        }
    }

    public class Selector : Node
    {
        public Selector(string name, int priority = 0) : base(name, priority) { }
        public override Status Process()
        {
            if (currentChildIndex < children.Count)
            {
                switch (children[currentChildIndex].Process())
                {
                    case Status.Success:
                        Reset();
                        return Status.Success;
                    case Status.Failure:
                        currentChildIndex++;
                        return currentChildIndex == children.Count ? Status.Failure : Status.Running;
                    case Status.Running:
                        return Status.Running;
                    default:
                        throw new System.Exception("Status not recognized, should never happen !");
                }
            }
            Reset();
            return Status.Failure;
        }
    }

    public class PrioritySelector : Selector
    {
        List<Node> sortedChildren;
        public List<Node> SortedChildren => sortedChildren ??= SortChildren();
        protected virtual List<Node> SortChildren() => children.OrderByDescending(c => c.priority).ToList();

        public PrioritySelector(string name, int priority = 0) : base(name, priority) { }
        public override Status Process()
        {
            if (currentChildIndex < SortedChildren.Count)
            {
                switch (SortedChildren[currentChildIndex].Process())
                {
                    case Status.Success:
                        Reset();
                        return Status.Success;
                    case Status.Failure:
                        currentChildIndex++;
                        return currentChildIndex == SortedChildren.Count ? Status.Failure : Status.Running;
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
            sortedChildren = null;
        }

    }

    // Note that this breaks Liskov's principle, as it is not a PrioritySelector, but it is easier to implement this way
    public class RandomSelector : PrioritySelector
    {
        protected override List<Node> SortChildren() => children.OrderBy(_ => UnityEngine.Random.value).ToList();

        public RandomSelector(string name, int priority = 0) : base(name, priority) { }
    }

    public class Inverter : Node
    {
        public Inverter(string name, int priority = 0) : base(name, priority) { }
        public override Status Process()
        {
            switch (children[0].Process())
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
                    throw new System.Exception("Status not recognized, should never happen !");
            }
        }

        public override void AddChild(Node child)
        {
            // TODO : Could make it work with multiple children if needed
            if (children.Count > 0) throw new System.Exception("Inverter can only have one child");
            base.AddChild(child);
        }
    }

    public class UntilFail : Node
    {
        public UntilFail(string name, int priority = 0) : base(name, priority) { }
        public override Status Process()
        {
            if (children[0].Process() == Status.Failure)
            {
                Reset();
                return Status.Success;
            }

            return Status.Running;
        }
        public override void AddChild(Node child)
        {
            // TODO : Could make it work with multiple children if needed
            if (children.Count > 0) throw new System.Exception("UntilFail can only have one child");
            base.AddChild(child);
        }
    }

    public class UntilSuccess : Node
    {
        public UntilSuccess(string name, int priority = 0) : base(name, priority) { }
        public override Status Process()
        {
            if (children[0].Process() == Status.Success)
            {
                Reset();
                return Status.Success;
            }

            return Status.Running;
        }
        public override void AddChild(Node child)
        {
            // TODO : Could make it work with multiple children if needed
            if (children.Count > 0) throw new System.Exception("UntilSuccess can only have one child");
            base.AddChild(child);
        }
    }

    // Repeat Node
    #endregion
}
