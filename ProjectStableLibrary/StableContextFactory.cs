using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ProjectStableLibrary {
	public class StableContextFactory {
		public static StableContext Build(string conStr) {
			var optionsBuilder = new DbContextOptionsBuilder<StableContext>();
			//optionsBuilder.
			optionsBuilder.UseMySQL(conStr);

			return new StableContext(optionsBuilder.Options);
		}
	}
	public class StableContext : DbContext {
		public StableContext(DbContextOptions<StableContext> options) : base(options) {
			//Model.
		}
		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			modelBuilder.Entity<Preference>()
				.HasKey(c => new {
					c.viewer_id,
					c.order
				});
			modelBuilder.Entity<Schedule>()
				.HasKey(c => new {
					c.date,
					c.block_id,
					c.presentation_id
				});
			modelBuilder.Entity<Registration>()
				.HasKey(c => new {
					c.date,
					c.block_id,
					c.viewer_id
				});

			modelBuilder.Entity<CoRequisiteGroup>()
				.ToTable("corequisite_groups");

			modelBuilder.Entity<CoRequisiteMember>()
				.ToTable("corequisite_members");

			modelBuilder.Entity<CoRequisiteMember>()
				.HasKey(c => new {
					c.group_id,
					c.p_id
				});

		}

		public DbSet<Date> dates {
			get;
			set;
		}
		public List<Date> Dates {
			get {
				return dates.AsNoTracking().ToList();
			}
		}
		public DbSet<Block> blocks {
			get;
			set;
		}
		public Dictionary<uint, Block> Blocks {
			get {
				Dictionary<uint, Block> blocks = new Dictionary<uint, Block>();
				foreach(Block b in this.blocks.AsNoTracking()) {
					blocks.Add(b.block_id, b);
				}

				return blocks;
			}
		}
		public DbSet<Grade> grades {
			get;
			set;
		}
		public Dictionary<uint, Grade> Grades {
			get {
				Dictionary<uint, Grade> grades = new Dictionary<uint, Grade>();
				foreach(Grade g in this.grades.AsNoTracking()) {
					grades.Add(g.grade_id, g);
				}

				return grades;
			}
		}
		public DbSet<House> houses {
			get;
			set;
		}
		public Dictionary<uint, House> Houses {
			get {
				Dictionary<uint, House> houses = new Dictionary<uint, House>();
				foreach(House h in this.houses.AsNoTracking()) {
					houses.Add(h.house_id, h);
				}

				return houses;
			}
		}
		public DbSet<Location> locations {
			get;
			set;
		}
		public Dictionary<uint, Location> Locations {
			get {
				Dictionary<uint, Location> locations = new Dictionary<uint, Location>();
				foreach(Location l in this.locations.AsNoTracking()) {
					locations.Add(l.location_id, l);
				}

				return locations;
			}
		}
		public DbSet<Presentation> presentations {
			get;
			set;
		}
		public Dictionary<uint, Presentation> Presentations {
			get {
				Dictionary<uint, Presentation> presentations = new Dictionary<uint, Presentation>();
				foreach(Presentation p in this.presentations.AsNoTracking()) {
					presentations.Add(p.presentation_id, p);
				}

				return presentations;
			}
		}
		public DbSet<Viewer> viewers {
			get;
			set;
		}
		public Dictionary<uint, SanitizedViewer> Viewers {
			get {
				Dictionary<uint, SanitizedViewer> viewers = new Dictionary<uint, SanitizedViewer>();
				foreach(Viewer v in this.viewers.AsNoTracking()) {
					viewers.Add(v.viewer_id, v.Sanitize());
				}

				return viewers;
			}
		}
		public DbSet<Preference> preferences {
			get;
			set;
		}
		public Dictionary<uint, Dictionary<uint, List<uint>>> GetPreferences(uint viewer_id) {
			var data = new Dictionary<uint, Dictionary<uint, List<uint>>>();
			var fetchedDates = dates.AsNoTracking().ToList();
			var fetchedBlocks = blocks.AsNoTracking().ToList();
			foreach(Date date in fetchedDates) {
				foreach(Block block in fetchedBlocks) {
					if(!data.ContainsKey(date.date))
						data.Add(date.date, new Dictionary<uint, List<uint>>());

					data[date.date].Add(block.block_id, GetPreferences(viewer_id, date.date, block.block_id));
				}
			}

			return data;
		}
		public List<uint> GetPreferences(uint viewer_id, uint date, uint block_id) {
			var subset = from thus in preferences.AsNoTracking()
						 where thus.viewer_id == viewer_id
						 orderby thus.order
						 select thus.presentation_id;

			return subset.ToList();
		}
		public DbSet<Schedule> schedule {
			get;
			set;
		}
		public List<Schedule> Schedule {
			get {
				var set  = from thus in schedule.AsNoTracking()
							orderby thus.date, thus.block_id select thus;
				
				return set.ToList();
			}
		}
		public DbSet<Registration> registrations {
			get;
			set;
		}
		public List<Registration> Registrations {
			get {
				var registrations = new List<Registration>();

				foreach (var r in this.registrations.AsNoTracking()) {
					registrations.Add(r);
				}

				return registrations;
			}
		}
		public Dictionary<uint, List<Schedule>> FullPresentations {
			get {
				var full = new Dictionary<uint, List<Schedule>>();
				var viewers = Viewers;
				var grades = this.grades.AsNoTracking().ToList();
				var sche = Schedule;
				var reg = registrations.AsNoTracking().ToList();
				
				foreach(Grade g in grades.AsEnumerable()) {
					full.Add(g.grade_id, new List<Schedule>());
					var viewerSubset = viewers.Where(thus => thus.Value.grade_id == g.grade_id);
					foreach(var s in sche) {
						int c = reg.Count(thus => thus.Schedule().Equals(s) && viewerSubset.Any(t => t.Value.viewer_id == thus.viewer_id));
						var presentation = presentations.AsNoTracking().First(thus => thus.presentation_id == s.presentation_id);

						int gradeMax = (s.block_id == 7 || s.block_id == 8) ? 12 : 9;
						int allMax = (s.block_id == 7 || s.block_id == 8) ? 60 : 50;

						if(presentation.location_id == 20) {
							if(reg.Count(thus => thus.Schedule().Equals(s)) >= allMax) {
								full[g.grade_id].Add(s);
							}
						} else {
							if(c >= gradeMax) {
								full[g.grade_id].Add(s);
							}
						}
						
					}
				}

				return full;
			}
		}
		public DbSet<CoRequisiteGroup> CoRequisiteGroups {
			get;
			set;
		}
		public DbSet<CoRequisiteMember> CoRequisiteMembers {
			get;
			set;
		}

		public List<CoRequisiteMember> CoRequisiteMembersList {
			get {
				var result = new List<CoRequisiteMember>();

				foreach (var c in CoRequisiteMembers.AsNoTracking()) {
					result.Add(c);
				}

				return result;
			}
		}
	}
}
