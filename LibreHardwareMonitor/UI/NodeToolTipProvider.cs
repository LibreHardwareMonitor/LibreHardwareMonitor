// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;

namespace LibreHardwareMonitor.UI;

internal class NodeToolTipProvider : IToolTipProvider
{
    public string GetToolTip(TreeNodeAdv node, NodeControl nodeControl) => (node.Tag as Node)?.ToolTip;
}