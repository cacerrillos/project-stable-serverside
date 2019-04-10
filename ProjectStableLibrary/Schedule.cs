using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ProjectStableLibrary {
	[Table("schedule")]
	public class Schedule {
		[Key, Column(Order = 1)]
		public uint location_id {
			get;
			set;
		}
		[Key, Column(Order = 2)]
		public uint date {
			get;
			set;
		}
		[Key, Column(Order = 3)]
		public uint block_id {
			get;
			set;
		}
		[Key, Column(Order = 4)]
		public uint presentation_id {
			get;
			set;
		}
		public override bool Equals(object o) {
			if(o == null)
				return false;
			
			var s = o as Schedule;
			if(s == null)
				return false;
			
			return location_id == s.location_id && date == s.date && block_id == s.block_id && presentation_id == s.presentation_id;
		}

		public override int GetHashCode() {
			return HashCode.Combine(location_id, date, block_id, presentation_id);
		}

		public override string ToString() {
			return location_id + "_" + date + "_" + block_id + "_" + presentation_id;
		}
	}
}
