using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ProjectStableLibrary {
	[Table("corequisite_groups")]
	public class CoRequisiteGroup {
		[Key]
		public uint group_id {
			get;
			set;
		}
		public ICollection<CoRequisiteMember> Members {
			get;
			set;
		}
	}
}
