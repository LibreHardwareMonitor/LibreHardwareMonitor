// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aga.Controls.Tree;

namespace OpenHardwareMonitor.GUI
{
    public class TreeModel : ITreeModel
    {
        private Node root;
        private bool forceVisible = false;

        public TreeModel()
        {
            root = new Node();
            root.Model = this;
        }

        public TreePath GetPath(Node node)
        {
            if (node == root)
                return TreePath.Empty;
            else
            {
                Stack<object> stack = new Stack<object>();
                while (node != root)
                {
                    stack.Push(node);
                    node = node.Parent;
                }
                return new TreePath(stack.ToArray());
            }
        }

        public Collection<Node> Nodes
        {
            get { return root.Nodes; }
        }

        private Node GetNode(TreePath treePath)
        {
            Node parent = root;
            foreach (object obj in treePath.FullPath)
            {
                Node node = obj as Node;
                if (node == null || node.Parent != parent)
                    return null;
                parent = node;
            }
            return parent;
        }

        public IEnumerable GetChildren(TreePath treePath)
        {
            Node node = GetNode(treePath);
            if (node != null)
            {
                foreach (Node n in node.Nodes)
                    if (forceVisible || n.IsVisible)
                        yield return n;
            }
            else
            {
                yield break;
            }
        }

        public bool IsLeaf(TreePath treePath)
        {
            return false;
        }

        public bool ForceVisible
        {
            get
            {
                return forceVisible;
            }
            set
            {
                if (value != forceVisible)
                {
                    forceVisible = value;
                    OnStructureChanged(root);
                }
            }
        }

#pragma warning disable 67
        public event EventHandler<TreeModelEventArgs> NodesChanged;
        public event EventHandler<TreePathEventArgs> StructureChanged;
        public event EventHandler<TreeModelEventArgs> NodesInserted;
        public event EventHandler<TreeModelEventArgs> NodesRemoved;
#pragma warning restore 67

        public void OnNodeChanged(Node parent, int index, Node node)
        {
            if (NodesChanged != null && parent != null)
            {
                TreePath path = GetPath(parent);
                if (path != null)
                    NodesChanged(this, new TreeModelEventArgs(
                      path, new int[] { index }, new object[] { node }));
            }
        }

        public void OnStructureChanged(Node node)
        {
            if (StructureChanged != null)
                StructureChanged(this,
                  new TreeModelEventArgs(GetPath(node), new object[0]));
        }

        public void OnNodeInserted(Node parent, int index, Node node)
        {
            if (NodesInserted != null)
            {
                TreeModelEventArgs args = new TreeModelEventArgs(GetPath(parent),
                  new int[] { index }, new object[] { node });
                NodesInserted(this, args);
            }

        }

        public void OnNodeRemoved(Node parent, int index, Node node)
        {
            if (NodesRemoved != null)
            {
                TreeModelEventArgs args = new TreeModelEventArgs(GetPath(parent),
                  new int[] { index }, new object[] { node });
                NodesRemoved(this, args);
            }
        }

    }
}
