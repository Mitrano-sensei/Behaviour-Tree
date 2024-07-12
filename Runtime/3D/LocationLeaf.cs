using System;
using UnityEngine;

namespace BehaviourTree
{
    public class LocationLeaf : Leaf
    {
        public LocationLeaf(Collider triggerZone, Transform target, string name = "LocationLeaf", int priority = 0) : base(new ColliderContainsStrategy(triggerZone, target), name, priority)
        {}
    }
    
    public class ColliderContainsStrategy : IStrategy
    {
        private readonly Collider _triggerZone;
        private readonly Transform _target;

        public ColliderContainsStrategy(Collider triggerZone, Transform target)
        {
            _triggerZone = triggerZone;
            _target = target;
        }

        public Node.Status Process()
        {
            return _triggerZone.bounds.Contains(_target.position) ? Node.Status.Success : Node.Status.Failure;
        }
    }
}