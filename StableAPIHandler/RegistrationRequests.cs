using System.Collections.Generic;
using System.Linq;
using ProjectStableLibrary;
using Microsoft.EntityFrameworkCore;
using System;

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
		public static RegistrationRequest FromPresentation(StableContext ctx,  uint p_id, uint v_id, string v_key) {
			var p = ctx.presentations.AsNoTracking().First(thus => thus.presentation_id == p_id);
			var p_s = ctx.schedule.AsNoTracking().First(thus => thus.presentation_id == p.presentation_id);
			
			return new RegistrationRequest() {
				date = p_s.date,
				block_id = p_s.block_id,
				presentation_id = p_id,
				viewer_id = v_id,
				viewer_key = v_key
			};
		}
	}
	public class RegistrationResponse {
		public bool status;
		public List<ProjectStableLibrary.Registration> data;
		public Dictionary<uint, List<Schedule>> full;
		public ViewerSavedError error = null;
	}
	public class ViewerSavedError {
		public uint code = 0;
		public string message = "";
	}
}
