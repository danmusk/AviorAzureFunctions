using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AviorAzureFunctions
{
    public static class AddUser
    {
	    private static readonly HttpClient AviorClient = new HttpClient()
	    {
            BaseAddress = new Uri("https://www.mobikey.eu")
        };

        [FunctionName("AddUser")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<MakePlansBookingConfirmed>(requestBody);
            
            // Prepare Payload
            var phone = data.booking.person.phonenumber;
            var name = $"B{data.booking.booked_from:MM}.{data.booking.person.name.Replace(" ", ".")}".Truncate(20);
            var from = data.booking.booked_from.AddMinutes(-15).ToString("yyMMddHHmm");
            var to = data.booking.booked_to.AddMinutes(15).ToString("yyMMddHHmm");
            var groupName = "Bursdag";
            var payload = $"AT#EU=T,{phone},{name},{groupName},{from},{to},YYYYYYY,";
            log.LogInformation($"Payload: {payload}");

            try
            {
	            // Get from appsettings
	            var userId = Environment.GetEnvironmentVariable("AviorUsername");
	            var password = Environment.GetEnvironmentVariable("AviorPassword");

	            log.LogInformation($"UserId: {userId}");
	            log.LogInformation($"Password: {password}");
                
                var stringContent = new StringContent(payload);
	            
                log.LogInformation("Adding User to Avior");
	            var result = await AviorClient.PostAsync($"cmd/T/{userId}/pwd/{password}", stringContent);
	            if (result.IsSuccessStatusCode)
	            {
		            log.LogInformation("User was added to Avior");
	            }
	            else
	            {
		            log.LogError("Something went wrong, user was not added to Avior");
		            log.LogError($"Result: {result.ReasonPhrase}");
	            }
            }
            catch (Exception ex)
            {
	            log.LogError($"Exception: {ex.Message}");
	            throw;
            }

            return new OkObjectResult("OK");
        }
    }
    public static class StringExt
    {
	    public static string Truncate(this string value, int maxLength)
	    {
		    if (string.IsNullOrEmpty(value)) return value;
		    return value.Length <= maxLength ? value : value.Substring(0, maxLength);
	    }
    }

    public class MakePlansBookingConfirmed
    {
	    public Booking booking { get; set; }
    }
    public class Booking
    {
	    public DateTimeOffset booked_from { get; set; }
	    public DateTimeOffset booked_to { get; set; }
	    public Person person { get; set; }
	    public Metadata resource { get; set; }
	    public Metadata service { get; set; }
    }

    public class Person
    {
	    public string name { get; set; }
	    public string email { get; set; }
	    public string phonenumber { get; set; }
    }

    public class Metadata
    {
	    public int id { get; set; }
	    public string title { get; set; }
    }
}
