using System;
using System.Collections.Generic;

namespace GUI.Semantic
{
    public sealed class AstProperty
    {
        public AstProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }
    }

    public sealed class AstChild
    {
        public AstChild(string label, AstNode node)
        {
            Label = label;
            Node = node;
        }

        public string Label { get; }
        public AstNode Node { get; }
    }

    public abstract class AstNode
    {
        public abstract string NodeType { get; }

        public virtual IReadOnlyList<AstProperty> GetProperties()
        {
            return Array.Empty<AstProperty>();
        }

        public virtual IReadOnlyList<AstChild> GetChildren()
        {
            return Array.Empty<AstChild>();
        }
    }


    // 
}