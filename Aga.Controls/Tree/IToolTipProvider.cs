using System;
using System.Collections.Generic;
using System.Text;
using Aga.Controls.Tree.NodeControls;

namespace Aga.Controls.Tree
{
	public interface IToolTipProvider
	{
		string GetToolTip(TreeNodeAdv node, NodeControl nodeControl);
	}
}
