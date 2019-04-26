using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ProjectStableLibrary;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace ConsoleTools {
	public class Capstone2 {
		private static bool prioritizeByGrade = true;
		const uint FirstBlockID = 1;
		const uint FirstBlockAfterLunchID = 5;
		const uint PrepPresentationId = 167;
		const uint LocationId_MPR = 20;

		public static void run() {
			string s;
			char c;
			do {
				Console.Write("Save to db? (y/n/q): ");
				s = Console.ReadLine().ToUpper();
				if (s.Length > 0) {
					c = s[0];
					if (c == 'Y' || c == 'N')
						PlaceViewers(c == 'Y');
				} else {
					c = ' ';
				}
			} while (c != 'Q');
		}
		static void PlaceViewers(bool saveToDB) {
			var conStr = new MySqlConnectionStringBuilder();
			using (StreamReader file = File.OpenText("config.json")) {
				using (JsonTextReader r = new JsonTextReader(file)) {
					JObject conf = (JObject)JToken.ReadFrom(r);
					conStr.Server = (string)conf["server"];
					conStr.Port = uint.Parse((string)conf["port"]);
					conStr.UserID = (string)conf["user"];
					conStr.Password = (string)conf["password"];
					conStr.Database = (string)conf["database"];
					conStr.SslMode = MySqlSslMode.Required;
				}
			}

			bool clean = true;
			clean = false;

			using (StableContext ctx = StableContextFactory.Build(conStr.ToString())) {
				//3 2 4 1
				var viewers = ctx.Viewers;
				Console.WriteLine($"Fetched {viewers.Count} viewers");

				var schedule = ctx.Schedule;
				schedule = schedule.Where(thus => thus.presentation_id != PrepPresentationId).ToList(); //ignore prep

				var dates = ctx.Dates;
				Console.WriteLine($"Fetched {dates.Count} dates");


				var newRegistrations = new List<Registration>();

				var currentRegistrations = ctx.Registrations;

				var capacityCheck = new Dictionary<Schedule, int>();
				foreach (Schedule s in schedule) {
					capacityCheck.Add(s, currentRegistrations.Count( thus => thus.presentation_id == s.presentation_id));
				}

				Console.WriteLine($"Fetched {schedule.Count} schedules");

				var blocks = ctx.Blocks;
				Console.WriteLine($"Fetched {blocks.Count} blocks");

				// var preferences = ctx.preferences.ToList();
				// Console.WriteLine($"Fetched {preferences.Count} preferences");

				var presentations = ctx.Presentations.Where(thus => thus.Key < 142);

				// int expected_count = blocks.Count;
				const int retry_count = 5;

				var coreqs = ctx.CoRequisiteMembersList;



				DateTime start = DateTime.Now;

				//foreach schedule (date, block)
				//get viewers that aren't signed up for this block
				//loop over those viewrs, each time place them in the least filled presentation

				foreach(var date in dates) {
					foreach(var block in blocks) {
						Schedule s = new Schedule() {
							date = date.date,
							block_id = block.Key
						};

						if (date.date == 20190503 && block.Key > 2)
							continue;

						List<uint> unregistered = GetUnRegistered(date, block.Value, viewers, currentRegistrations, newRegistrations);

						unregistered.Randomize();

						int x = 0;
						foreach(var viewer in unregistered) {
							var pres = capacityCheck.Where(thus => thus.Key.date == date.date && thus.Key.block_id == block.Key).OrderBy(thus => thus.Value);

							bool added = false;
							foreach (var p in pres) {
								int current_size = capacityCheck[p.Key];

								int cap = p.Key.location_id == LocationId_MPR ? 38 : 32;

								if(current_size >= cap)
									continue;
								
								
								//attempt to add to

								CoRequisiteMember co = null; 
								if ((co = coreqs.FirstOrDefault(thus => thus.p_id == p.Key.presentation_id)) != null) {
									var otherPresentation = coreqs.First(thus => thus.group_id == co.group_id && thus.p_id != co.p_id);
									var otherSchedule = schedule.First(thus => thus.presentation_id == otherPresentation.p_id);

									if(currentRegistrations.Concat(newRegistrations).Any(thus => thus.date == otherSchedule.date && thus.block_id == otherSchedule.block_id && thus.viewer_id == viewer)) {
										continue;
										//Already registered in coreq's timeslot
									} else {
										newRegistrations.Add(new Registration() {
											date = date.date,
											block_id = block.Key,
											presentation_id = p.Key.presentation_id,
											viewer_id = viewer
										});

										capacityCheck[p.Key]++;

										newRegistrations.Add(new Registration() {
											date = otherSchedule.date,
											block_id = otherSchedule.block_id,
											presentation_id = otherSchedule.presentation_id,
											viewer_id = viewer
										});

										capacityCheck[otherSchedule]++;

									}




									added = true;
									break;
								}

								newRegistrations.Add(new Registration() {
									date = date.date,
									block_id = block.Key,
									presentation_id = p.Key.presentation_id,
									viewer_id = viewer
								});

								capacityCheck[p.Key]++;
								added = true;
								break;
							}

							if(!added) {
								Console.WriteLine($"Force Adding {s}");
								//force add
								var fPres = capacityCheck.Where(thus => thus.Key.date == date.date && thus.Key.block_id == block.Key).OrderBy(thus => thus.Value);
								foreach(var p in fPres) {

									CoRequisiteMember co = null;
									if ((co = coreqs.FirstOrDefault(thus => thus.p_id == p.Key.presentation_id)) != null) {
										var otherPresentation = coreqs.First(thus => thus.group_id == co.group_id && thus.p_id != co.p_id);
										var otherSchedule = schedule.First(thus => thus.presentation_id == otherPresentation.p_id);

										if (currentRegistrations.Concat(newRegistrations).Any(thus => thus.date == otherSchedule.date && thus.block_id == otherSchedule.block_id && thus.viewer_id == viewer)) {
											continue;
											//Already registered in coreq's timeslot
										} else {
											newRegistrations.Add(new Registration() {
												date = date.date,
												block_id = block.Key,
												presentation_id = p.Key.presentation_id,
												viewer_id = viewer
											});

											capacityCheck[p.Key]++;

											newRegistrations.Add(new Registration() {
												date = otherSchedule.date,
												block_id = otherSchedule.block_id,
												presentation_id = otherSchedule.presentation_id,
												viewer_id = viewer
											});

											capacityCheck[otherSchedule]++;

										}




										added = true;
										break;
									}

									newRegistrations.Add(new Registration() {
										date = date.date,
										block_id = block.Key,
										presentation_id = p.Key.presentation_id,
										viewer_id = viewer
									});

									capacityCheck[p.Key]++;

									break;
								}
							}
							x++;
						}
						x = 0;

					}
				}

				//sanity check
				foreach(var v in viewers.Values) {
					int vC = currentRegistrations.Concat(newRegistrations).Count(thus => thus.viewer_id == v.viewer_id);
					if (vC != 8) {
						Console.WriteLine($"Error, {v.viewer_id} {vC}");
					}



				}

				DateTime end = DateTime.Now;
				Console.WriteLine($"Took {(end - start).TotalMilliseconds} ms to sort");
				if (saveToDB) {
					start = DateTime.Now;

					if (clean && false) {
						using (var tx = ctx.Database.BeginTransaction()) {
							try {
								ctx.Database.ExecuteSqlCommand("DELETE FROM `registrations`;");
								tx.Commit();
							} catch (Exception e) {
								tx.Rollback();
								Console.WriteLine(e);
							}
						}
						
					}

					using (var tx = ctx.Database.BeginTransaction()) {
						try {
							ctx.registrations.AddRange(newRegistrations);
							ctx.SaveChanges();
							tx.Commit();
						} catch (Exception e) {
							tx.Rollback();
							Console.WriteLine(e);
						}
					}

					end = DateTime.Now;
					Console.WriteLine($"Took {(end - start).TotalMilliseconds} ms to add to db!");
				}

				foreach (var e in capacityCheck) {
					Console.WriteLine(e.Key.block_id + " " + e.Key.presentation_id + " " + e.Value);
				}
				if (!saveToDB)
					Console.WriteLine("Did not save to DB!");

				Console.WriteLine($"{newRegistrations.Count} entries to add to DB!");
			}
			Console.WriteLine("Clean: " + clean.ToString());
		}

		static List<uint> GetUnRegistered(Date date, Block block, Dictionary<uint, SanitizedViewer> viewers, List<Registration> registrations, List<Registration> newRegistrations) {
			var all = registrations.Concat(newRegistrations);

			var inThisSlot = all.Where(thus => thus.date == date.date && thus.block_id == block.block_id);

			return viewers.Keys.Where(thus => !inThisSlot.Any(thus2 => thus == thus2.viewer_id)).ToList();
		}

	}
}