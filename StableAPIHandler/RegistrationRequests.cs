using System;
using System.Collections.Generic;
using System.Text;

namespace StableAPIHandler {
	public struct PresentationStats {
		public uint count;
		public uint max;
		public PresentationStats(uint count, uint max) {
			this.count = count;
			this.max = max;
			full = count >= max;
		}
		public bool full;
	}
	public class RegistrationRequest {
		public uint viewer_id {
			get;
			set;
		}
		public string viewer_key {
			get;
			set;
		}
		public uint date {
			get;
			set;
		}
		public uint block_id {
			get;
			set;
		}
		public uint presentation_id {
			get;
			set;
		}
		public override string ToString() {
			return $"viewer_id: {viewer_id} viewer_key: {viewer_key} date: {date} block_id: {block_id} presentation_id: {presentation_id}";
		}
	}
	public class RegistrationResponse {
		public bool status;
		public List<ProjectStableLibrary.Registration> data;
		public Dictionary<uint, List<ProjectStableLibrary.Schedule>> full;
		public ViewerSavedError error = null;
	}
	public class ViewerSavedError {
		public uint code = 0;
		public string message = "";
	}
}
