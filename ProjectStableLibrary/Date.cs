using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace ProjectStableLibrary {
	[Table("dates")]
	public class Date {
		[Key]
		public uint date {
			get;
			set;
		}
		public override string ToString() {
			return $"date: {date}";
		}

		public static async Task<List<Date>> GetDates(AmazonDynamoDBClient client) {
			List<Date> list = new List<Date>();

			var dates = await client.ScanAsync(new ScanRequest() {
				TableName = "endearing-otter-dates"
			});

			foreach(var date in dates.Items) {
				list.Add(new Date() {
					date = uint.Parse(date["date"].N)
				});
			}

			return list;
		}
	}
}
