// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using Aga.Controls.Tree;

namespace OpenHardwareMonitor.GUI
{
    public class Node
    {
        private TreeModel _treeModel;
        private Node _parent;
        private NodeCollection _nodes;
        private string _text;
        private Image _image;
        private bool _visible;

        public delegate void NodeEventHandler(Node node);
        public event NodeEventHandler IsVisibleChanged;
        public event NodeEventHandler NodeAdded;
        public event NodeEventHandler NodeRemoved;

        private TreeModel RootTreeModel()
        {
            Node node = this;
            while (node != null)
            {
                if (node.Model != null)
                    return node.Model;
                node = node._parent;
            }
            return null;
        }

        public Node() : this(string.Empty) { }

        public Node(string text)
        {
            _text = text;
            _nodes = new NodeCollection(this);
            _visible = true;
        }

        public TreeModel Model
        {
            get { return _treeModel; }
            set { _treeModel = value; }
        }

        public Node Parent
        {
            get { return _parent; }
            set
            {
                if (value != _parent)
                {
                    if (_parent != null)
                        _parent._nodes.Remove(this);
                    if (value != null)
                        value._nodes.Add(this);
                }
            }
        }

        public Collection<Node> Nodes
        {
            get { return _nodes; }
        }

        public virtual string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                }
            }
        }

        public Image Image
        {
            get { return _image; }
            set
            {
                if (_image != value)
                {
                    _image = value;
                }
            }
        }

        public virtual bool IsVisible
        {
            get { return _visible; }
            set
            {
                if (value != _visible)
                {
                    _visible = value;
                    TreeModel model = RootTreeModel();
                    if (model != null && _parent != null)
                    {
                        int index = 0;
                        for (int i = 0; i < _parent._nodes.Count; i++)
                        {
                            Node node = _parent._nodes[i];
                            if (node == this)
                                break;
                            if (node.IsVisible || model.ForceVisible)
                                index++;
                        }
                        if (model.ForceVisible)
                        {
                            model.OnNodeChanged(_parent, index, this);
                        }
                        else
                        {
                            if (value)
                                model.OnNodeInserted(_parent, index, this);
                            else
                                model.OnNodeRemoved(_parent, index, this);
                        }
                    }
                    if (IsVisibleChanged != null)
                        IsVisibleChanged(this);
                }
            }
        }

        private class NodeCollection : Collection<Node>
        {
            private Node _owner;

            public NodeCollection(Node owner)
            {
                _owner = owner;
            }

            protected override void ClearItems()
            {
                while (Count != 0)
                    RemoveAt(Count - 1);
            }

            protected override void InsertItem(int index, Node item)
            {
                if (item == null)
                    throw new ArgumentNullException("item");

                if (item._parent != _owner)
                {
                    if (item._parent != null)
                        item._parent._nodes.Remove(item);
                    item._parent = _owner;
                    base.InsertItem(index, item);

                    TreeModel model = _owner.RootTreeModel();
                    if (model != null)
                        model.OnStructureChanged(_owner);
                    if (_owner.NodeAdded != null)
                        _owner.NodeAdded(item);
                }
            }

            protected override void RemoveItem(int index)
            {
                Node item = this[index];
                item._parent = null;
                base.RemoveItem(index);

                TreeModel model = _owner.RootTreeModel();
                if (model != null)
                    model.OnStructureChanged(_owner);
                if (_owner.NodeRemoved != null)
                    _owner.NodeRemoved(item);
            }

            protected override void SetItem(int index, Node item)
            {
                if (item == null)
                    throw new ArgumentNullException("item");

                RemoveAt(index);
                InsertItem(index, item);
            }
        }
    }
}
