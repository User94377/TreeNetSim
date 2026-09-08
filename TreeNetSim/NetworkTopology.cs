using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace TreeNetSim
{
    public class NetworkTopology
    {
        private List<NetworkNode> nodes;
        private int nextNodeId;

        public List<NetworkNode> Nodes => nodes;
        public NetworkNode RootNode => nodes.FirstOrDefault(n => n.Type == NodeType.Server);

        public NetworkTopology()
        {
            nodes = new List<NetworkNode>();
            nextNodeId = 0;
        }

        // Добавить узел
        public NetworkNode AddNode(NodeType type, Point position)
        {
            // Проверка: может быть только один сервер
            if (type == NodeType.Server && RootNode != null)
            {
                throw new InvalidOperationException("В сети может быть только один сервер!");
            }

            var node = new NetworkNode(nextNodeId++, type, position);
            nodes.Add(node);
            return node;
        }

        // Удалить узел
        public void RemoveNode(NetworkNode node)
        {
            if (node == null) return;

            // Удаляем связи с родителем
            if (node.Parent != null)
            {
                node.Parent.Children.Remove(node);
            }

            // Рекурсивно удаляем всех потомков
            var childrenCopy = new List<NetworkNode>(node.Children);
            foreach (var child in childrenCopy)
            {
                RemoveNode(child);
            }

            nodes.Remove(node);
        }

        // Создать связь между узлами
        public bool AddConnection(NetworkNode parent, NetworkNode child)
        {
            if (parent == null || child == null) return false;

            // Проверка: ребенок уже имеет родителя
            if (child.Parent != null)
            {
                throw new InvalidOperationException($"Узел {child.Name} уже имеет родителя!");
            }

            // Проверка: не создаем цикл
            if (WouldCreateCycle(parent, child))
            {
                throw new InvalidOperationException("Это соединение создаст цикл в сети!");
            }

            // Проверка: станция не может быть родителем
            if (parent.Type == NodeType.Station)
            {
                throw new InvalidOperationException("Станция не может иметь дочерних узлов!");
            }

            parent.Children.Add(child);
            child.Parent = parent;
            return true;
        }

        // Удалить связь
        public void RemoveConnection(NetworkNode parent, NetworkNode child)
        {
            if (parent == null || child == null) return;

            parent.Children.Remove(child);
            child.Parent = null;
        }

        // Проверка на создание цикла
        private bool WouldCreateCycle(NetworkNode parent, NetworkNode child)
        {
            var current = parent;
            while (current != null)
            {
                if (current == child)
                    return true;
                current = current.Parent;
            }
            return false;
        }

        // Проверка корректности топологии
        public (bool isValid, string errorMessage) ValidateTopology()
        {
            // 1. Должен быть хотя бы один сервер
            if (RootNode == null)
                return (false, "В сети должен быть сервер!");

            // 2. Проверка: все узлы должны быть связаны
            var reachableNodes = new HashSet<NetworkNode>();
            TraverseTree(RootNode, reachableNodes);

            if (reachableNodes.Count != nodes.Count)
            {
                var orphans = nodes.Count - reachableNodes.Count;
                return (false, $"В сети есть {orphans} несвязанных узлов!");
            }

            // 3. Проверка: должна быть хотя бы одна станция
            if (!nodes.Any(n => n.Type == NodeType.Station))
                return (false, "В сети должна быть хотя бы одна станция!");

            // 4. Проверка: нет циклов (у каждого узла кроме корня один родитель)
            foreach (var node in nodes)
            {
                if (node != RootNode && node.Parent == null)
                    return (false, $"Узел {node.Name} не подключен к сети!");
            }

            return (true, "Топология корректна");
        }

        // Обход дерева
        private void TraverseTree(NetworkNode node, HashSet<NetworkNode> visited)
        {
            if (node == null || visited.Contains(node))
                return;

            visited.Add(node);
            foreach (var child in node.Children)
            {
                TraverseTree(child, visited);
            }
        }

        // Найти узел по позиции
        public NetworkNode FindNodeAtPosition(Point position, double radius = 20)
        {
            foreach (var node in nodes)
            {
                double distance = Math.Sqrt(
                    Math.Pow(node.Position.X - position.X, 2) +
                    Math.Pow(node.Position.Y - position.Y, 2)
                );

                if (distance <= radius)
                    return node;
            }
            return null;
        }

        // Очистить всю топологию
        public void Clear()
        {
            nodes.Clear();
            nextNodeId = 0;
        }

        // Получить все станции
        public List<NetworkNode> GetStations()
        {
            return nodes.Where(n => n.Type == NodeType.Station).ToList();
        }
    }
}