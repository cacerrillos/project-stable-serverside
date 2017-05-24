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
		
		public static void run() {
            using(var dbCon = new MySqlConnection(Program.GetConStr())) {
                dbCon.Open();

                A(dbCon);

            }
        }
        static void A(MySqlConnection dbCon) {
            var data = new Dictionary<uint, Dictionary<uint, uint>>();
            var pres = new List<uint>();
            var presC = new Dictionary<uint, uint>();
            string q;

            q = "SELECT `viewer_id` FROM `viewers`;";
            using(var cmd = new MySqlCommand(q, dbCon)) {
                using(var r = cmd.ExecuteReader()) {
                    while(r.Read()) {
                        data.Add(r.GetUInt32("viewer_id"), new Dictionary<uint, uint>());
                    }
                }
            }

            Console.WriteLine($"Viewer count: {data.Count}");

            q = "SELECT `viewer_id`, `block_id`, `presentation_id` FROM `registrations`;";
            using(var cmd = new MySqlCommand(q, dbCon)) {
                using(var r = cmd.ExecuteReader()) {
                    while(r.Read()) {
                        data[r.GetUInt32("viewer_id")].Add(r.GetUInt32("block_id"), r.GetUInt32("presentation_id"));
                    }
                }
            }
            int regCount = data.Sum(thus => thus.Value.Count);
            int expected = data.Count * 8;

            Console.WriteLine($"Registration Count: {regCount} Expected: {expected} ({((double)regCount/expected)} %)");

            q = "SELECT `presentation_id` FROM `presentations`;";
            using(var cmd = new MySqlCommand(q, dbCon)) {
                using(var r = cmd.ExecuteReader()) {
                    while(r.Read()) {
                        pres.Add(r.GetUInt32("presentation_id"));
                    }
                }
            }

            
            Console.WriteLine("done");

            foreach(var p in pres) {
                presC.Add(p, Convert.ToUInt32(GetCount(dbCon, p)));
            }

            //foreach()

            foreach(var v in data.Where(thus => thus.Value.Count < 8)) {

            }
            
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
}