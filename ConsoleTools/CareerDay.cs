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
	public class CareerDay {
		public const int date = 20190315;
		private static bool prioritizeByGrade = true;
		public static void run() {
			bool careerDay = false;
			string s;
			char c;
			do {
				Console.Write("Save to db? (y/n/q): ");
				s = Console.ReadLine().ToUpper();
				if(s.Length > 0) {
					c = s[0];
					if(c == 'Y' || c == 'N')
						PlaceViewers(c == 'Y');
				} else {
					c = ' ';
				}
			} while(c != 'Q');
		}
		static void PlaceViewers(bool saveToDB) {
			var conStr = new MySqlConnectionStringBuilder();
			using(StreamReader file = File.OpenText("config.json")) {
				using(JsonTextReader r = new JsonTextReader(file)) {
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

			using(StableContext ctx = StableContextFactory.Build(conStr.ToString())) {
				//3 2 4 1
				var viewers = ctx.Viewers;
				Console.WriteLine($"Fetched {viewers.Count} viewers");

				var schedule = ctx.Schedule;

				var capacityCheck = new Dictionary<Schedule, uint>();
				foreach(Schedule s in schedule) {
					capacityCheck.Add(s, 0);
				}
				Console.WriteLine($"Fetched {schedule.Count} schedules");

				var blocks = ctx.Blocks;
				Console.WriteLine($"Fetched {blocks.Count} blocks");

				var preferences = ctx.preferences.ToList();
				Console.WriteLine($"Fetched {preferences.Count} preferences");

				var presentations = ctx.Presentations;

				Console.WriteLine($"Fetched {presentations.Count} presentations");

				int expected_count = blocks.Count;
				const int retry_count = 5;

				uint f_count = 0;
				var toAddToDB = new List<Registration>();
				{
					string s;
					char c;
					do {
						Console.Write("Prioritize by grade? (y/n): ");
						s = Console.ReadLine().ToUpper();
						if(s.Length > 0) {
							c = s[0];
							if(c == 'Y' || c == 'N') {
								prioritizeByGrade = c == 'Y';
								break;
							}
						} else {
							c = ' ';
						}
					} while(true);
				}

				uint[] grade_pri = new uint[] { 0 };
				if(prioritizeByGrade) {
					grade_pri = new uint[] { 3, 2, 4, 1 };
				}
				
				uint max_viewers = 26;

				do {
					try {
						Console.Write($"max_viewers [{max_viewers}]: ");
						string s = Console.ReadLine();
						if(s.Length == 0)
							break;
						
						max_viewers = uint.Parse(s);
						break;
					} catch {
						Console.WriteLine("Invalid input!");
					}
				} while(true);


				var registrationsToAdd = new List<Registration>();

				DateTime start = DateTime.Now;

				foreach(Block b in blocks.Values) {
					var temp_s = new Schedule() { date = date };
					temp_s.block_id = b.block_id;

					foreach(uint g in grade_pri) {
						var viewers_to_proc = new List<uint>();


						if(prioritizeByGrade)
							viewers_to_proc.AddRange(from thus in viewers where thus.Value.grade_id == g select thus.Value.viewer_id);
						else
							viewers_to_proc.AddRange(from thus in viewers select thus.Value.viewer_id);

						viewers_to_proc.Randomize();

						foreach(uint v in viewers_to_proc) {
							//var currentSelectedPresentations = new List<uint>();
							var v_pref = (from thus in preferences where thus.viewer_id == v orderby thus.order select thus.presentation_id).ToList();

							var currentRegistrations = (from thus in registrationsToAdd where thus.viewer_id == v select thus.presentation_id).ToList();

							bool randomize = v_pref.Count < presentations.Count;
							if(randomize) {
								Dictionary<uint, int> currentPCount = new Dictionary<uint, int>();
								foreach(var p_id in presentations.Keys) {
									currentPCount.Add(
										p_id,
										registrationsToAdd.Count(thus => thus.date == temp_s.date && thus.block_id == temp_s.block_id && thus.presentation_id == p_id)
									);
								}
								var popOrder = currentPCount.OrderBy(thus => thus.Value);
								v_pref = popOrder.Select(thus => thus.Key).ToList();
								//v_pref = (from thus in presentations select thus.Value.presentation_id).ToList();
							}

							

							//if(currentRegistrations.Count == 1)
								//Console.WriteLine();

							v_pref = v_pref.Where(thus => !currentRegistrations.Contains(thus)).ToList();

							var v_pref_queue = new Queue<uint>(v_pref);

							uint presentation_to_add;

							//Add capacity check
							int presentationViewerCount = 0;
							do {
								if(v_pref_queue.Count == 0) {
									f_count++;
									foreach(uint p_idd in presentations.Select(thus => thus.Value.presentation_id)) {
										var c2 = registrationsToAdd.Count(thus => thus.date == temp_s.date && thus.block_id == temp_s.block_id && thus.presentation_id == p_idd);

										//Console.WriteLine($"{p_idd}: {c2}");
									}

									v_pref = v_pref.Where(thus => !currentRegistrations.Contains(thus)).ToList();
									v_pref_queue = new Queue<uint>(v_pref);

									presentation_to_add = v_pref_queue.Dequeue();

									break;
								}
								presentation_to_add = v_pref_queue.Dequeue();
								
								presentationViewerCount = registrationsToAdd.Count(thus => thus.date == temp_s.date && thus.block_id == temp_s.block_id && thus.presentation_id == presentation_to_add);

								//if(presentationViewerCount >= max_viewers)
									//Console.WriteLine();

							} while(presentationViewerCount >= max_viewers);

							registrationsToAdd.Add(new Registration() {
								date = temp_s.date,
								block_id = temp_s.block_id,
								presentation_id = presentation_to_add,
								viewer_id = v
							});

						}
					}
				
					foreach(uint p_idd in presentations.Select(thus => thus.Value.presentation_id)) {
						var c2 = registrationsToAdd.Count(thus => thus.date == temp_s.date && thus.block_id == temp_s.block_id && thus.presentation_id == p_idd);

						Console.WriteLine($"{temp_s.block_id} {p_idd}: {c2}");
					}

				}

				clean = registrationsToAdd.Count == viewers.Count * blocks.Count;

				toAddToDB = registrationsToAdd;
				/*
				foreach(uint g in grade_pri) {
					// if(g != 3)
					// 	continue;
					var viewers_to_proc = new List<uint>();

					if(prioritizeByGrade)
						viewers_to_proc.AddRange(from thus in viewers where thus.Value.grade_id == g select thus.Value.viewer_id);
					else
						viewers_to_proc.AddRange(from thus in viewers select thus.Value.viewer_id);

					viewers_to_proc.Randomize();

					foreach(uint v in viewers_to_proc) {
						var registrationEntries = new List<Tuple<uint, uint>>();
						var currentSelectedPresentations = new List<uint>();
						var v_pref = (from thus in preferences where thus.viewer_id == v orderby thus.order select thus.presentation_id).ToList();
						
						bool randomize = v_pref.Count < presentations.Count;
						
						
						var temp_s = new Schedule(){ date = date };
						var blocks_r = blocks.Values.ToList();

						int error_count = 0;
						do {

							blocks_r.Randomize();
							foreach(Block b in blocks_r) {
								temp_s.block_id = b.block_id;

								if(randomize) {
									var r_p = from thus in capacityCheck orderby thus.Value select thus.Key.presentation_id;

									foreach(uint p in r_p) {
										if(currentSelectedPresentations.Contains(p))
											continue;
										temp_s.presentation_id = p;
										if(!capacityCheck.ContainsKey(temp_s))
											continue;
										if(capacityCheck[temp_s] >= max_viewers)
											continue;
										capacityCheck[temp_s]++;
										registrationEntries.Add(new Tuple<uint, uint>(b.block_id, p));
										currentSelectedPresentations.Add(p);
										break;
									}
									continue;
								}

								foreach(uint p in v_pref) {
									if(currentSelectedPresentations.Contains(p))
										continue;
									temp_s.presentation_id = p;
									if(!capacityCheck.ContainsKey(temp_s))
										continue;
									if(capacityCheck[temp_s] >= max_viewers)
										continue;
									capacityCheck[temp_s]++;
									registrationEntries.Add(new Tuple<uint, uint>(b.block_id, p));
									currentSelectedPresentations.Add(p);
									break;
								}

							}
							if(currentSelectedPresentations.Count != expected_count) {
								//Console.WriteLine("ERROR " + string.Join(", ", bleh_v));

								//undo
								foreach(var toRemove in registrationEntries) {
									temp_s.block_id = toRemove.Item1;
									temp_s.presentation_id = toRemove.Item2;
									capacityCheck[temp_s]--;
								}
								registrationEntries.Clear();
								currentSelectedPresentations.Clear();
								error_count++;
								if(error_count > retry_count) {
									clean = false;
									break;
								}

							} else {
								foreach(var toAdd in registrationEntries) {
									toAddToDB.Add(new Registration() {
										date = temp_s.date,
										block_id = toAdd.Item1,
										presentation_id = toAdd.Item2,
										viewer_id = v
									});
								}
							}
						} while(currentSelectedPresentations.Count != expected_count);
						
						//var rng = randomize ? "RNG" : "";
						//Console.WriteLine($"Student: {v} {rng} {string.Join(", ", bleh)}");
					}

					
				}
				*/

				DateTime end = DateTime.Now;
				Console.WriteLine($"Took {(end - start).TotalMilliseconds} ms to sort");
				Console.WriteLine($"F Count: {f_count}");
				if(saveToDB) {
					start = DateTime.Now;

					if(clean) {
						using(var tx = ctx.Database.BeginTransaction()) {
							try {
								ctx.Database.ExecuteSqlCommand("DELETE FROM `registrations`;");
								tx.Commit();
							} catch(Exception e) {
								tx.Rollback();
								Console.WriteLine(e);
							}
						}
						using(var tx = ctx.Database.BeginTransaction()) {
							try {
								ctx.registrations.AddRange(toAddToDB);
								ctx.SaveChanges();
								tx.Commit();
							} catch(Exception e) {
								tx.Rollback();
								Console.WriteLine(e);
							}
						}
					}

					end = DateTime.Now;
					Console.WriteLine($"Took {(end - start).TotalMilliseconds} ms to add to db!");
				}

				foreach(var e in capacityCheck) {
					Console.WriteLine(e.Key.block_id + " " + e.Key.presentation_id + " " + e.Value);
				}
				if(!saveToDB)
					Console.WriteLine("Did not save to DB!");
				
				Console.WriteLine($"{toAddToDB.Count} entries to add to DB!");
			}
			Console.WriteLine("Clean: " + clean.ToString());
		}
		
	}
}