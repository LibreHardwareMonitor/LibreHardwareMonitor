// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.ObjectModel;
using System.Drawing;

namespace LibreHardwareMonitor.UI;

public class Node
{
    private Node _parent;
    private readonly NodeCollection _nodes;
    private string _text;
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

    public TreeModel Model { get; set; }

    public Node Parent
    {
        get { return _parent; }
        set
        {
            if (value != _parent)
            {
                _parent?._nodes.Remove(this);
                value?._nodes.Add(this);
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
            _text = value;
        }
    }

    public virtual string ToolTip { get; }

    public Image Image { get; set; }

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
                IsVisibleChanged?.Invoke(this);
            }
        }
    }

    private class NodeCollection : Collection<Node>
    {
        private readonly Node _owner;

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
                throw new ArgumentNullException(nameof(item));

            if (item._parent != _owner)
            {
                item._parent?._nodes.Remove(item);
                item._parent = _owner;
                base.InsertItem(index, item);

                TreeModel model = _owner.RootTreeModel();
                model?.OnStructureChanged(_owner);
                _owner.NodeAdded?.Invoke(item);
            }
        }

        protected override void RemoveItem(int index)
        {
            Node item = this[index];
            item._parent = null;
            base.RemoveItem(index);

            TreeModel model = _owner.RootTreeModel();
            model?.OnStructureChanged(_owner);
            _owner.NodeRemoved?.Invoke(item);
        }

        protected override void SetItem(int index, Node item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            RemoveAt(index);
            InsertItem(index, item);
        }
    }
}