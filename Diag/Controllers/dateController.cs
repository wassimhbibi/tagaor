using Diag.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace Diag.Controllers
{
    public class dateController : ApiController
    {
        [HttpGet]
        [Route("api/stream-date")]
        public async Task<HttpResponseMessage> Get()
        {
            var response = new HttpResponseMessage
            {
                Content = new PushStreamContent(async (stream, content, context) =>
                {
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        try
                        {
                            while (true)
                            {
                                if (!stream.CanWrite) break;

                                // Fetch all reserved dates
                                string query = @"SELECT datee FROM dbo.datee";
                                DataTable table = new DataTable();

                                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
                                using (var cmd = new SqlCommand(query, con))
                                {
                                    cmd.CommandType = CommandType.Text;
                                    using (var da = new SqlDataAdapter(cmd))
                                    {
                                        await Task.Run(() => da.Fill(table));
                                    }
                                }

                                if (table.Rows.Count > 0)
                                {
                                    // Convert all reserved dates to a JSON array
                                    var reservedDates = table.AsEnumerable()
                                        .Select(row => row["datee"].ToString())
                                        .ToList();
                                    string jsonData = JsonConvert.SerializeObject(reservedDates);

                                    // Send the updated data as an SSE event
                                    writer.WriteLine($"data: {jsonData}\n");
                                    writer.Flush();
                                }

                                // Delay before next check
                                await Task.Delay(1000);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                        finally
                        {
                            writer.Close();
                            stream.Close();
                        }
                    }
                }, "text/event-stream")
            };

            return response;
        }






        [HttpPost]
        public String post(Date datee)
        {
            try
            {
                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
                {
                    con.Open();

                    // Insert the reservation
                    string query = @"INSERT INTO dbo.datee (datee, dateDotNetFormat) VALUES (@datee, @dateDotNetFormat)";
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@datee", datee.datee);
                        cmd.Parameters.AddWithValue("@dateDotNetFormat", datee.dateDotNetFormat);
                        cmd.ExecuteNonQuery();
                    }

                    // Update ChangeLog so SSE can detect the change
                    string logQuery = @"INSERT INTO dbo.ChangeLog (ChangedData, ChangeTime) VALUES (@data, GETDATE())";
                    using (var logCmd = new SqlCommand(logQuery, con))
                    {
                        logCmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(datee)); // Convert object to JSON
                        logCmd.ExecuteNonQuery();
                    }
                }

                return "Added successfully!";
            }
            catch (Exception ex)
            {
                return $"Failed to add! Error: {ex.Message}";
            }
        }
    }
    }