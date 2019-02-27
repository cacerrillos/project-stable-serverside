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
		public const int date = 20180316;
		private static bool prioritizeByGrade = true;
		public static void run() {
			bool careerDay = false;
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

				var schedule = ctx.Schedule.Where(thus => thus.presentation_id < 142).ToList(); //ignore prep

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


				if(false) {
					string s;
					char c;
					do {
						Console.Write("Prioritize by grade? (y/n): ");
						s = Console.ReadLine().ToUpper();
						if (s.Length > 0) {
							c = s[0];
							if (c == 'Y' || c == 'N') {
								prioritizeByGrade = c == 'Y';
								break;
							}
						} else {
							c = ' ';
						}
					} while (true);
				}

				if (false) {
					uint max_viewers = 36;

					do {
						try {
							Console.Write($"max_viewers [{max_viewers}]: ");
							string s = Console.ReadLine();
							if (s.Length == 0)
								break;

							max_viewers = uint.Parse(s);
							break;
						} catch {
							Console.WriteLine("Invalid input!");
						}
					} while (true);
				}



				DateTime start = DateTime.Now;

				//foreach schedule (date, block)
				//get viewers that aren't signed up for this block
				//loop over those viewrs, each time place them in the least filled presentation
				List<uint> mpr = new List<uint>() {
					36,64,74,99,100,115,120,122,131,133,148
				};

				foreach(var date in dates) {
					foreach(var block in blocks) {
						Schedule s = new Schedule() {
							date = date.date,
							block_id = block.Key
						};

						if (date.date == 20180601 && block.Key > 2)
							continue;

						List<uint> unregistered = GetUnRegistered(date, block.Value, viewers, currentRegistrations, newRegistrations);

						unregistered.Randomize();

						int x = 0;
						foreach(var viewer in unregistered) {
							var pres = capacityCheck.Where(thus => thus.Key.date == date.date && thus.Key.block_id == block.Key).OrderBy(thus => thus.Value);

							bool added = false;
							foreach (var p in pres) {
								int size = capacityCheck[p.Key];

								if (mpr.Contains(p.Key.presentation_id)) {
									if (capacityCheck[p.Key] >= 80)
										continue;
								} else {
									int cap = (s.block_id == 7 || s.block_id == 8) ? 48 : 36;
									if (capacityCheck[p.Key] >= cap)
										continue;
								}
								
								
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
				/*
				foreach (uint g in grade_pri) {
					// if(g != 3)
					// 	continue;
					var viewers_to_proc = new List<uint>();

					if (prioritizeByGrade)
						viewers_to_proc.AddRange(from thus in viewers where thus.Value.grade_id == g select thus.Value.viewer_id);
					else
						viewers_to_proc.AddRange(from thus in viewers select thus.Value.viewer_id);

					viewers_to_proc.Randomize();

					foreach (uint v in viewers_to_proc) {
						var bleh = new List<Tuple<uint, uint>>();
						var bleh_v = new List<uint>();
						var v_pref = (from thus in preferences where thus.viewer_id == v orderby thus.order select thus.presentation_id).ToList();

						bool randomize = v_pref.Count < presentations.Count;


						var temp_s = new Schedule() { date = date };
						var blocks_r = blocks.Values.ToList();

						int error_count = 0;
						do {

							blocks_r.Randomize();
							foreach (Block b in blocks_r) {
								temp_s.block_id = b.block_id;

								if (randomize) {
									var r_p = from thus in capacityCheck orderby thus.Value select thus.Key.presentation_id;

									foreach (uint p in r_p) {
										if (bleh_v.Contains(p))
											continue;
										temp_s.presentation_id = p;
										if (!capacityCheck.ContainsKey(temp_s))
											continue;
										if (capacityCheck[temp_s] >= max_viewers)
											continue;
										capacityCheck[temp_s]++;
										bleh.Add(new Tuple<uint, uint>(b.block_id, p));
										bleh_v.Add(p);
										break;
									}
									continue;
								}

								foreach (uint p in v_pref) {
									if (bleh_v.Contains(p))
										continue;
									temp_s.presentation_id = p;
									if (!capacityCheck.ContainsKey(temp_s))
										continue;
									if (capacityCheck[temp_s] >= max_viewers)
										continue;
									capacityCheck[temp_s]++;
									bleh.Add(new Tuple<uint, uint>(b.block_id, p));
									bleh_v.Add(p);
									break;
								}

							}
							if (bleh_v.Count != expected_count) {
								//Console.WriteLine("ERROR " + string.Join(", ", bleh_v));

								//undo
								foreach (var toRemove in bleh) {
									temp_s.block_id = toRemove.Item1;
									temp_s.presentation_id = toRemove.Item2;
									capacityCheck[temp_s]--;
								}
								bleh.Clear();
								bleh_v.Clear();
								error_count++;
								if (error_count > retry_count) {
									clean = false;
									break;
								}

							} else {
								foreach (var toAdd in bleh) {
									newRegistrations.Add(new Registration() {
										date = temp_s.date,
										block_id = toAdd.Item1,
										presentation_id = toAdd.Item2,
										viewer_id = v
									});
								}
							}
						} while (bleh_v.Count != expected_count);

						//var rng = randomize ? "RNG" : "";
						//Console.WriteLine($"Student: {v} {rng} {string.Join(", ", bleh)}");
					}


				}
				*/

				//sanity check
				foreach(var v in viewers.Values) {
					int vC = currentRegistrations.Concat(newRegistrations).Count(thus => thus.viewer_id == v.viewer_id);
					if (vC != 10) {
						Console.WriteLine($"Error, {v.viewer_id} {vC}");
					}



				}

				DateTime end = DateTime.Now;
				Console.WriteLine($"Took {(end - start).TotalMilliseconds} ms to sort");
				if (saveToDB) {
					start = DateTime.Now;

					if (clean) {
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