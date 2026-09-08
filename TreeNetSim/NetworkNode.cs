using System;
using System.Collections.Generic;
using System.Windows;

namespace TreeNetSim
{
    public enum NodeType
    {
        Server,      // Сервер (корень дерева)
        Switch,      // Коммутатор
        Station      // Рабочая станция (лист дерева)
    }

    public class NetworkNode
    {
        public int Id { get; set; }
        public NodeType Type { get; set; }
        public Point Position { get; set; }
        public List<NetworkNode> Children { get; set; }
        public NetworkNode Parent { get; set; }
        public string Name { get; set; }

        public NetworkNode(int id, NodeType type, Point position)
        {
            Id = id;
            Type = type;
            Position = position;
            Children = new List<NetworkNode>();
            Parent = null;
            Name = $"{type}_{id}";
        }

        public bool IsLeaf => Children.Count == 0;
        public bool IsRoot => Parent == null;
    }
}