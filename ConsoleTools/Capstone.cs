using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ProjectStableLibrary;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ConsoleTools {
	public class Capstone {
		const int Full = 9;
		public static void run() {
            using(var dbCon = new MySqlConnection(Program.GetConStr())) {
                dbCon.Open();

                A(dbCon);

            }
        }
        static void A(MySqlConnection dbCon) {
            var pres_grade = new Dictionary<uint, uint>();
            var data = new Dictionary<uint, Dictionary<string, uint>>();
            var pres = new List<uint>();
            var presC = new Dictionary<uint, uint>();
            var presBlock = new Dictionary<uint, uint>();
            string q;

            q = "SELECT `viewer_id`, `grade_id` FROM `viewers`;";
            using(var cmd = new MySqlCommand(q, dbCon)) {
                using(var r = cmd.ExecuteReader()) {
                    uint viewer_id;
                    while(r.Read()) {
                        viewer_id = r.GetUInt32("viewer_id");
                        data.Add(viewer_id, new Dictionary<string, uint>());
                        pres_grade.Add(viewer_id, r.GetUInt32("grade_id"));
                    }
                }
            }

            Console.WriteLine($"Viewer count: {data.Count}");

            q = "SELECT `viewer_id`,`date`, `block_id`, `presentation_id` FROM `registrations`;";
            using(var cmd = new MySqlCommand(q, dbCon)) {
                using(var r = cmd.ExecuteReader()) {
                    while(r.Read()) {
                        data[r.GetUInt32("viewer_id")].Add(r.GetUInt32("date") + "_" + r.GetUInt32("block_id"), r.GetUInt32("presentation_id"));
                    }
                }
            }
			
            int regCount = data.Sum(thus => thus.Value.Count);
            int expected = data.Count * Full;
            int partial = data.Where(thus => thus.Value.Count < Full).Count();

            Console.WriteLine($"Registration Count: {regCount} Expected: {expected} ({((double)regCount/expected)} %) Partial Count: {partial}");

            q = "SELECT `presentation_id`, `block_id` FROM `presentations`;";
            using(var cmd = new MySqlCommand(q, dbCon)) {
                using(var r = cmd.ExecuteReader()) {
                    uint pres_id;
                    while(r.Read()) {
                        pres_id = r.GetUInt32("presentation_id");
                        pres.Add(pres_id);
                        presBlock.Add(pres_id, r.GetUInt32("block_id"));
                    }
                }
            }

            
            Console.WriteLine("done");

            foreach(var p in pres) {
                presC.Add(p, Convert.ToUInt32(GetCount(dbCon, p)));
            }
            var toAdd = new List<Entry>();
            var toRemove = new List<Entry>();
			List<uint> toSkip = new List<uint>() {
				132,39,32,96,74,99
			};
            foreach(var g in pres_grade.OrderByDescending(thus => thus.Value)) {
                if(data[g.Key].Count < Full) {
                    //missing pres

                    for(uint x = 1; x <= 8; x++) {
                        if(!data[g.Key].ContainsKey("20180531" + "_" + x)) {
                            var pres_in_block = presBlock.Where(thus => thus.Value == x);
                            var pres_by_count = presC.Where(thus => pres_in_block.Any(t => thus.Key == t.Key) && thus.Value < 37).OrderBy(thus => thus.Value);
                            Console.WriteLine($"Block {x} Possible: " + string.Join(", ", pres_by_count.Select( thus => thus.Key +"C"+thus.Value)));
                            try {
                                uint p_to_add = pres_by_count.First().Key;

								if (toSkip.Contains(p_to_add)) {
									throw new Exception();
								}

								/*
                                if(p_to_add == 39 || p_to_add == 132) {
                                    presC[39]++;
                                    presC[132]++;
                                    toAdd.Add(new Entry() {
                                        date = 20180601,
                                        block_id = 2,
                                        viewer_id = g.Key,
                                        presentation_id = 132
                                    });
                                    
                                    toAdd.Add(new Entry() {
                                        date = 20180601,
                                        block_id = 1,
                                        viewer_id = g.Key,
                                        presentation_id = 39
                                    });
                                    if(!data[g.Key].ContainsKey(7))
                                        data[g.Key].Add(7, 77);
                                    else {
                                        presC[data[g.Key][7]]--;
                                        toRemove.Add(new Entry() {
                                            date = 20170601,
                                            block_id = 7,
                                            viewer_id = g.Key,
                                            presentation_id = data[g.Key][7]
                                        });
                                        data[g.Key][7] = 77;

                                    }

                                    if(!data[g.Key].ContainsKey(8))
                                        data[g.Key].Add(8, 78);
                                    else {
                                        presC[data[g.Key][8]]--;
                                        toRemove.Add(new Entry() {
                                            date = 20170601,
                                            block_id = 8,
                                            viewer_id = g.Key,
                                            presentation_id = data[g.Key][8]
                                        });
                                        data[g.Key][8] = 78;
                                    }
                                    continue;
                                }
                                if(p_to_add == 50 || p_to_add == 91) {
                                    presC[50]++;
                                    presC[91]++;
                                    toAdd.Add(new Entry() {
                                        date = 20170601,
                                        block_id = 2,
                                        viewer_id = g.Key,
                                        presentation_id = 50
                                    });
                                    toAdd.Add(new Entry() {
                                        date = 20170601,
                                        block_id = 1,
                                        viewer_id = g.Key,
                                        presentation_id = 91
                                    });
                                    if(!data[g.Key].ContainsKey(1))
                                        data[g.Key].Add(1, 91);
                                    else {
                                        presC[data[g.Key][1]]--;
                                        toRemove.Add(new Entry() {
                                            date = 20170601,
                                            block_id = 1,
                                            viewer_id = g.Key,
                                            presentation_id = data[g.Key][1]
                                        });
                                        data[g.Key][1] = 91;
                                    }

                                    if(!data[g.Key].ContainsKey(2))
                                        data[g.Key].Add(2, 50);
                                    else {
                                        presC[data[g.Key][2]]--;
                                        toRemove.Add(new Entry() {
                                            date = 20170601,
                                            block_id = 2,
                                            viewer_id = g.Key,
                                            presentation_id = data[g.Key][2]
                                        });
                                        data[g.Key][2] = 50;
                                    }
                                    continue;
                                }
								*/

                                //increas count
                                presC[p_to_add]++;
                                toAdd.Add(new Entry() {
                                    date = 20170601,
                                    block_id = x,
                                    viewer_id = g.Key,
                                    presentation_id = p_to_add
                                });
                                
                            } catch {
                                Console.WriteLine($"Warn! Empty!! block_id: {x}");
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Partial Reg: " + string.Join(", ", data.Where(thus => thus.Value.Count < 8).Select(thus => thus.Key)));

            //final
            foreach(var s in presC.OrderBy(thus => thus.Value).Select(thus => thus.Key +" " + thus.Value)) {
                Console.WriteLine(s);
            }
            string sqlD = "";
            foreach(var d in toRemove) {
                sqlD += $" DELETE from registrations where date = {d.date} and block_id = {d.block_id} and viewer_id = {d.viewer_id};";
            }
            Console.WriteLine("PAUSE");
            Console.ReadLine();

            string sql = "INSERT INTO `registrations` VALUES ";
            sql += string.Join(", ", toAdd.Select( thus => $"({thus.date},{thus.block_id},{thus.viewer_id},{thus.presentation_id})"));
            sql += ";";
            Console.WriteLine(sqlD);
            Console.WriteLine($"Registration Count: {regCount} Expected: {expected} ({((double)regCount/expected)} %) Partial Count: {partial} Final {regCount + toAdd.Count}");
            long r_count;
            long a_count;
            using(var cmd = new MySqlCommand(sql, dbCon)) {
                a_count = cmd.ExecuteNonQuery();
            }
            Console.WriteLine($"Affected rows {a_count}");
        }
        static long GetCount(MySqlConnection dbCon, uint p_id) {
            string q = "SELECT COUNT(`presentation_id`) FROM `registrations` WHERE `presentation_id` = @p_id;";
            
            using(var cmd = new MySqlCommand(q, dbCon)) {
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@p_id", p_id);
                return (long) cmd.ExecuteScalar();
            }
        }
	}
    struct Entry {
        public uint date {
            get;
            set;
        }
        public uint block_id {
            get;
            set;
        }
        public uint viewer_id {
            get;
            set;
        }
        public uint presentation_id {
            get;
            set;
        }
    }
}