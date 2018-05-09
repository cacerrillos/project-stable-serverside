using System;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;


using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System.Threading.Tasks;
using System.Threading;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace DynamoDBEntityTest {
	public class Function {
		private static readonly JsonSerializer _jsonSerializer = new JsonSerializer();

		public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context) {
			APIGatewayProxyResponse resp = new APIGatewayProxyResponse();

			var client = new AmazonDynamoDBClient();

			GetItemRequest getItemRequest = new GetItemRequest() {
				TableName = "endearing-otter-blocks",
				
			};
			var blocks =
			await client.ScanAsync(new ScanRequest() {
				TableName = "endearing-otter-blocks",
				AttributesToGet = {
					"block_id",
					"block_name"
				}
			});

			resp.Body = "";

			foreach(var item in blocks.Items) {
				resp.Body += $"Block {item["block_id"].N} - {item["block_name"].S}_-_";
			}

			var locations =
			await client.ScanAsync(new ScanRequest() {
				TableName = "endearing-otter-locations",
				AttributesToGet = {
					"location_id",
					"location_name"
				}
			});

			foreach(var item in locations.Items) {
				resp.Body += $"Location {item["location_id"].N} - {item["location_name"].S}_-_";
			}



			return resp;
		}
	}
}