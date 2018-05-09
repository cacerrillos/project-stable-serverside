using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectStableLibrary {
	[Table("grades")]
	public class Grade {
		[Key]
		public uint grade_id {
			get;
			set;
		}
		[MaxLength(255)]
		public string grade_name {
			get;
			set;
		}
		public uint default_amount {
			get;
			set;
		}

		public static async Task<Dictionary<uint, Grade>> GetGrades(AmazonDynamoDBClient client) {
			Dictionary<uint, Grade> response = new Dictionary<uint, Grade>();

			var grades = await client.ScanAsync(new ScanRequest() {
				TableName = "endearing-otter-grades"
			});

			foreach(var grade in grades.Items) {
				var g = new Grade() {
					grade_id = uint.Parse(grade["grade_id"].N),
					grade_name = grade["grade_name"].S,
					default_amount = uint.Parse(grade["default_amount"].N)
				};

				response.Add(g.grade_id, g);
			}
			

			return response;
		}
	}
}
