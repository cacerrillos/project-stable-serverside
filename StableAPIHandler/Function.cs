using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using ProjectStableLibrary;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace StableAPIHandler {
	public class Function {

		/// <summary>
		/// A simple function that takes a string and does a ToUpper
		/// </summary>
		/// <param name="input"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		///
		ILambdaLogger Logger;
		const uint FirstBlockID = 1;
		const uint FirstBlockAfterLunchID = 5;
		const uint PrepPresentationId = 167;
		public static string GetEnvironmentVariable(string variable) {
			try {
				string r = Environment.GetEnvironmentVariable(variable);
				if(r == null)
					throw new Exception();
				return r;
			} catch(Exception) {
				switch(variable) {
					case "enabled":
						return "true";
					case "freeforall":
						return "true";
					case "SITE_DOMAIN":
						return "*";
					case "admin_code":
						return "admin_code";
					default:
						return "";
				}
			}
		}
		public APIGatewayProxyResponse TryFunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context) {
			APIGatewayProxyResponse resp;
			try {
				context.Logger.LogLine("Request: " + Environment.NewLine + str(apigProxyEvent));
				resp = FunctionHandler(apigProxyEvent, context);
			} catch(Exception e) {
				context.Logger.LogLine(e.Message + Environment.NewLine + e.StackTrace);
				resp = StableAPIResponse.InternalServerError(e);
			}
			context.Logger.LogLine("Response: " + Environment.NewLine + str(resp));

			return resp;
		}
		public static string str(APIGatewayProxyRequest r) {
			if(r.Path.Contains("signup") && !r.Path.Contains("signup/finish")) {
				if(r.Body.Contains("password")) {
					try {
						SignupRequest sr = JsonConvert.DeserializeObject<SignupRequest>(r.Body);
						sr.password = "***";

						return $"Method: {r.HttpMethod}" + Environment.NewLine +
						$"Path: {r.Path}" + Environment.NewLine +
						$"Body: {JsonConvert.SerializeObject(sr)}";
					} catch {
						return $"Method: {r.HttpMethod}" + Environment.NewLine +
						$"Path: {r.Path}" + Environment.NewLine +
						$"Body: PRIVATE";
					}
				}
			}
			return $"Method: {r.HttpMethod}" + Environment.NewLine +
				$"Path: {r.Path}" + Environment.NewLine +
				$"Body: {r.Body}";
		}
		public static string str(APIGatewayProxyResponse r) {
			return $"Status: {r.StatusCode}" + Environment.NewLine +
				$"Body: {r.Body}";
		}
		Dictionary<uint, DateTime> enableTimes = new Dictionary<uint, DateTime>() {
			{4, new DateTime(2019,04,10,20,00,00, DateTimeKind.Utc) },
			{3, new DateTime(2019,04,11,20,00,00, DateTimeKind.Utc) },
			{2, new DateTime(2019,04,12,20,00,00, DateTimeKind.Utc) },
			{1, new DateTime(2019,04,12,20,00,00, DateTimeKind.Utc) },
		};
		bool CheckEnabled(uint grade = 0) {
			bool enabled = bool.Parse(GetEnvironmentVariable("enabled"));

			if(grade == 0)
				return enabled;

			DateTime now = DateTime.UtcNow;

			if(!enableTimes.ContainsKey(grade))
				return false;

			DateTime enableTime = enableTimes[grade];

			return enabled && now >= enableTime;
		}
		APIGatewayProxyResponse noSignups = new APIGatewayProxyResponse() {
			Body = "{}",
			Headers = new Dictionary<string, string>() { { "access-control-allow-origin", GetEnvironmentVariable("SITE_DOMAIN") } },
			StatusCode = 418
		};
		public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context) {
			Logger = context.Logger;
			object resultObject = new object();
			int resultCode = 405;

			
			bool enabled = bool.Parse(GetEnvironmentVariable("enabled"));

			bool freeforall = false;
			try {
				freeforall = bool.Parse(GetEnvironmentVariable("freeforall"));
			} catch(Exception e) {
				Logger.LogLine("Warning! " + e.Message);
			}



			//Pre check the request path to save time
			if(apigProxyEvent.Path.Contains("/api") && apigProxyEvent.Path.Length > 4) {
				apigProxyEvent.Path = apigProxyEvent.Path.Substring(4, apigProxyEvent.Path.Length - 4);
			}
			switch(apigProxyEvent.Path.ToLower()) {
				case "/":
					return new StableAPIResponse {
						Body = "What are you doing here?",
						StatusCode = HttpStatusCode.OK
					};
				case "/enabletimes":
				case "/enabletimes/":
					return new StableAPIResponse {
						Body = JsonConvert.SerializeObject(enableTimes.AsEnumerable().Append(new KeyValuePair<uint, DateTime>(0, DateTime.UtcNow)))
						+ Environment.NewLine + CheckEnabled(1).ToString()
						+ Environment.NewLine + CheckEnabled(2).ToString()
						+ Environment.NewLine + CheckEnabled(3).ToString()
						+ Environment.NewLine + CheckEnabled(4).ToString(),
						StatusCode = HttpStatusCode.OK
					};
				case "/status":
					if(apigProxyEvent.HttpMethod == "GET") {
						return new StableAPIResponse {
							Body = JsonConvert.SerializeObject(enabled),
							StatusCode = HttpStatusCode.OK
						};
					}
					return new StableAPIResponse {
						Body = "{}",
						StatusCode = HttpStatusCode.NotFound
					};

				case "/dates":
				case "/dates/":
				case "/blocks":
				case "/blocks/":
				case "/grades":
				case "/grades/":
				case "/houses":
				case "/houses/":
				case "/locations":
				case "/locations/":
				case "/presentations":
				case "/presentations/":
				case "/viewers":
				case "/viewers/":
				case "/schedule":
				case "/schedule/":
				case "/print":
				case "/print/":
				case "/full":
				case "/full/":
					break;

				case "/signup":
				case "/signup/":
				case "/register":
				case "/register/":
					break;

				case "/signup/finish":
				case "/signup/finish/":
					break;

				case "/preferences":
				case "/preferences/":
					break;

				default:
					return new StableAPIResponse {
						Body = "{}",
						StatusCode = HttpStatusCode.NotFound
					};
			}

			StableAPIResponse response = new StableAPIResponse {
				Body = "{}",
				StatusCode = HttpStatusCode.NotFound
			};

			string conStr = new MySqlConnectionStringBuilder() {
				Server = GetEnvironmentVariable("DB_ADDRESS"),
				Port = uint.Parse(GetEnvironmentVariable("DB_PORT")),
				UserID = GetEnvironmentVariable("DB_USER"),
				Password = GetEnvironmentVariable("DB_PASSWORD"),
				Database = GetEnvironmentVariable("DB_NAME"),
				SslMode = MySqlSslMode.Required
			}.ToString();
			using(StableContext ctx = StableContextFactory.Build(conStr)) {
				switch(apigProxyEvent.HttpMethod) {
					case "GET":
						#region GETs
						switch(apigProxyEvent.Path.ToLower()) {
							case "/":
								resultObject = "What are you doing here?";
								resultCode = 200;
								break;

							case "/dates":
							case "/dates/":
								response = new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(ctx.Dates),
									StatusCode = HttpStatusCode.OK
								};
								break;

							case "/blocks":
							case "/blocks/":
								response = new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(ctx.Blocks),
									StatusCode = HttpStatusCode.OK
								};
								break;

							case "/grades":
							case "/grades/":
								response = new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(ctx.Grades),
									StatusCode = HttpStatusCode.OK
								};
								break;

							case "/houses":
							case "/houses/":
								response = new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(ctx.Houses),
									StatusCode = HttpStatusCode.OK
								};
								break;

							case "/locations":
							case "/locations/":
								response = new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(ctx.Locations),
									StatusCode = HttpStatusCode.OK
								};
								break;

							case "/presentations":
							case "/presentations/":
								response = new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(ctx.Presentations),
									StatusCode = HttpStatusCode.OK
								};
								break;

							case "/viewers":
							case "/viewers/":
								if(apigProxyEvent.QueryStringParameters != null) {
									if(apigProxyEvent.QueryStringParameters.Count(thus => thus.Key == "date" || thus.Key == "block_id" || thus.Key == "presentation_id") == 3) {
										try {
											uint date = uint.Parse(apigProxyEvent.QueryStringParameters["date"]);
											uint block_id = uint.Parse(apigProxyEvent.QueryStringParameters["block_id"]);
											uint presentation_id = uint.Parse(apigProxyEvent.QueryStringParameters["presentation_id"]);

											var regs = ctx.registrations.AsNoTracking().Where(thus => thus.date == date && thus.block_id == block_id && thus.presentation_id == presentation_id).ToList();
											var viewers = ctx.viewers.AsNoTracking().ToList();
											var result = new List<SanitizedViewer>();
											foreach(var r in regs) {
												result.Add(viewers.Find(thus => thus.viewer_id == r.viewer_id).Sanitize());
											}
											response = new StableAPIResponse() {
												Body = JsonConvert.SerializeObject(result),
												StatusCode = HttpStatusCode.OK
											};
										} catch(Exception e) {
											Logger.LogLine(e.ToString());
											response = new StableAPIResponse() {
												Body = JsonConvert.SerializeObject(new Result(e)),
												StatusCode = HttpStatusCode.BadRequest
											};
										}
										break;
									} else if(apigProxyEvent.QueryStringParameters.ContainsKey("viewer_id")) {
										try {
											uint viewer_id = uint.Parse(apigProxyEvent.QueryStringParameters["viewer_id"]);
											response = new StableAPIResponse() {
												Body = JsonConvert.SerializeObject(ctx.viewers.AsNoTracking().First(thus => thus.viewer_id == viewer_id)),
												StatusCode = HttpStatusCode.OK
											};
										} catch(Exception e) {
											Logger.LogLine(e.ToString());
											response = new StableAPIResponse() {
												Body = JsonConvert.SerializeObject(new Result(e)),
												StatusCode = HttpStatusCode.BadRequest
											};
										}
										break;
									}
								}
								response = new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(ctx.Viewers),
									StatusCode = HttpStatusCode.OK
								};
								break;

							case "/preferences":
							case "/preferences/":
								try {
									uint viewer_id = uint.Parse(apigProxyEvent.QueryStringParameters["viewer_id"]);
									response = new StableAPIResponse() {
										Body = JsonConvert.SerializeObject(ctx.GetPreferences(viewer_id)),
										StatusCode = HttpStatusCode.OK
									};
								} catch(Exception e) {
									response = new StableAPIResponse() {
										Body = JsonConvert.SerializeObject(new Result(e)),
										StatusCode = HttpStatusCode.BadRequest
									};
								}
								break;

							case "/schedule":
							case "/schedule/":
								if(apigProxyEvent.QueryStringParameters != null)
									if(apigProxyEvent.QueryStringParameters.ContainsKey("viewer_id")) {
										try {
											uint viewer_id = uint.Parse(apigProxyEvent.QueryStringParameters["viewer_id"]);

											var regs = ctx.registrations.AsNoTracking().Where(thus => thus.viewer_id == viewer_id).ToList();
											response = new StableAPIResponse() {
												Body = JsonConvert.SerializeObject(regs),
												StatusCode = HttpStatusCode.OK
											};
											break;
										} catch(Exception e) {
											Logger.LogLine(e.ToString());
											response = new StableAPIResponse() {
												Body = JsonConvert.SerializeObject(new Result(e)),
												StatusCode = HttpStatusCode.BadRequest
											};
										}
										break;
									}
								response = new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(ctx.Schedule),
									StatusCode = HttpStatusCode.OK
								};
								break;
							case "/print":
							case "/print/":
								response = HandlePrint(apigProxyEvent, ctx);
								break;
							case "/full":
							case "/full/":
								response = new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(ctx.FullPresentations),
									StatusCode = HttpStatusCode.OK
								};
								break;
							default:
								break;
						}
						#endregion
						break;
					case "POST":
						#region POSTs
						switch(apigProxyEvent.Path.ToLower()) {
							case "/dates":
							case "/dates/":
								response = HandlePOST<Date>(apigProxyEvent, ctx);
								break;
							case "/blocks":
							case "/blocks/":
								response = HandlePOST<Block>(apigProxyEvent, ctx);
								break;
							case "/grades":
							case "/grades/":
								response = HandlePOST<Grade>(apigProxyEvent, ctx);
								break;
							case "/houses":
							case "/houses/":
								response = HandlePOST<House>(apigProxyEvent, ctx);
								break;
							case "/locations":
							case "/locations/":
								response = HandlePOST<Location>(apigProxyEvent, ctx);
								break;
							case "/presentations":
							case "/presentations/":
								response = HandlePOST<Presentation>(apigProxyEvent, ctx);
								break;
							case "/schedule":
							case "/schedule/":
								response = HandlePOST<Schedule>(apigProxyEvent, ctx);
								break;
							case "/viewers":
							case "/viewers/":
								//response = HandlePOST<Viewer>(apigProxyEvent, ctx);
								break;

							case "/signup":
							case "/signup/":
								if(!enabled)
									return noSignups;
								response = startSignup(apigProxyEvent, ctx, conStr);
								break;
							case "/register":
							case "/register/":
								if(!enabled)
									return noSignups;
								if(freeforall)
									response = handleRegister(apigProxyEvent, ctx, conStr);
								else
									response = new StableAPIResponse() {
										Body = "{}",
										StatusCode = HttpStatusCode.Forbidden
									};
								break;

							case "/signup/finish":
							case "/signup/finish/":
								if(!enabled)
									return noSignups;
								if(freeforall)
									response = finishRegister(apigProxyEvent, ctx, context);
								else
									response = finishSignup(apigProxyEvent, ctx, context);
								break;
						}
						#endregion
						break;
					case "PUT":
						#region PUTs
						switch(apigProxyEvent.Path.ToLower()) {
							case "/dates":
							case "/dates/":
								response = HandlePUT<Date>(apigProxyEvent, ctx);
								break;
							case "/blocks":
							case "/blocks/":
								response = HandlePUT<Block>(apigProxyEvent, ctx);
								break;
							case "/grades":
							case "/grades/":
								response = HandlePUT<Grade>(apigProxyEvent, ctx);
								break;
							case "/houses":
							case "/houses/":
								response = HandlePUT<House>(apigProxyEvent, ctx);
								break;
							case "/locations":
							case "/locations/":
								response = HandlePUT<Location>(apigProxyEvent, ctx);
								break;
							case "/presentations":
							case "/presentations/":
								response = HandlePUT<Presentation>(apigProxyEvent, ctx);
								break;
							case "/viewers":
							case "/viewers/":
								response = HandlePUT<Viewer>(apigProxyEvent, ctx);
								break;
						}
						break;
					#endregion
					case "DELETE":
						#region DELETEs
						switch(apigProxyEvent.Path.ToLower()) {
							case "/dates":
							case "/dates/":
								response = HandleDELETE<Date>(apigProxyEvent, ctx);
								break;
							case "/blocks":
							case "/blocks/":
								response = HandleDELETE<Block>(apigProxyEvent, ctx);
								break;
							case "/grades":
							case "/grades/":
								response = HandleDELETE<Grade>(apigProxyEvent, ctx);
								break;
							case "/houses":
							case "/houses/":
								response = HandleDELETE<House>(apigProxyEvent, ctx);
								break;
							case "/locations":
							case "/locations/":
								response = HandleDELETE<Location>(apigProxyEvent, ctx);
								break;
							case "/presentations":
							case "/presentations/":
								response = HandleDELETE<Presentation>(apigProxyEvent, ctx);
								break;
							case "/schedule":
							case "/schedule/":
								response = HandleDELETE<Schedule>(apigProxyEvent, ctx);
								break;
							case "/viewers":
							case "/viewers/":
								response = HandleDELETE<Viewer>(apigProxyEvent, ctx);
								break;
							default:
								break;
						}
						#endregion
						break;
					default:
						break;
				}
			}
			//Logger.LogLine($"RESPONSE CODE: {((HttpStatusCode)response.StatusCode).ToString()}{Environment.NewLine}{response.Body}");

			return response;
		}

		//You gotta love generic typing!! :D

		private StableAPIResponse HandlePOST<E>(APIGatewayProxyRequest request, StableContext ctx) where E : class {
			try {
				string adminCode = GetEnvironmentVariable("admin_code");
				if(adminCode == null || adminCode == "")
					throw new InvalidOperationException("admin_code not set on server");

				if(!request.Headers.ContainsKey("admin_code"))
					throw new ArgumentException("admin_code is missing");

				if(request.Headers["admin_code"] != adminCode)
					throw new UnauthorizedAccessException("Invalid admin_code");

				E obj = JsonConvert.DeserializeObject<E>(request.Body);

				using(var tx = ctx.Database.BeginTransaction()) {
					try {
						ctx.Add(obj);
						int status = ctx.SaveChanges();
						tx.Commit();
						return new StableAPIResponse() {
							Body = JsonConvert.SerializeObject((status == 1)),
							StatusCode = HttpStatusCode.OK
						};
					} catch(Exception e) {
						tx.Rollback();
						Logger.LogLine(e.ToString());
						return new StableAPIResponse() {
							Body = JsonConvert.SerializeObject(new Result(e)),
							StatusCode = HttpStatusCode.InternalServerError
						};
					}
				}

			} catch(Exception e) {
				Logger.LogLine(e.ToString());
				return StableAPIResponse.BadRequest(e);
			}
		}
		private StableAPIResponse HandlePUT<E>(APIGatewayProxyRequest request, StableContext ctx) where E : class {
			try {
				string adminCode = GetEnvironmentVariable("admin_code");
				if(adminCode == null || adminCode == "")
					throw new InvalidOperationException("admin_code not set on server");

				if(!request.Headers.ContainsKey("admin_code"))
					throw new ArgumentException("admin_code is missing");

				if(request.Headers["admin_code"] != adminCode)
					throw new UnauthorizedAccessException("Invalid admin_code");

				E obj = JsonConvert.DeserializeObject<E>(request.Body);

				using(var tx = ctx.Database.BeginTransaction()) {
					try {
//						var existing = ctx.Attach<E>(obj);
						
						ctx.Entry(obj).State = EntityState.Modified;
						int status = ctx.SaveChanges();
						tx.Commit();
						return new StableAPIResponse() {
							Body = JsonConvert.SerializeObject((status == 1)),
							StatusCode = HttpStatusCode.OK
						};
					} catch(Exception e) {
						tx.Rollback();
						Logger.LogLine(e.ToString());
						return new StableAPIResponse() {
							Body = JsonConvert.SerializeObject(new Result(e)),
							StatusCode = HttpStatusCode.InternalServerError
						};
					}
				}

			} catch(Exception e) {
				Logger.LogLine(e.ToString());
				return StableAPIResponse.BadRequest(e);
			}
		}
		private StableAPIResponse HandleDELETE<E>(APIGatewayProxyRequest request, StableContext ctx) where E : class {
			try {
				string adminCode = GetEnvironmentVariable("admin_code");
				if(adminCode == null || adminCode == "")
					throw new InvalidOperationException("admin_code not set on server");

				if(!request.Headers.ContainsKey("admin_code"))
					throw new ArgumentException("admin_code is missing");

				if(request.Headers["admin_code"] != adminCode)
					throw new UnauthorizedAccessException("Invalid admin_code");

				E obj = JsonConvert.DeserializeObject<E>(request.Body);
				/*
				 * Gotta wrap DB ops in a transaction
				 * otherwise, if they die in a try catch
				 * it could leave an uncommitted tx on the db
				 * causing problems with future requests
				using(var tx = ctx.Database.BeginTransaction()) {
					
				}
				*/
				using(var tx = ctx.Database.BeginTransaction()) {
					try {
						//Logger.LogLine(obj.GetType().ToString());
						ctx.Remove(obj);
						//ctx.Attach(obj);
						//ctx.Remove(obj);
						//ctx.dates.Remove(ctx.dates.Single(thus => thus.date == date));
						int status = ctx.SaveChanges();
						tx.Commit();
						return new StableAPIResponse() {
							Body = JsonConvert.SerializeObject((status == 1)),
							StatusCode = HttpStatusCode.OK
						};
					} catch(Exception e) {
						tx.Rollback();
						Logger.LogLine(e.ToString());
						return new StableAPIResponse() {
							Body = JsonConvert.SerializeObject(new Result(e)),
							StatusCode = HttpStatusCode.InternalServerError
						};
					}
				}

			} catch(Exception e) {
				Logger.LogLine(e.ToString());
				return StableAPIResponse.BadRequest(e);
			}
		}
		//preferences array
		private StableAPIResponse startSignup(APIGatewayProxyRequest request, StableContext ctx, string conStr) {
			try {
				SignupRequest sr = JsonConvert.DeserializeObject<SignupRequest>(request.Body);
				try {
					sr.TrimAll();

					if(sr.version != "2.0" || !sr.resume.HasValue) {
						//Send error with HTTP 200 for backwards compatability until next year

						return new StableAPIResponse() {
							Body = JsonConvert.SerializeObject(new Result() {
								status = false,
								details = "Outdated Version! Hard reload or try a different browser!"
							}),

							StatusCode = HttpStatusCode.OK
						};
					}

					if(sr.resume.Value) {
						//fetch user by email and password, send signup response
						try {
							var viewers = ctx.viewers.AsNoTracking().Where(thus => thus.email == sr.email);

							if(viewers.Count() != 1) {
								return new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(new Result() {
										status = false,
										details = "Email not found!"
									}),

									StatusCode = HttpStatusCode.OK
								};
							} else {
								//check password
								Viewer viewer = viewers.First();

								if(!SecurePasswordHasher.Verify(sr.password, viewer.password)) {
									return new StableAPIResponse() {
										Body = JsonConvert.SerializeObject(new Result() {
											status = false,
											details = "Incorrect Password!"
										}),

										StatusCode = HttpStatusCode.OK
									};
								}

								return new StableAPIResponse() {
									Body = JsonConvert.SerializeObject(new SignupResponse(viewer) {
										status = true
									}),
									StatusCode = HttpStatusCode.OK
								};
							}

							
						} catch {
							return new StableAPIResponse() {
								Body = JsonConvert.SerializeObject(new Result() {
									status = false,
									details = "Login Error!"
								}),

								StatusCode = HttpStatusCode.OK
							};
						}
						throw new Exception("LoginError");
					}

					// Create viewer entry first, so if they don't submit properly
					// we'll have their info and can randomly place them.

					if(ctx.viewers.AsNoTracking().Any(thus => thus.email == sr.email)) {
						return new StableAPIResponse() {
							Body = JsonConvert.SerializeObject(new Result() {
								status = false,
								details = "Email already used!"
							}),

							StatusCode = HttpStatusCode.OK
						};
					}

					Viewer v = new Viewer() {
						first_name = sr.first_name.Trim(),
						last_name = sr.last_name.Trim(),
						grade_id = sr.grade,
						house_id = sr.house,
						viewer_key = Guid.NewGuid().ToString().Substring(0, 16),
						reserved = sr.reserved,
						email = sr.email,
						password = SecurePasswordHasher.Hash(sr.password)
					};

					if(!CheckEnabled(v.grade_id))
						return StableAPIResponse.NoSignups;

					if (v.first_name.Length == 0 || v.last_name.Length == 0) {
						return new StableAPIResponse() {
							Body = JsonConvert.SerializeObject(new Result() {
								status = false,
								details = "Name cannot be empty!"
							}),
							
							StatusCode = HttpStatusCode.OK
						};
					}

					Presentation p = null;
					Schedule presentationSenior = null;

					Presentation pre_presentation = null;
					Schedule pre_schedule = null;

					Dictionary<Presentation, Schedule> others = new Dictionary<Presentation, Schedule>();


					if(sr.reserved != -1 && sr.grade == 4) {
						p = ctx.presentations.AsNoTracking().FirstOrDefault(thus => thus.presentation_id == sr.reserved);
						uint block_id = uint.MaxValue;
						presentationSenior = ctx.schedule.AsNoTracking().FirstOrDefault(thus => thus.presentation_id == sr.reserved);

						var main_co = ctx.CoRequisiteMembers.AsNoTracking().FirstOrDefault(thus => thus.p_id == p.presentation_id);
						if(main_co != null) {
							var group_id = main_co.group_id;
							var otherP = ctx.CoRequisiteMembers.FirstOrDefault(thus => thus.group_id == group_id && thus.p_id != p.presentation_id);

							var otherS = ctx.schedule.AsNoTracking().FirstOrDefault(thus => thus.presentation_id == otherP.p_id);

							others.Add(ctx.presentations.AsNoTracking().FirstOrDefault(thus => thus.presentation_id == otherP.p_id), otherS);
						}



						if(presentationSenior.block_id != FirstBlockID && presentationSenior.block_id != FirstBlockAfterLunchID) { // Find previous presentation at the same location
							var found = ctx.schedule.AsNoTracking().Where(
									thus => thus.date == presentationSenior.date
									&& thus.block_id == (presentationSenior.block_id - 1)
									&& thus.location_id == presentationSenior.location_id
								);



							if(found.Count() == 1) {
								pre_schedule = found.First();
								pre_presentation = ctx.presentations.AsNoTracking().FirstOrDefault(thus => thus.presentation_id == pre_schedule.presentation_id);

								
								var main_co_2 = ctx.CoRequisiteMembers.AsNoTracking().FirstOrDefault(thus => thus.p_id == pre_presentation.presentation_id);
								if(main_co_2 != null) {
									var group_id = main_co_2.group_id;
									var otherP = ctx.CoRequisiteMembers.FirstOrDefault(thus => thus.group_id == group_id && thus.p_id != pre_presentation.presentation_id);

									var otherS = ctx.schedule.AsNoTracking().FirstOrDefault(thus => thus.presentation_id == otherP.p_id);

									others.Add(ctx.presentations.AsNoTracking().FirstOrDefault(thus => thus.presentation_id == otherP.p_id), otherS);
								}

							}
						}
					}
					

					
					using(var tx = ctx.Database.BeginTransaction()) {
						try {
							ctx.viewers.Add(v);
							ctx.SaveChanges();
							tx.Commit();
							if(p != null) {
								if(presentationSenior == null)
									throw new Exception($"Schedule not found for presentation! [{p.presentation_id}]");
								try {
									using(var dbCon = new MySqlConnection(conStr)) {
										dbCon.Open();
										Register(dbCon, v, new RegistrationRequest() {
											date = presentationSenior.date,
											block_id = presentationSenior.block_id,
											presentation_id = p.presentation_id,
											viewer_id = v.viewer_id,
											viewer_key = v.viewer_key,
											location_id = presentationSenior.location_id
										}, true);

										if(pre_presentation != null && pre_schedule != null) {
											Register(dbCon, v, new RegistrationRequest() {
												date = pre_schedule.date,
												block_id = pre_schedule.block_id,
												presentation_id = pre_presentation.presentation_id,
												viewer_id = v.viewer_id,
												viewer_key = v.viewer_key,
												location_id = pre_schedule.location_id
											}, true);

										}

										foreach(var o in others) {
											if(o.Key.presentation_id == p.presentation_id)
												continue;

											if(pre_presentation != null)
												if(pre_presentation.presentation_id == o.Key.presentation_id)
													continue;

											Register(dbCon, v, new RegistrationRequest() {
												date = o.Value.date,
												block_id = o.Value.block_id,
												presentation_id = o.Key.presentation_id,
												viewer_id = v.viewer_id,
												viewer_key = v.viewer_key,
												location_id = o.Value.location_id
											}, true);
										}
									}
								} catch(Exception e) {
									Logger.LogLine(str(StableAPIResponse.InternalServerError(e)));
									return new StableAPIResponse() {
										Body = JsonConvert.SerializeObject(new Result(e)),
										StatusCode = HttpStatusCode.InternalServerError
									};
								}
							}

							

							return new StableAPIResponse() {
								Body = JsonConvert.SerializeObject(new SignupResponse(v) {
									status = true
								}),
								StatusCode = HttpStatusCode.OK
							};

						} catch(Exception e) {
							tx.Rollback();
							return new StableAPIResponse() {
								Body = JsonConvert.SerializeObject(new Result(e)),
								StatusCode = HttpStatusCode.InternalServerError
							};
						}
					}
				} catch(Exception e) {
					return new StableAPIResponse() {
						Body = JsonConvert.SerializeObject(new Result(e)),
						StatusCode = HttpStatusCode.InternalServerError
					};
				}
			} catch(Exception e) {
				return StableAPIResponse.BadRequest(e);
			}
		}
		//preferences array
		private StableAPIResponse finishSignup(APIGatewayProxyRequest apigProxyEvent, StableContext ctx, ILambdaContext context) {
			try {
				var req = JsonConvert.DeserializeObject<FinishSignupRequest>(apigProxyEvent.Body);
				req.status = true;

				try {
					//&& !thus.Saved()
					if(ctx.viewers.AsNoTracking().Count(thus => thus.viewer_id == req.viewer_id && thus.viewer_key == req.viewer_key) != 1)
						return new StableAPIResponse() {
							Body = "{}",
							StatusCode = HttpStatusCode.Unauthorized
						};
					List<Preference> toAdd = new List<Preference>();
					for(int x = 0; x < req.data.Count; x++) {
						toAdd.Add(new Preference() {
							viewer_id = req.viewer_id,
							order = (uint)(x + 1),
							presentation_id = req.data[x]
						});
					}
					using(var tx = ctx.Database.BeginTransaction()) {
						try {
							if(ctx.preferences.Any(thus =>  thus.viewer_id == req.viewer_id)) {
								List<Preference> toRemove = new List<Preference>();
								toRemove.AddRange(ctx.preferences.Where(thus => thus.viewer_id == req.viewer_id));
								foreach(var p in toRemove) {
									ctx.preferences.Remove(p);
								}
								ctx.SaveChanges();
							}
							foreach(Preference p in toAdd) {
								ctx.preferences.Add(p);
							}
							Viewer v = ctx.viewers.FirstOrDefault(thus => thus.viewer_id == req.viewer_id && thus.viewer_key == req.viewer_key);
							v.saved = 1;
							ctx.SaveChanges();
							tx.Commit();
						} catch(DbUpdateException e) {
							tx.Rollback();
							if(e.InnerException != null) {
								if(e.InnerException.GetType() == typeof(MySqlException)) {
									var me = e.InnerException as MySqlException;
									return new StableAPIResponse() {
										Body = JsonConvert.SerializeObject(new SignupErrorResponse(me.Number)),
										StatusCode = HttpStatusCode.OK
									};
								}
							}
							return new StableAPIResponse() {
								Body = JsonConvert.SerializeObject(e),
								StatusCode = HttpStatusCode.InternalServerError
							};
						} catch(Exception e) {
							tx.Rollback();
							var expt = e;
							while(expt != null) {
								context.Logger.LogLine(expt.Message);
								expt = expt.InnerException;
							}
							return new StableAPIResponse() {
								Body = JsonConvert.SerializeObject(new Result(e)),
								StatusCode = HttpStatusCode.InternalServerError
							};

						}
					}
				} catch(Exception e) {
					return new StableAPIResponse() {
						Body = JsonConvert.SerializeObject(new Result(e)),
						StatusCode = HttpStatusCode.InternalServerError
					};

				}
				return new StableAPIResponse() {
					StatusCode = HttpStatusCode.OK,
					Body = JsonConvert.SerializeObject(req)
				};
			} catch(Exception e) {
				return StableAPIResponse.BadRequest(e);
			}
		}
		private StableAPIResponse HandlePrint(APIGatewayProxyRequest request, StableContext ctx) {
			uint pres_id;

			try {
				pres_id = uint.Parse(request.QueryStringParameters["presentation_id"]);
				var res = new StableAPIResponse() {
					Body = "",
					StatusCode = HttpStatusCode.OK,

				};
				if(pres_id == PrepPresentationId) {
					return new StableAPIResponse() {
						Body = "Cannot fetch Prep presentation schedule",
						StatusCode = HttpStatusCode.BadRequest
					};
				}
				if(pres_id == 0) {
					var schedule = ctx.Schedule;

					foreach(var k in schedule.OrderBy(thus => thus.location_id).ThenBy(thus => thus.date).ThenBy(thus => thus.block_id)) {
						if(k.presentation_id == PrepPresentationId)
							continue;

						res.Body += PrintPres(k.presentation_id, ctx);
					}
				} else {
					res.Body = PrintPres(pres_id, ctx);
				}

				res.Headers.Add("Content-Type", "text/html; charset=utf-8");
				return res;
			} catch(Exception e) {
				return new StableAPIResponse() {
					Body = JsonConvert.SerializeObject(new Result(e)),
					StatusCode = HttpStatusCode.BadRequest
				};
			}
		}
		private string PrintPres(uint pres_id, StableContext ctx) {
			try {
				var presentation = ctx.presentations.First(thus => thus.presentation_id == pres_id);
				var schedule = ctx.schedule.First(thus => thus.presentation_id == pres_id);
				var location = ctx.locations.First(thus => thus.location_id == schedule.location_id);
				var blocks = ctx.Blocks;
				var grades = ctx.Grades;
				var houses = ctx.Houses;

				var schedules = ctx.schedule.Where(thus => thus.presentation_id == pres_id).ToList();
				var viewers = new Dictionary<Schedule, List<Viewer>>();

				foreach(Schedule s in schedules) {
					viewers.Add(s, new List<Viewer>());
				}

				var temp = ctx.registrations.Where(thus => thus.presentation_id == pres_id).ToList();
				if(temp.Count > 0) {
					var viewers_in_pres = from thus in temp select thus.viewer_id;

					var viewers_with_data = ctx.viewers.Where(thus => viewers_in_pres.Contains(thus.viewer_id)).ToList();
					foreach(Registration r in temp) {
						var temp_schedule = r.Schedule();
						temp_schedule.location_id = location.location_id;

						viewers[temp_schedule].Add(viewers_with_data.First(thus => thus.viewer_id == r.viewer_id));
					}
				}

				PrintOutput print = new PrintOutput() {
					presentationData = presentation,
					locationData = location,
					blocks = blocks,
					schedule = schedules,
					grades = grades,
					houses = houses,
					viewers = viewers

				};
				return print.ToString();
			} catch(Exception e) {
				throw;
			}
		}
		//Free for all
		private StableAPIResponse handleRegister(APIGatewayProxyRequest request, StableContext ctx, string conStr) {
			try {
				var req = JsonConvert.DeserializeObject<RegistrationRequest>(request.Body);
				try {
					if(ctx.viewers.AsNoTracking().Count(thus => thus.viewer_id == req.viewer_id && thus.viewer_key == req.viewer_key) != 1) {
						return StableAPIResponse.Unauthorized;
					}

					//disable for login
					if(false && ctx.viewers.AsNoTracking().Count(thus => thus.viewer_id == req.viewer_id && thus.Saved()) == 1) {
						return new StableAPIResponse() {
							StatusCode = HttpStatusCode.OK,
							Body = JsonConvert.SerializeObject(new RegistrationResponse() {
								status = false,
								error = new ViewerSavedError() {
									code = 103,
									message = "Viewer already saved, no further changes are allowed."
								}
							})
						};
					}


					if (ctx.schedule.AsNoTracking().Count(thus => thus.date == req.date && thus.block_id == req.block_id && thus.presentation_id == req.presentation_id) != 1)
						return StableAPIResponse.BadRequest(new Exception("Presentation instance not found!"));

					//var presentation = ctx.presentations.AsNoTracking().First(thus => thus.presentation_id == req.presentation_id);
					//req.location_id = presentation.location_id;

					Viewer v = ctx.viewers.AsNoTracking().FirstOrDefault(thus => thus.viewer_id == req.viewer_id);

					if(!CheckEnabled(v.grade_id))
						return StableAPIResponse.NoSignups;


					//attempt to update presentations
					try {
						using(MySqlConnection dbCon = new MySqlConnection(conStr)) {
							dbCon.Open();

							if(req.presentation_id == PrepPresentationId)
								throw new Exception("Locked Presentation!");
							Register(dbCon, v, req);
						}
						return new StableAPIResponse() {
							StatusCode = HttpStatusCode.OK,
							Body = JsonConvert.SerializeObject(new RegistrationResponse() {
								status = true,
								data = ctx.registrations.AsNoTracking().Where(thus => thus.viewer_id == req.viewer_id).ToList(),
								full = ctx.FullPresentations
							})
						};
					} catch(Exception e) {
						return new StableAPIResponse() {
							Body = JsonConvert.SerializeObject(new SignupErrorResponse() {
								Message = e.Message,
								trace = e.StackTrace,
								data = ctx.registrations.AsNoTracking().Where(thus => thus.viewer_id == req.viewer_id).ToList(),
								full = ctx.FullPresentations
							}),
							StatusCode = HttpStatusCode.InternalServerError
						};
					}
				} catch(Exception e) {
					return StableAPIResponse.InternalServerError(e);
				}
			} catch(Exception e) {
				Logger.LogLine(e.Message.ToString());
				return StableAPIResponse.BadRequest(e);
			}
		}

		//free for all
		private void Register(MySqlConnection dbCon, Viewer v, RegistrationRequest req, bool ignoreFull = false) {
			List<RegistrationRequest> reqs = new List<RegistrationRequest>();
			reqs.Add(req);

			string q;

			//Find a coreq group
			uint? g_id = null;

			q = "SELECT `group_id` FROM `corequisite_members` WHERE `p_id` = @p_id LIMIT 1;";

			using(var cmd = new MySqlCommand(q, dbCon)) {
				cmd.Prepare();
				cmd.Parameters.AddWithValue("@p_id", req.presentation_id);
				g_id = (uint?)cmd.ExecuteScalar();
			}


			if(g_id.HasValue) {
				//get list of coreqs to add
				q = "SELECT `p_id` FROM `corequisite_members` WHERE `group_id` = @g_id AND `p_id` != @p_id;";
				List<uint> ids = new List<uint>();
				using(var cmd = new MySqlCommand(q, dbCon)) {
					cmd.Prepare();
					cmd.Parameters.AddWithValue("@g_id", g_id.Value);
					cmd.Parameters.AddWithValue("@p_id", req.presentation_id);
					using(var r = cmd.ExecuteReader()) {
						while(r.Read()) {
							ids.Add(r.GetUInt32("p_id"));
						}
						r.Close();
					}
				}

				Logger.LogLine($"PSSOSOS: {string.Join(", ", ids)}");

				q = "SELECT `date`, `block_id`, `presentation_id`, `location_id` FROM `schedule` WHERE `presentation_id` IN (" + string.Join(",", ids) + ");";

				List<Schedule> schedules = new List<Schedule>();

				using(var cmd = new MySqlCommand(q, dbCon)) {
					using(var r = cmd.ExecuteReader()) {
						while(r.Read()) {
							schedules.Add(new Schedule() {
								date = r.GetUInt32("date"),
								block_id = r.GetUInt32("block_id"),
								presentation_id = r.GetUInt32("presentation_id"),
								location_id = r.GetUInt32("location_id")
							});
						}
					}
				}

				//get presentation info for each id to add, including coreqs
				q = "SELECT `presentation_id` FROM `presentations` WHERE `presentation_id` IN (" + string.Join(",", ids) + ");";

				

				using(var cmd = new MySqlCommand(q, dbCon)) {

					using(var r = cmd.ExecuteReader()) {
						while(r.Read()) {
							var p_id = r.GetUInt32("presentation_id");

							var sch = schedules.First(thus => thus.presentation_id == p_id);
							if(sch == null)
								throw new Exception();

							reqs.Add(new RegistrationRequest() {
								date = sch.date,
								block_id = sch.block_id,
								presentation_id = p_id,
								viewer_id = req.viewer_id,
								viewer_key = req.viewer_key,
								location_id = sch.location_id
							});
						}
					}
				}
			}

			Logger.LogLine($"PSSOSOS: {string.Join(", ", reqs)}");
			//register all ids
			using(var tx = dbCon.BeginTransaction()) {
				try {
					foreach(var r in reqs) {
						RegisterInternal(dbCon, tx, v, r, ignoreFull);
					}
					tx.Commit();
				} catch {
					tx.Rollback();
					throw;
				}

			}

		}
		const uint LocationId_MPR = 20;
		private void RegisterInternal(MySqlConnection dbCon, MySqlTransaction tx, Viewer v, RegistrationRequest req, bool ignoreFull) {
			string q;
			//check the count if not ignoring
			long count = 0;
			long totalCount = 0;
			if(!ignoreFull) {
				q = "SELECT COUNT(`viewers`.`grade_id`) FROM `registrations` JOIN `viewers` ON `viewers`.`viewer_id` = `registrations`.`viewer_id` WHERE "
					+ " `registrations`.`date`=@date AND `registrations`.`block_id`=@block_id AND `registrations`.`presentation_id`=@presentation_id AND `viewers`.`grade_id`=@g_id;";
				using(var cmd = new MySqlCommand(q, dbCon, tx)) {
					cmd.Prepare();
					cmd.Parameters.AddWithValue("@date", req.date);
					cmd.Parameters.AddWithValue("@block_id", req.block_id);
					cmd.Parameters.AddWithValue("@presentation_id", req.presentation_id);
					cmd.Parameters.AddWithValue("@g_id", v.grade_id);
					count = (long)cmd.ExecuteScalar();
				}

				q = "SELECT COUNT(*) FROM `registrations` WHERE `registrations`.`date`=@date AND `registrations`.`block_id`=@block_id AND `registrations`.`presentation_id`=@presentation_id;";
				using(var cmd = new MySqlCommand(q, dbCon, tx)) {
					cmd.Prepare();
					cmd.Parameters.AddWithValue("@date", req.date);
					cmd.Parameters.AddWithValue("@block_id", req.block_id);
					cmd.Parameters.AddWithValue("@presentation_id", req.presentation_id);
					totalCount = (long)cmd.ExecuteScalar();
				}

				Logger.LogLine("Count by g: " + count);
				Logger.LogLine("Count total: " + totalCount);

				int maxViewersPerGrade = v.grade_id == 4 ? 17 : 12;
				int maxTotal = req.location_id == LocationId_MPR ? 38 : 32;

				



				if(count > maxViewersPerGrade || totalCount > maxTotal)
					throw new InvalidOperationException("Presentation is full!");
				
			}

			Logger.LogLine($"Req: {req.ToString()}");

			//Remove any existing regs in that slot, and if it has coreqs, them too
			RemoveExisting(dbCon, tx, req);

			//Add new reg
			q = "INSERT INTO `registrations` (`date`, `block_id`, `viewer_id`, `presentation_id`) VALUES (@date, @block_id, @viewer_id, @p_id); ";
			using(var cmd = new MySqlCommand(q, dbCon, tx)) {
				cmd.Prepare();
				cmd.Parameters.AddWithValue("@date", req.date);
				cmd.Parameters.AddWithValue("@block_id", req.block_id);
				cmd.Parameters.AddWithValue("@viewer_id", req.viewer_id);
				cmd.Parameters.AddWithValue("@p_id", req.presentation_id);
				if(cmd.ExecuteNonQuery() != 1)
					throw new Exception();
			}
		}
		private void RemoveExisting(MySqlConnection dbCon, MySqlTransaction tx, RegistrationRequest req) {
			List<uint> toRemove = new List<uint>();
			string q;

			uint? existingPresentation = null;

			//get existing p_id in the registration slot
			q = "SELECT `presentation_id` FROM `registrations` WHERE `date`=@date AND `block_id`=@block_id AND `viewer_id`=@viewer_id;";
			using(var cmd = new MySqlCommand(q, dbCon, tx)) {
				cmd.Prepare();
				cmd.Parameters.AddWithValue("@date", req.date);
				cmd.Parameters.AddWithValue("@block_id", req.block_id);
				cmd.Parameters.AddWithValue("@viewer_id", req.viewer_id);
				existingPresentation = (uint?)cmd.ExecuteScalar();
			}

			if(!existingPresentation.HasValue) {
				Logger.LogLine("Exists: false");
				return;
			}
			Logger.LogLine("Exists: true");

			toRemove.Add(existingPresentation.Value);

			if(existingPresentation.Value == PrepPresentationId)
				throw new Exception("PREP Required!");

			//Check if it has coreqs
			uint? g_id = null;

			q = "SELECT `group_id` FROM `corequisite_members` WHERE `p_id`=@p_id LIMIT 1;";
			using(var cmd = new MySqlCommand(q, dbCon, tx)) {
				cmd.Prepare();
				cmd.Parameters.AddWithValue("@p_id", existingPresentation.Value);
				g_id = (uint?)cmd.ExecuteScalar();
			}

			if(g_id.HasValue) {
				//it does, so add them to the remove list
				q = "SELECT `p_id` FROM `corequisite_members` WHERE `group_id`=@g_id;";
				using(var cmd = new MySqlCommand(q, dbCon, tx)) {
					cmd.Prepare();
					cmd.Parameters.AddWithValue("@g_id", g_id.Value);
					using(var r = cmd.ExecuteReader()) {
						while(r.Read()) {
							toRemove.Add(r.GetUInt32("p_id"));
						}
					}
				}
			}

			//remove all
			toRemove = toRemove.Distinct().ToList();

			q = "DELETE FROM `registrations` WHERE `viewer_id`=@v_id AND `presentation_id` IN (" + string.Join(",", toRemove) + ");";
			using(var cmd = new MySqlCommand(q, dbCon, tx)) {
				cmd.Prepare();
				cmd.Parameters.AddWithValue("@v_id", req.viewer_id);
				cmd.ExecuteNonQuery();
			}
		}
		private StableAPIResponse finishRegister(APIGatewayProxyRequest request, StableContext ctx, ILambdaContext context) {
			try {
				var req = JsonConvert.DeserializeObject<FinishSignupRequest>(request.Body);
				req.status = true;

				try {
					if(ctx.viewers.AsNoTracking().Count(thus => thus.viewer_id == req.viewer_id && thus.viewer_key == req.viewer_key) != 1)
						return StableAPIResponse.Unauthorized;

					using(var tx = ctx.Database.BeginTransaction()) {
						try {
							Viewer v = ctx.viewers.First(thus => thus.viewer_id == req.viewer_id && !thus.Saved());
							v.saved = 1;
							tx.Commit();
							ctx.SaveChanges();
						} catch(Exception e) {
							tx.Rollback();
							return new StableAPIResponse() {
								StatusCode = HttpStatusCode.OK,
								Body = JsonConvert.SerializeObject(new RegistrationResponse() {
									status = false,
									error = new ViewerSavedError() {
										code = 103,
										message = "Viewer already saved, no further changes are allowed."
									}
								})
							};
						}
					}


				} catch(Exception e) {
					return new StableAPIResponse() {
						Body = JsonConvert.SerializeObject(new Result(e)),
						StatusCode = HttpStatusCode.InternalServerError
					};

				}
				return new StableAPIResponse() {
					StatusCode = HttpStatusCode.OK,
					Body = JsonConvert.SerializeObject(req)
				};
			} catch(Exception e) {
				return StableAPIResponse.BadRequest(e);
			}
		}
	}
	public class StableAPIResponse : APIGatewayProxyResponse {
		public StableAPIResponse() {
			Headers = new Dictionary<string, string>() {
				{ "access-control-allow-origin", Function.GetEnvironmentVariable("SITE_DOMAIN") }
			};
		}
		new public HttpStatusCode StatusCode {
			get {
				return (HttpStatusCode)base.StatusCode;
			}
			set {
				base.StatusCode = (int)value;
			}
		}
		public static StableAPIResponse OK {
			get {
				return new StableAPIResponse() {
					Body = "{}",
					StatusCode = HttpStatusCode.OK
				};
			}
		}
		public static StableAPIResponse NotImplemented {
			get {
				return new StableAPIResponse() {
					Body = "{}",
					StatusCode = HttpStatusCode.NotImplemented
				};
			}
		}
		public static StableAPIResponse Unauthorized {
			get {
				return new StableAPIResponse() {
					Body = "{}",
					StatusCode = HttpStatusCode.Unauthorized
				};
			}
		}
		public static StableAPIResponse NoSignups {
			get {
				return new StableAPIResponse() {
					Body = "{}",
					StatusCode = (HttpStatusCode)418
				};
			}
		}
		public static StableAPIResponse BadRequest(Exception e) {
			return new StableAPIResponse() {
				StatusCode = HttpStatusCode.BadRequest,
				Body = JsonConvert.SerializeObject(new Result(e))
			};
		}
		public static StableAPIResponse InternalServerError(Exception e) {
			return new StableAPIResponse() {
				StatusCode = HttpStatusCode.InternalServerError,
				Body = JsonConvert.SerializeObject(new Result(e))
			};
		}
	}
}
