using System;

namespace ProjectStableLibrary {
	public struct Result {
		public bool status {
			get;
			set;
		}
		public string details {
			get;
			set;
		}
		public string trace {
			get;
			set;
		}
		public Result(Exception e) {
			status = false;
			if(e != null) {
				details = e.InnerException != null ? e.InnerException.Message : e.Message;
				trace = e.StackTrace;
			} else {
				details = "{}";
				trace = null;
			}
		}
	}
}
