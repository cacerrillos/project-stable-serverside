using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace StableAPIHandler.Tests {
	public class LambdaLogger : ILambdaLogger {
		public void Log(string message) {
			Console.WriteLine(message);
		}

		public void LogLine(string message) {
			Console.WriteLine(message + Environment.NewLine);
		}
	}
}
