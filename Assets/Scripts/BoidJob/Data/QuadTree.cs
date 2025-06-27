using System.Collections.Generic;
using UnityEngine;

namespace BoidJob.Data
{
    public class QuadTree
    {
        private class Node
        {
            public Rect bounds;
            public List<int> indices;
            public Node[] children;
            public bool IsLeaf => children == null;

            public Node(Rect bounds)
            {
                this.bounds = bounds;
                indices = new List<int>();
                children = null;
            }
        }

        private Node root;
        private int maxObjects;
        private int maxDepth;
        private ListBoidVariable listBoidVariable;

        public QuadTree(Rect bounds, int maxObjects, int maxDepth, ListBoidVariable listBoidVariable)
        {
            root = new Node(bounds);
            this.maxObjects = maxObjects;
            this.maxDepth = maxDepth;
            this.listBoidVariable = listBoidVariable;
        }

        public void Clear()
        {
            root = new Node(root.bounds);
        }

        public void Insert(int index)
        {
            Insert(root, index, 0);
        }

        private void Insert(Node node, int index, int depth)
        {
            var pos = listBoidVariable.boidDatas[index].position;
            if (!node.bounds.Contains(new Vector2(pos.x, pos.y))) return;

            if (node.IsLeaf && (node.indices.Count < maxObjects || depth >= maxDepth))
            {
                node.indices.Add(index);
                return;
            }

            if (node.IsLeaf)
                Subdivide(node);

            foreach (var child in node.children)
                Insert(child, index, depth + 1);
        }

        private void Subdivide(Node node)
        {
            node.children = new Node[4];
            float w = node.bounds.width / 2f;
            float h = node.bounds.height / 2f;
            float x = node.bounds.xMin;
            float y = node.bounds.yMin;
            node.children[0] = new Node(new Rect(x, y, w, h)); // bottom left
            node.children[1] = new Node(new Rect(x + w, y, w, h)); // bottom right
            node.children[2] = new Node(new Rect(x, y + h, w, h)); // top left
            node.children[3] = new Node(new Rect(x + w, y + h, w, h)); // top right
            // Di chuyển các index hiện tại vào các node con
            foreach (var idx in node.indices)
                foreach (var child in node.children)
                    Insert(child, idx, 0);
            node.indices.Clear();
        }

        public void Query(Rect area, List<int> result)
        {
            Query(root, area, result);
        }

        private void Query(Node node, Rect area, List<int> result)
        {
            if (!node.bounds.Overlaps(area)) return;
            if (node.IsLeaf)
            {
                foreach (var idx in node.indices)
                {
                    var pos = listBoidVariable.boidDatas[idx].position;
                    if (area.Contains(new Vector2(pos.x, pos.y)))
                        result.Add(idx);
                }
            }
            else
            {
                foreach (var child in node.children)
                    Query(child, area, result);
            }
        }
    }
}