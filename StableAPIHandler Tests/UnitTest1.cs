using System;
using Amazon.Lambda.Core;
using NUnit;
using Amazon.Lambda.APIGatewayEvents;
using NUnit.Framework;
using System.Net;
using System.Collections.Generic;

namespace StableAPIHandler.Tests {
	[TestFixture]
	public class UnitTest1 {
		
		private LambdaContext lambdaContext;
		private Function f;
		[OneTimeSetUp]
		public void Init() {
			lambdaContext = new LambdaContext();
			f = new Function();
		}
		
		[Test]
		public void API_GET_STATUS() {
			var req = GET("/status");

			Environment.SetEnvironmentVariable("enabled", "true");
			TestAPICall(req, new StableAPIResponse() {
				StatusCode = HttpStatusCode.OK,
				Body = "true"
			});

			Environment.SetEnvironmentVariable("enabled", "false");
			TestAPICall(req, new StableAPIResponse() {
				StatusCode = HttpStatusCode.OK,
				Body = "false"
			});
		}
		private APIGatewayProxyRequest GET(string path) {
			return new APIGatewayProxyRequest() {
				Path = path,
				HttpMethod = "GET"
			};
		}
		private void TestAPICall(APIGatewayProxyRequest req, APIGatewayProxyResponse res) {
			var response = f.FunctionHandler(req, lambdaContext);
			LogResponse(response);
			Assert.That(response.StatusCode, Is.EqualTo(res.StatusCode));
			Assert.That(response.Body, Is.EqualTo(res.Body));
			Assert.That(response.Headers, Is.EquivalentTo(res.Headers));
		}
		private void LogResponse(APIGatewayProxyResponse response) {
			Console.WriteLine($"Status = {response.StatusCode}");
			string headers = "";
			foreach(var e in response.Headers) {
				headers += $"{{ \"{e.Key}\": \"{e.Value}\" }}" + Environment.NewLine;
			}
			Console.WriteLine($"Headers = {headers}");
			Console.WriteLine($"Body = {response.Body}");
		}
	}
}
