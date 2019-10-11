using System;
using System.Collections.Generic;
using System.Text;

namespace Aga.Controls.Threading
{
	public enum WorkItemStatus 
	{ 
		Completed, 
		Queued, 
		Executing, 
		Aborted 
	}
}
