using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTree
{
    public class DemoCubeBehaviour : MonoBehaviour
    {
        private BehaviourTree _behaviour = new();
        private Renderer _renderer;

        void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        void Start()
        {
            var repeat = new Repeat(5, "Repeater");
            var sequence = new Sequence("Main Sequence");

            var say1 = new DebugLeaf("Turning Red");
            var turnRed = new Leaf(new ActionStrategy(() => _renderer.material.color = Color.red));
            
            var wait = new WaitLeaf(.5f);
            var say2 = new DebugLeaf("Turning Blue");
            var turnBlue = new Leaf(new ActionStrategy(() => _renderer.material.color = Color.blue));
            
            sequence.AddChild(say1);
            sequence.AddChild(turnRed);
            sequence.AddChild(wait);
            sequence.AddChild(say2);
            sequence.AddChild(turnBlue);
            sequence.AddChild(wait);
            
            repeat.AddChild(sequence);
            
            _behaviour.AddChild(repeat);
        }

        void Update()
        {
            _behaviour.Process();
        }
    }
}
