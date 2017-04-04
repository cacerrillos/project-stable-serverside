﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ProjectStableLibrary {
	[Table("preferences")]
	public class Preference {
		[Key, Column(Order = 1)]
		public uint viewer_id {
			get;
			set;
		}
		[Key, Column(Order = 2)]
		public uint order {
			get;
			set;
		}
		public uint presentation_id {
			get;
			set;
		}
		// public DateTime timestamp {
		// 	get;
		// 	set;
		// }
	}
}
