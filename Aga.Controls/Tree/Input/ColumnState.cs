using System;
using System.Collections.Generic;
using System.Text;

namespace Aga.Controls.Tree
{
	internal abstract class ColumnState : InputState
	{
		private TreeColumn _column;
		public TreeColumn Column 
		{
			get { return _column; } 
		}

		public ColumnState(TreeViewAdv tree, TreeColumn column)
			: base(tree)
		{
			_column = column;
		}
	}
}
