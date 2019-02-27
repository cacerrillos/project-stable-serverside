using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ProjectStableLibrary {
	[Table("locations")]
	public class Location {
		[Key]
		public uint location_id {
			get;
			set;
		}
		[MaxLength(255)]
		public string location_name {
			get;
			set;
		}
		public override bool Equals(object obj) {
			if(obj.GetType() == typeof(Location)) {
				Location b = (Location)obj;
				return location_id == b.location_id && location_name == b.location_name;
			}
			return false;
		}

		public override int GetHashCode() {
			return HashCode.Combine(location_id, location_name);
		}
	}
}
