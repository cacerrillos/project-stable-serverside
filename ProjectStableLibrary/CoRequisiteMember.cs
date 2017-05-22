using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ProjectStableLibrary {
	[Table("corequisite_members")]
	public class CoRequisiteMember {
		public uint group_id {
			get;
			set;
		}
		public uint p_id {
			get;
			set;
		}
	}
}
