using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Design;

namespace Aga.Controls
{
	public class StringCollectionEditor : CollectionEditor
	{
		public StringCollectionEditor(Type type): base(type)
		{
		}

		protected override Type CreateCollectionItemType()
		{
			return typeof(string);
		}

		protected override object CreateInstance(Type itemType)
		{
			return "";
		}
	}
}
