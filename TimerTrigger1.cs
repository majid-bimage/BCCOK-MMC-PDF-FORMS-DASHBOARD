using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using iTextSharp.text.pdf;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
namespace Bimage.Function

{
    public class TimerTrigger1
    {
                private static readonly HttpClient client = new HttpClient();
                public static readonly string ForgeClientID = "Ak5xhjoOVN80nIGnGXBgWtWf1LS6GbWA";
                public static readonly string ForgeClientSecret = "ForgeClientSecret";

                // public static readonly string CallBackURL2 = "CallBackURL2";

                // public static readonly string CallBackURL = "CallBackURL";

                public static readonly string CallBackURL = "http://localhost:7071/api/HttpTrigger1";
                public static readonly string CallBackURL2 = "http%3A%2F%2Flocalhost%3A7071%2Fapi%2FHttpTrigger1";



        [FunctionName("TimerTrigger1")]
        public async Task RunAsync([TimerTrigger("0 */20 * * * *")]TimerInfo myTimer, ILogger log)
        {

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            // client.GetAsync("https://expcrfia.azurewebsites.net/api/RFIA");
            await client.GetAsync("http://localhost:7071/api/HttpTrigger1");



        }




 
    }

    
}
