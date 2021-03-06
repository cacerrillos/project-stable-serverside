
using System.Collections.Generic;

namespace StableAPIHandler {
	public class SignupRequest {
		public string first_name {
			get;
			set;
		}
		public string last_name {
			get;
			set;
		}
		public uint house {
			get;
			set;
		}
		public uint grade {
			get;
			set;
		}
		public int reserved {
			get;
			set;
		}

		public string version {
			get;
			set;
		}

		public string email {
			get;
			set;
		}
		public string password {
			get;
			set;
		}
		public bool? resume {
			get;
			set;
		}
		public void TrimAll() {
			email = email.Trim();
			first_name = first_name.Trim();
			last_name = last_name.Trim();
		}
	}
	public class SignupResponse : ProjectStableLibrary.Viewer {
		public SignupResponse(ProjectStableLibrary.Viewer v) {
			viewer_id = v.viewer_id;
			viewer_key = v.viewer_key;
			first_name = v.first_name;
			last_name = v.last_name;
			house_id = v.house_id;
			grade_id = v.grade_id;
			reserved = v.reserved;
		}
		public bool status {
			get;
			set;
		}
	}
	public class SignupErrorResponse {
		public SignupErrorResponse() {
			status = false;
		}
		public SignupErrorResponse(int code) {
			this.status = false;
			this.code = code;
		}
		public bool status {
			get;
			set;
		}
		public int code {
			get;
			set;
		}
		public string Message;
		public string trace;
		public List<ProjectStableLibrary.Registration> data;
		public Dictionary<uint, List<ProjectStableLibrary.Schedule>> full;
	}
	public class FinishSignupRequest {
		public uint viewer_id {
			get;
			set;
		}
		public string viewer_key {
			get;
			set;
		}
		public List<uint> data {
			get;
			set;
		}
		public bool status {
			get;
			set;
		}
	}
}
