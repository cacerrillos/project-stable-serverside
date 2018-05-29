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
	public class Program {
		public static void Main(String[] args) {
			Console.WriteLine("1) Capstone");
			Console.WriteLine("2) Capstone Old");
			Console.WriteLine("3) Career Day");
			Console.Write("Choice [1]: ");

			string s = Console.ReadLine();

			switch(s) {
				case "":
				case "1":
					Capstone2.run();
					break;
				case "2":
					Capstone.run();
					break;
				case "3":
					CareerDay.run();
					break;
				default:
					Console.WriteLine("Invalid option!");
					break;
			}


		}
		public static string GetConStr() {
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
			return conStr.ToString();
		}
	}
	public static class Rng {
		private static Random rng = new Random();  
		public static void Randomize<T>(this IList<T> list) {  
			int n = list.Count;  
			while (n > 1) {  
				n--;  
				int k = rng.Next(n + 1);  
				T value = list[k];  
				list[k] = list[n];  
				list[n] = value;  
			}  
		}
	}
}