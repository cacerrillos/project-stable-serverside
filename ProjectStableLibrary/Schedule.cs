using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ProjectStableLibrary {
	[Table("schedule")]
	public class Schedule {
		[Key, Column(Order = 1)]
		public uint date {
			get;
			set;
		}
		[Key, Column(Order = 2)]
		public uint block_id {
			get;
			set;
		}
		[Key, Column(Order = 3)]
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
			
			return date == s.date && block_id == s.block_id && presentation_id == s.presentation_id;
		}

		public override int GetHashCode() {
			unchecked {
				int result = 37;

				result *= 397;
				result += date.GetHashCode();

				result *= 397;
				result += block_id.GetHashCode();

				result *= 397;
				result += presentation_id.GetHashCode();

				return result;
			}
		}
		public override string ToString() {
			return date + "_" + block_id + "_" + presentation_id;
		}
	}
}
