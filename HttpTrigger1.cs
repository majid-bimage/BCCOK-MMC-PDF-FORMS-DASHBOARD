using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Microsoft.Data.SqlClient;
using iTextSharp.text.pdf;
using Org.BouncyCastle.Bcpg.Sig;
using System.Linq;
using System.ComponentModel;

namespace Company.Function
{
    public static class HttpTrigger1
    {
                private static readonly HttpClient client = new HttpClient();

                public static readonly string ForgeClientID = "H9yDiVt6wHAJIZArAp9YvsptbMds0rOI7eqQUFqAY66H9jlB";
                public static readonly string ForgeClientSecret = "Svj4LNTKYEzMuURfjuKq4vcDcvTD5nn8ushq5ZKs6cb0iSTImo2Y6mEIohUljxWk";
               
                public static string RefreshToken3Legged = "";
                public static string AccessToken3Legged = "";
                public static DateTime AccessToken3LeggedExpiredDateTime;

                // public static readonly string CallBackURL2 = "CallBackURL2";

                // public static readonly string CallBackURL = "CallBackURL";

                public static readonly string CallBackURL = "http://localhost:7071/api/HttpTrigger1/";
                public static readonly string CallBackURL2 = "http%3A%2F%2Flocalhost%3A7071%2Fapi%2FHttpTrigger1%2F";
                public static string scope = "data:read";


        [FunctionName("HttpTrigger1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                string name = req.Query["name"];
                // public static async Task<RedirectResult> GetAuthCode(){
    //         var url = "https://developer.api.autodesk.com/authentication/v1/authorize?response_type=code&client_id=" + ForgeClientID + "&redirect_uri=" + CallBackURL2 + "&scope=" + scope;
    //         Console.WriteLine(url);
    //         RedirectResult redirectResult = new RedirectResult(url);

    //         //connection.Close();
    //         return redirectResult;
    // // }

                        Token3Legged tk3 = GetSecretFromTable();
                        Console.WriteLine(tk3);
                        if(tk3 == null){
                            var AuthCode = req.Query["code"];
                            if (AuthCode.ToString() == string.Empty)
                                {
                                    var url = "https://developer.api.autodesk.com/authentication/v1/authorize?response_type=code&client_id=" + ForgeClientID + "&redirect_uri=" + CallBackURL2 + "&scope=" + scope;
                                    RedirectResult redirectResult = new RedirectResult(url);

                                    //connection.Close();
                                    return redirectResult;
                                }

                                //2. if we have the code, call forge api to obtain new access & refresh token
                                else
                                {
        
                                    log.LogInformation(AuthCode);
                                    Token3Legged tk3a = await Update3LeggedTokenUsingAuthCode(AuthCode,log);
                                    Console.WriteLine(tk3a.AccessToken3Legged);
                                    InsertTokens(tk3a);
                                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                                    dynamic data = JsonConvert.DeserializeObject(requestBody);
                                    name = name ?? data?.name;

                                    string responseMessage = string.IsNullOrEmpty(name)
                                        ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                                        : $"Hello, {name}. This HTTP triggered function executed successfully.";

                                    return new OkObjectResult(responseMessage);
                                }
                            }else{
                                AccessToken3Legged = tk3.AccessToken3Legged;
                                AccessToken3LeggedExpiredDateTime = tk3.AccessToken3LeggedExpiredDateTime;
                                Console.WriteLine(AccessToken3LeggedExpiredDateTime);
                                Console.WriteLine(AccessToken3Legged);
                                DateTime currentTime = DateTime.UtcNow.AddMinutes(15);
                                Console.WriteLine(currentTime);
                                if (currentTime > AccessToken3LeggedExpiredDateTime)
                                {
                                    Console.WriteLine("Current time is greater than the specified time.");
                                    string refresh_token = tk3.RefreshToken3Legged;
                                    Token3Legged tk3aa = await RefreshTokens(refresh_token);
                                    AccessToken3Legged = tk3aa.AccessToken3Legged;
                                    Console.WriteLine(AccessToken3Legged);
                                    Console.WriteLine(tk3aa.AccessToken3LeggedExpiredDateTime);

                                }
                                else if (currentTime == AccessToken3LeggedExpiredDateTime)
                                {
                                    Console.WriteLine("Current time is equal to the specified time.");
                                }
                                else
                                {
                                    Console.WriteLine("Current time is less than the specified time.");
                                }
                                    string token = AccessToken3Legged;
                                if(AccessToken3Legged!= ""){
                                    List<FormField> pdfFields = new List<FormField>();
                                        string hubid = "b.d99fcbc9-1a61-4913-adcd-67f3afb1eda1";


                                        // RedirectResult a= await GetAuthCode();

                                    // Console.WriteLine(a);


                                        // string token = await GetAccessTokenAsync();
                                        // Console.WriteLine(token);

                                        

                                        Project[] projects =  await GetProjects(token, hubid);

                                        foreach (Project project in projects)
                                        {
                                                Console.WriteLine(project.Id.ToString());

                                            if(project.Id == "b.8eddc901-636e-46c8-b2f1-926b903f2dd3")
                                            {
                                                
                                                string GetValueForName(FormData form, string name) {
                                                        var value = form.pdfValues.FirstOrDefault(val => val.name.Contains(name));
                                                        return value?.value ?? "";
                                                    }
                                                Console.WriteLine(project.Id.ToString());
                                                List<FormData> forms = await GetForms(token, "8eddc901-636e-46c8-b2f1-926b903f2dd3");
                                                Console.WriteLine(forms.Count);
                                              /*  foreach(FormData form in forms){
                                                    Console.WriteLine(form.description);
                                                    Console.WriteLine(form.formTemplate.name);
                                                    Console.WriteLine(" --------------------------------------------------------------- ");


                                                }*/
                                                


                                               Project[] TopFolders = await GetTopFolders(token, project.relationships.topFolders.links.related.href);
                                                foreach (Project topFolder in TopFolders)
                                                {
                                                    Content[] contents = await GetContents(token, topFolder.relationships.contents.links.related.href);  
                                                                int ctr =0 ;
                                                    
                                                    foreach(Content content in contents)
                                                    {
                                                        if (content.type == "folders" /*  && content.attributes.displayName.Contains("M&M Metal Sdn Bhd") */  ){
                                                            try{
                                                                var dataToInsert = new Dictionary<string, string>();
                                                                dataToInsert["projectId"] = project.Id;
                                                                Content[] contents_1 = await GetContents(token, content.relationships.contents.links.related.href);
                                                                foreach (Content content1 in contents_1)
                                                                {
                                                                    if (content1.type == "folders" /* && content1.attributes.displayName == "Submitted Forms (MMCE Use Only)" */ ){
                                                                
                                                                    Project[] foldercontents  = await GetTopFolders(token, content1.relationships.contents.links.related.href);

                                                                            string parentName = content.attributes.displayName;
                                                                            // Find the index of the first space character
                                                                            int firstSpaceIndex = parentName.IndexOf(' ');

                                                                            // Extract the substring after the first space
                                                                            string partAfterFirstSpace = (firstSpaceIndex != -1) ? parentName.Substring(firstSpaceIndex + 1) : parentName;

                                                                            Console.WriteLine(partAfterFirstSpace); // Output: M&M Metal Sdn Bhd


                                                                            // Example criteria
                                                                            string targetDescription = partAfterFirstSpace; // "M&M Metal Sdn Bhd";
                                                                            string targetTemplateName = "1. General Info";

                                                                            // Find the form with the specific description and template name
                                                                            FormData targetForm = forms.FirstOrDefault(form =>
                                                                                form.description == targetDescription &&
                                                                                form.formTemplate.name == targetTemplateName);

                                                                            if (targetForm != null)
                                                                            {
                                                                                // Found the form
                                                                                Console.WriteLine("Form found:");
                                                                                Console.WriteLine("Description: " + targetForm.description);
                                                                                Console.WriteLine("Template Name: " + targetForm.formTemplate.name);
                                                                                    dataToInsert["form_display_id"]  = targetForm.formNum.ToString();
                                                                                dataToInsert["form_id"]  = targetForm.id;
                                                                                
                                                                                dataToInsert["trim_parent_name"] = partAfterFirstSpace;
                                                                                dataToInsert["companyNameofSubContractor"] = targetForm.pdfValues.FirstOrDefault(val =>  val.name == "COMPANY NAME OF SUBCONTRACTOR / SUPPLIER :")?.value;
                                                                                dataToInsert["officeAddress_1"] = GetValueForName(targetForm, "Registered Office Address_1");
                                                                                dataToInsert["lptcName"] = GetValueForName(targetForm, "LPTC Name");
                                                                                dataToInsert["lptcName_1"] = GetValueForName(targetForm, "LPTC Name1");
                                                                                dataToInsert["glc"] = GetValueForName(targetForm, "GLC");
                                                                                dataToInsert["govtBody"] = GetValueForName(targetForm, "Government Body");
                                                                                dataToInsert["jv_or_partnership"] =  GetValueForName(targetForm, "JV/Partnership");
                                                                                dataToInsert["solePro_or_enterprise"] = GetValueForName(targetForm, "Sole Proprietory/Enterprise");
                                                                                dataToInsert["company_description"] = GetValueForName(targetForm, "Text25");
                                                                                
                                                                                for(int i=1; i<=7; i++){
                                                                                    string a = GetValueForName(targetForm, "G"+i);
                                                                                    if(a != null && a != ""){
                                                                                    dataToInsert["g"+i] = a;

                                                                                    }
                                                                                }
                                                                                for(int i=1; i<=9; i++){
                                                                                    string a = GetValueForName(targetForm, "CCE-00"+i);
                                                                                    string b = GetValueForName(targetForm, "CEW-00"+i);
                                                                                    string cms = GetValueForName(targetForm, "CMS-00"+i);
                                                                                    string cbs = GetValueForName(targetForm, "CBS-00"+i);
                                                                                    string cma = GetValueForName(targetForm, "CMA-00"+i);
                                                                                    string cme = GetValueForName(targetForm, "CME-00"+i);
                                                                                    string csp = GetValueForName(targetForm, "CSP-00"+i);
                                                                                    string sup = GetValueForName(targetForm, "SUP-00"+i);

                                                                                    if(a != null && a != ""){
                                                                                        dataToInsert["cce_00"+i] = a;

                                                                                    }

                                                                                    if(b != null && b != ""){
                                                                                        dataToInsert["cew_00"+i] = b;
                                                                                    }

                                                                                    if(cms != null && cms != ""){
                                                                                        dataToInsert["cms_00"+i] = cms;

                                                                                    }
                                                                                    if(cbs != null && cbs != ""){
                                                                                        dataToInsert["cbs_00"+i] = cbs;

                                                                                    }
                                                                                    if(cma != null && cma != ""){
                                                                                        dataToInsert["cbs_00"+i] = cma;

                                                                                    }
                                                                                    if(cme != null && cme != ""){
                                                                                        dataToInsert["cbs_00"+i] = cme;

                                                                                    }
                                                                                    if(csp != null && csp != ""){
                                                                                        dataToInsert["csp_00"+i] = csp;

                                                                                    }
                                                                                    if(sup != null && sup != ""){
                                                                                        dataToInsert["sup_00"+i] = sup;

                                                                                    }
                                                                                }

                                                                                for(int i=10; i<=20; i++){
                                                                                    string a = GetValueForName(targetForm, "CCE-0"+i);
                                                                                    string b = GetValueForName(targetForm, "CEW-0"+i);
                                                                                    string cms = GetValueForName(targetForm, "CMS-0"+i);
                                                                                    string cbs = GetValueForName(targetForm, "CBS-0"+i);
                                                                                    string cma = GetValueForName(targetForm, "CMA-0"+i);
                                                                                    string cme = GetValueForName(targetForm, "CME-0"+i);
                                                                                    string csp = GetValueForName(targetForm, "CSP-0"+i);
                                                                                    string sup = GetValueForName(targetForm, "SUP-0"+i);

                                                                                    if(a != null && a != ""){
                                                                                        dataToInsert["cce_0"+i] = a;

                                                                                    }

                                                                                    if(b != null && b != ""){
                                                                                        dataToInsert["cew_0"+i] = b;
                                                                                    }

                                                                                    if(cms != null && cms != ""){
                                                                                        dataToInsert["cms_0"+i] = cms;

                                                                                    }
                                                                                    if(cbs != null && cbs != ""){
                                                                                        dataToInsert["cbs_0"+i] = cbs;

                                                                                    }
                                                                                    if(cma != null && cma != ""){
                                                                                        dataToInsert["cbs_0"+i] = cma;

                                                                                    }
                                                                                    if(cme != null && cme != ""){
                                                                                        dataToInsert["cbs_0"+i] = cme;

                                                                                    }
                                                                                    if(csp != null && csp != ""){
                                                                                        dataToInsert["csp_0"+i] = csp;

                                                                                    }
                                                                                    if(sup != null && sup != ""){
                                                                                        dataToInsert["sup_0"+i] = sup;

                                                                                    }
                                                                                }

                                                                            
                                                                            
                                                                            }
                                                                            else
                                                                            {
                                                                                // Form not found
                                                                                Console.WriteLine("Form not found with the specified criteria.");
                                                                            }

                                                                            targetTemplateName = "AVL Evaluation Sheet";
                                                                            // Find the form with the specific description and template name
                                                                            FormData targetFormAvl = forms.FirstOrDefault(form =>
                                                                                form.description == targetDescription &&
                                                                                form.formTemplate.name == targetTemplateName);
                                                                                if(targetFormAvl != null){
                                                                                    Console.WriteLine(GetValueForName(targetFormAvl, "Ramco_No"));
                                                                                    dataToInsert["ramco_no"] = GetValueForName(targetFormAvl, "Ramco_No");
                                                                                }else{
                                                                                    Console.WriteLine("Form not found with the specified criteria AVL.");

                                                                                }
                                                                                                                       
                                                                            }
                                                                    if (content1.type == "folders"  && content1.attributes.displayName == "Company Details (Catalogue, Machinery List, Certificate)"  ){
                                                                            dataToInsert["company_details"] = content1.links.self.href;
                                                                            //Console.WriteLine(content1.links.self.href);

                                                                            }
                                                                    if (content1.type == "folders"  && content1.attributes.displayName == "Financial Details"  ){
                                                                            dataToInsert["financial_details"] = content1.links.self.href;
                                                                            //Console.WriteLine(content1.links.self.href);

                                                                            } 
                                                                    if (content1.type == "folders"  && content1.attributes.displayName == "Vendor Assessment Questionnaires (VAQ)"  ){
                                                                                Content[] contents_2 = await GetContents(token, content1.relationships.contents.links.related.href);
                                                                                foreach (Content content2 in contents_2)
                                                                                    {
                                                                                    // Console.WriteLine(content2.attributes.displayName.ToString() );

                                                                                        if (content2.type == "folders"  && content2.attributes.displayName == "2.0 Quality System"  ){
                                                                                            dataToInsert["quality_system"] = content2.links.self.href;
                                                                                        //  Console.WriteLine(content2.links.self.href);
                                                                                            // Console.WriteLine("quality_system");

                                                                                        }
                                                                                        if (content2.type == "folders"  && content2.attributes.displayName == "3.0 Safety, Health and Environmental"  ){
                                                                                            dataToInsert["safety_health_and_env"] = content2.links.self.href;
                                                                                            //Console.WriteLine(content1.links.self.href);
                                                                                        }
                                                                                        if (content2.type == "folders"  && content2.attributes.displayName == "4.0 Human Rights and Supply Chain"  ){
                                                                                            dataToInsert["human_rights"] = content2.links.self.href;
                                                                                            //Console.WriteLine(content1.links.self.href);
                                                                                        }

                                                                                    }

                                                                        

                                                                        }                         
                                                                
                                                                   
                                                                }
                                                                 int  t = 0;
                                                                    foreach (var kvp in dataToInsert)
                                                                    {
                                                                        t++;
                                                                        if(kvp.Key == "glc"){
                                                                            
                                                                            //{kvp.Value} : {kvp.Value.GetType()}
                                                                                Console.WriteLine($"{kvp.Key}:  ");
                                                                        } 
                                                                        if (kvp.Value != null)
                                                                                    {
                                                                                        Console.WriteLine($"{kvp.Key}: {kvp.Value.GetType()}");
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        Console.WriteLine($"{kvp.Key}: Value is null");
                                                                                        dataToInsert["glc"] = "";
                                                                                    }
                                                                    }
                                                                    if(t>8){
                                                                        InsertPdfDatatoTable(dataToInsert);
                                                                                
                                                                    }else{
                                                                    Console.WriteLine($"keys less than 8");
                                                                        
                                                                    }
                                                                                

                                                            }
                                                            catch(Exception err){
                                                                    Console.WriteLine($"Error in main: {err.Message}");

                                                            }

                                                        }
                                                    }

                                                } 
                                                

                                            }
                                        }
                                    return null;
                                }

                                return null;
                            }
                        }


    public static async Task<List<FormData>> GetForms(string accessToken, string projectId){
        try{
            string url = $"https://developer.api.autodesk.com/construction/forms/v1/projects/{projectId}/forms?";
            List<FormData> allProjects = new List<FormData>();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            while(url != "" && url != " "){
                try{
                
                HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic responseData = JsonConvert.DeserializeObject(jsonResponse);
                var projects = responseData.data.ToObject<FormData[]>();
                allProjects.AddRange(projects); 
                Console.WriteLine(responseData.pagination.limit);
                url = responseData.pagination.nextUrl;

            }
            else
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
            }
                }
                catch(Exception ex){
            url="";

                }
            }
            return allProjects;
        }
        catch(Exception ex){
            return null;
        }
    } 
        static int CheckExistingRow(string tableName, Dictionary<string, string> data)
    {
        string connectionString = "Data Source=bimageforge.database.windows.net;Initial Catalog=bimageforge;User ID=forge;Password=BimageNow2020";

         try
    {
        // Construct the SELECT query dynamically
        string query = $"SELECT COUNT(*) FROM {tableName} WHERE ";

        // List of keys to exclude
        var excludedKeys = new HashSet<string> { "cbs_005", "cbs_006","cbs_007", "cbs_008", "cbs_010" };

        // Exclude fields that you don't want to insert
        var filteredData = data.Where(kv => !excludedKeys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);


        string whereClause = string.Join(" AND ", filteredData.Select(kv => $"[{kv.Key}] = @{kv.Key}"));
        query += whereClause;

        using (SqlConnection connection = new SqlConnection(connectionString))
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            // Add parameters for conditions
            foreach (var kvp in filteredData)
            {
                command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
            }

            // Open the connection and execute the command
            connection.Open();
            object result = command.ExecuteScalar();

            // If result is not null, return the ID
            return result != null ? Convert.ToInt32(result) : -1;

            
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error in check existence: " + ex.Message);
        return -1;
    }
    }
        public static async Task<Project[]> GetProjects(string accessToken, string hubId)
        {
            try
            {
                string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubId}/projects";
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic responseData = JsonConvert.DeserializeObject(jsonResponse);
                    var projects = responseData.data.ToObject<Project[]>();
                    return projects;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    return new Project[] { new Project { Id = null, Name = $"Error: {response.StatusCode}, {errorResponse}" } };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetProjects: {ex.Message}");
                return null;
            }
        }

        public static async Task<Token3Legged> Update3LeggedTokenUsingAuthCode(string AuthCode, ILogger log)
 {
    try{

    
     log.LogInformation("Getting New AccessToken using AuthCode");

     HttpClient client = new HttpClient();
     client.BaseAddress = new Uri("https://developer.api.autodesk.com/");

    //  var kvp = new Dictionary<string, string>();
    //  kvp.Add("client_id", ForgeClientID);
    //  kvp.Add("client_secret", ForgeClientSecret);
    //  kvp.Add("grant_type", "authorization_code");
    //  kvp.Add("code", AuthCode);
    //  kvp.Add("redirect_uri", CallBackURL2);
    
string clientCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{ForgeClientID}:{ForgeClientSecret}"));

var requestData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", AuthCode),
                new KeyValuePair<string, string>("redirect_uri", CallBackURL)
            });

client.DefaultRequestHeaders.Add("Authorization", $"Basic {clientCredentials}");
     HttpResponseMessage response = await client.PostAsync("https://developer.api.autodesk.com/authentication/v2/token", requestData);
if (response.IsSuccessStatusCode)
            {
     string body = await response.Content.ReadAsStringAsync();
     JObject joResponse = JObject.Parse(body);
     Console.WriteLine(joResponse);

     Token3Legged tk3 = new Token3Legged();
     tk3.AccessToken3Legged = joResponse["access_token"].ToString();
     tk3.RefreshToken3Legged = joResponse["refresh_token"].ToString();
     tk3.AccessToken3LeggedExpiredDateTime = DateTime.UtcNow.AddSeconds(int.Parse(joResponse["expires_in"].ToString()));

     return tk3;
            }else{
     string body = await response.Content.ReadAsStringAsync();

                Console.WriteLine(body);
                Console.WriteLine(response.StatusCode);
                return null;
            }
    }
    catch(Exception ex){
        Console.WriteLine(ex);
        return null;
    }
 }

        public static async Task<Token3Legged> RefreshTokens(string refresh_token){
                try{

    

                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri("https://developer.api.autodesk.com/");

                
    
                    string clientCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{ForgeClientID}:{ForgeClientSecret}"));

                    var requestData = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("grant_type", "refresh_token"),
                            new KeyValuePair<string, string>("refresh_token", refresh_token)
                        });

                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {clientCredentials}");
                        HttpResponseMessage response = await client.PostAsync("https://developer.api.autodesk.com/authentication/v2/token", requestData);
                    if (response.IsSuccessStatusCode)
                                {
                        string body = await response.Content.ReadAsStringAsync();
                        JObject joResponse = JObject.Parse(body);
                        Console.WriteLine(joResponse);

                        Token3Legged tk3 = new Token3Legged();
                        tk3.AccessToken3Legged = joResponse["access_token"].ToString();
                        tk3.RefreshToken3Legged = joResponse["refresh_token"].ToString();
                        tk3.AccessToken3LeggedExpiredDateTime = DateTime.UtcNow.AddSeconds(int.Parse(joResponse["expires_in"].ToString()));
                        UpdateRefreshToken(tk3);
                        return tk3;
                        }else{
                            string body = await response.Content.ReadAsStringAsync();

                            Console.WriteLine(body);
                            Console.WriteLine(response.StatusCode);
                            return null;
                        }
                        }
                        catch(Exception ex){
                            Console.WriteLine(ex);
                            return null;
                        }
                    }

        public static string InsertTokens(Token3Legged tk3)
        {
            try{

            

            string connectionString = "Data Source=bimageforge.database.windows.net;Initial Catalog=bimageforge;User ID=forge;Password=BimageNow2020";

            // Create a new SqlConnection using the connection string
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Open the connection
                connection.Open();

                // Define the SQL INSERT statement
                string sql = @"INSERT INTO [BCCOK].[RefreshTokens] ([app], [client_id], [refreshtoken], [expiry], [tm8], [expiry8], [access_token], [msg])
                           VALUES (@App, @ClientId, @RefreshToken, @Expiry, @Tm8,  @Expiry8, @AccessToken, @Message)";

                // Create a new SqlCommand with the SQL statement and SqlConnection
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // Add parameters to the SqlCommand
                    command.Parameters.AddWithValue("@App", "MCC");
                    command.Parameters.AddWithValue("@ClientId", ForgeClientID);
                    command.Parameters.AddWithValue("@RefreshToken", tk3.RefreshToken3Legged);
                    command.Parameters.AddWithValue("@Expiry", tk3.AccessToken3LeggedExpiredDateTime); // Example for expiry
                    command.Parameters.AddWithValue("@Tm8", tk3.AccessToken3LeggedExpiredDateTime); // Example for expiry
                    command.Parameters.AddWithValue("@Expiry8", tk3.AccessToken3LeggedExpiredDateTime); // Example for expiry
                    command.Parameters.AddWithValue("@AccessToken", tk3.AccessToken3Legged);
                    command.Parameters.AddWithValue("@Message", "mcc pdf");

                    // Execute the SqlCommand
                    int rowsAffected = command.ExecuteNonQuery();

                    // Check if any rows were affected
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("Data inserted successfully.");
                        return "success";
                    }
                    else
                    {
                        Console.WriteLine("No rows were inserted.");
                        return "error";
                    }
                }
            }
            }
            catch(Exception ex){
                Console.WriteLine(ex);
                return null;
            }
        }
   

        static void UpdateRefreshToken(Token3Legged data)
    {
            string connectionString = "Data Source=bimageforge.database.windows.net;Initial Catalog=bimageforge;User ID=forge;Password=BimageNow2020";

        // Update query
        string updateSql = @"UPDATE [BCCOK].[RefreshTokens]
                            SET [expiry] = @Expiry,
                                [tm8] = @Tm8,
                                [expiry8] = @Expiry8,
                                [access_token] = @AccessToken,
                                [refreshtoken] = @RefreshToken
                            WHERE [client_id] = @ClientId";

        // Execute the update
        using (SqlConnection connection = new SqlConnection(connectionString))
        using (SqlCommand command = new SqlCommand(updateSql, connection))
        {
            try
            {
                // Open the connection
                connection.Open();

                // Add parameters
                command.Parameters.AddWithValue("@Expiry", data.AccessToken3LeggedExpiredDateTime);
                command.Parameters.AddWithValue("@Tm8", data.AccessToken3LeggedExpiredDateTime);
                command.Parameters.AddWithValue("@Expiry8", data.AccessToken3LeggedExpiredDateTime);
                command.Parameters.AddWithValue("@AccessToken", data.AccessToken3Legged);
                command.Parameters.AddWithValue("@RefreshToken", data.RefreshToken3Legged);
                command.Parameters.AddWithValue("@ClientId", ForgeClientID);

                // Execute the update query
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine($"{rowsAffected} row(s) updated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }

        public static Token3Legged GetSecretFromTable()
{
    string tsql = $"select top 1 refreshtoken , expiry, access_token from BCCOK.RefreshTokens where refreshtoken is not null and client_id = '{ForgeClientID}' order by tm8 desc";
    var cb = new SqlConnectionStringBuilder();
    cb.DataSource = "bimageforge.database.windows.net";
    cb.UserID = "forge";
    cb.Password = "BimageNow2020";
    cb.InitialCatalog = "bimageforge";//database name
    cb.ConnectTimeout = 0;

    using (var connection = new SqlConnection(cb.ConnectionString))
    {
        connection.Open();
        using (var command = new SqlCommand(tsql, connection))
        {
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.IsDBNull(0))
                    {
                        return null;
                    }
                    Token3Legged tk3 = new Token3Legged();
                    Console.WriteLine(reader.GetString(0));
                    tk3.AccessToken3LeggedExpiredDateTime = reader.GetDateTime(1);
                    tk3.RefreshToken3Legged = reader.GetString(0);
                    tk3.AccessToken3Legged = reader.GetString(2);
                    return tk3;
                }
            }
        }
        connection.Close();
    }
    return null;
}
   
    

        public class Token3Legged
{
    public string AccessToken3Legged = "";
    public string RefreshToken3Legged = "";
    public DateTime AccessToken3LeggedExpiredDateTime;
    public StringBuilder sb = null;
}


        public static async Task<string> GetS3url(String token, String objectid)
    {
        try
        {
            String url  = $"https://developer.api.autodesk.com/oss/v2/buckets/wip.dm.prod/objects/{objectid}/signeds3download";
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic responseData = JsonConvert.DeserializeObject(jsonResponse);
                var s3url = responseData.url;
                return s3url;
            }
            else
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetS3url: {ex.Message}");
            return null;
        }
    }


        public static void InsertPdfDatatoTable(Dictionary<string, string> data)
        {

            Console.WriteLine("*************** InsertPdfDatatoTable ***************");
            try
            {
                string connectionString = "Data Source=bimageforge.database.windows.net;Initial Catalog=bimageforge;User ID=forge;Password=BimageNow2020";

                string tableName = "BCCOK.MCCPDFDATA";
                int exist =CheckExistingRow(tableName, data);
                // Construct the INSERT query dynamically
                if(exist > 0){
                        UpdateDataInTable(tableName, data, exist);
                }else{

                   
        // List of keys to exclude
        var excludedKeys = new HashSet<string> { "cbs_005", "cbs_006","cbs_007", "cbs_008", "cbs_010" };

        // Exclude fields that you don't want to insert
        var filteredData = data.Where(kv => !excludedKeys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

                // Construct the INSERT query dynamically
                // Construct the INSERT query dynamically
                string columns = string.Join(", ", filteredData.Keys.Select(key => "[" + key + "]"));
                string values = string.Join(", ", filteredData.Keys.Select(key => "@" + key));
                string query = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

                Console.WriteLine(query);

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    try
                    {
                        command.CommandTimeout = 300; // Set timeout to 300 seconds (5 minutes)

                        // Open the connection
                        connection.Open();

                        // Add parameters for each property
                        foreach (var kvp in filteredData)
                        {
                            command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
                        }
                       //  command.Parameters.AddWithValue("@glc", DBNull.Value);


                        // Execute the INSERT query
                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"{rowsAffected} row(s) inserted.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error in parameter add : " + ex.Message);
                    }
                }

                /* string columns = "";
                string values = "";
                
                foreach (var property in data.GetType().GetProperties())
                    {
                        columns += $"[{property.Name}], ";
                        values += $"@{property.Name}, ";

                    }

                    // Remove the trailing comma and space
                    columns = columns.Remove(columns.Length - 2);
                    values = values.Remove(values.Length - 2);

                    query += $"{columns}) VALUES ({values})";
                    Console.WriteLine (query);

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        try
                        {
                            // Open the connection
                            connection.Open();

                            // Add parameters for each property
                            foreach (var property in data.GetType().GetProperties())
                            {
                                command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(data));
                            }

                            // Execute the INSERT query
                            int rowsAffected = command.ExecuteNonQuery();
                            Console.WriteLine($"{rowsAffected} row(s) inserted.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }
                    } */
                } 

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                //return ex.ToString();
            }
        }

        static void UpdateDataInTable(string tableName, Dictionary<string, string> data , int id)
    {
        // Construct the UPDATE query dynamically

        string connectionString = "Data Source=bimageforge.database.windows.net;Initial Catalog=bimageforge;User ID=forge;Password=BimageNow2020";


        try
    {
         // List of keys to exclude
        var excludedKeys = new HashSet<string> { "cbs_005", "cbs_006","cbs_007", "cbs_008", "cbs_010" };

        // Exclude fields that you don't want to insert
        var filteredData = data.Where(kv => !excludedKeys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);


        // Construct the UPDATE query dynamically
        string query = $"UPDATE {tableName} SET ";
        string setValues = string.Join(", ", filteredData.Select(kv => $"[{kv.Key}] = @{kv.Key}"));
        query += $"{setValues} WHERE [Id] = @Id";

        using (SqlConnection connection = new SqlConnection(connectionString))
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            // Add parameters for SET values
            foreach (var kvp in filteredData)
            {
                command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
            }

            // Add parameter for the ID
            command.Parameters.AddWithValue("@Id", id);

            // Open the connection and execute the command
            connection.Open();
            int rowsAffected = command.ExecuteNonQuery();
            Console.WriteLine($"{rowsAffected} row(s) updated.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error in update query: " + ex.Message);
    }
    }
        public static async Task<string> GetAccessTokenAsync()
    {
        string connectionString = "Data Source=bimageforge.database.windows.net;Initial Catalog=bimageforge;User ID=forge;Password=BimageNow2020";
        string accessToken = "";

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            using (var command = new SqlCommand("SELECT access_token FROM [BCCOK].[AccLoginTokens] WHERE expiry > GETDATE() AND company = 'MMC' AND purpose = 'avl_dashboard'", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    accessToken = reader.GetString(0);
                }
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                using (var command = new SqlCommand("SELECT headers FROM [BCCOK].[AccLoginTokens] WHERE company = 'MMC' AND purpose = 'avl_dashboard'", connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var headersJson = reader.GetString(0);
                        // Console.WriteLine(headersJson);
                        var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
                        using (var httpClient = new HttpClient())
                        {
                            /* var response = await httpClient.GetAsync("https://login.acc.autodesk.com/api/v1/authentication/refresh?currentUrl=https%3A%2F%2Facc.autodesk.com%2Fprojects", headers);
                            var json = await response.Content.ReadAsStringAsync();
                            var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                            accessToken = result["accessToken"];
                            var expiry = result["expiresAt"];

                            using (var updateCommand = new SqlCommand($"UPDATE [BCCOK].[AccLoginTokens] SET access_token = '{accessToken}', expiry = '{expiry}' WHERE company = 'MMC' AND purpose = 'email'", connection))
                            {
                                await updateCommand.ExecuteNonQueryAsync();
                            } */
                        
                        var request = new HttpRequestMessage(HttpMethod.Get, "https://login.acc.autodesk.com/api/v1/authentication/refresh?currentUrl=https%3A%2F%2Facc.autodesk.com%2Fprojects");

                        foreach (var header in headers)
                        {
                           

                            request.Headers.Add(header.Key, header.Value);
                        }

                        var response = await httpClient.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            // Console.WriteLine(json);
                            var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                            accessToken = result["accessToken"];
                            var expiry = result["expiresAt"];

                            using (var updateCommand = new SqlCommand($"UPDATE [BCCOK].[AccLoginTokens] SET access_token = '{accessToken}', expiry = '{expiry}' WHERE company = 'MMC' AND purpose = 'avl_dashboard'", connection))
                            {
                                await updateCommand.ExecuteNonQueryAsync();
                            }

                        }
                        else
                        {
                            // Handle error response
                            return null;
                        } 
                        }
                    }
                }
            }
        }

        return accessToken;
    }

        public static async Task<Project[]> GetTopFolders(String token, String link)
        {
            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                HttpResponseMessage response = await client.GetAsync(link);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic responseData = JsonConvert.DeserializeObject(jsonResponse);
                    var projects = responseData.data.ToObject<Project[]>();
                    return projects;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    return new Project[] { new Project { Id = null, Name = $"Error: {response.StatusCode}, {errorResponse}" } };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTopFolders: {ex.Message}");
                return null;
            }
        }

        public static async Task<Content[]> GetContents(String token, String link)
        {
            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                HttpResponseMessage response = await client.GetAsync(link);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic responseData = JsonConvert.DeserializeObject(jsonResponse);
                    var projects = responseData.data.ToObject<Content[]>();
                    return projects;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    return new Content[] { new Content { Id = null, Name = $"Error: {response.StatusCode}, {errorResponse}" } };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetContents: {ex.Message}");
                return null;
            }
        }

        public static async Task<Project[]> GetItemData(String token, String url)
        {
            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic responseData = JsonConvert.DeserializeObject(jsonResponse);
                    // Console.WriteLine(responseData);
                    var projects = responseData.included.ToObject<Project[]>();
                    return projects;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    return new Project[] { new Project { Id = null, Name = $"Error: {response.StatusCode}, {errorResponse}" } };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetItemData: {ex.Message}");
                return null;
            }
        }

    public class FormField
{
	public string Name { get; set; }

	public string Value { get; set; }
}

public static class GeneralVariables
{
	public static string ClientId = "Ak5xhjoOVN80nIGnGXBgWtWf1LS6GbWA";

	public static string ClientSecret = "sbAM81dbuHiVieIF";

	public static string TokenEndpoint = "https://developer.api.autodesk.com/authentication/v2/token";
}
public class Project
{
    public string Id { get; set; }
    public Attributes attributes { get; set; }
    public string Name { get; set; }

    public Relationship relationships { get; set; }

    public Links links { get; set; }

    public Included included { get; set; }

    //  public string type { get; set; }

    // Add other properties as needed
}

public class Included
{
    public Links links { get; set; }
}
public class Content
{
    public string Id { get; set; }
    public Attributes attributes { get; set; }
    public string Name { get; set; }

    public Relationship relationships { get; set; }

    public string type { get; set; }

    public Links links {get;set;}

    // Add other properties as needed
}

public class Attributes
{
    public string name { get; set; }

    public string displayName { get; set; }

    public string lastModifiedTime {get; set;}
    
}
public class Relationship
{
    public RootFolderData rootFolder { get; set; }
    public Links topFolders { get; set; }
    public Links contents { get; set; }

    public Meta issues { get; set; }
    public Meta submittals { get; set; }
    public Meta rfis { get; set; }
    public Meta markups { get; set; }
    public Meta checklists { get; set; }
    public Meta cost { get; set; }
    public Meta locations { get; set; }

    public Meta storage { get; set; }

}

public class StorageData
{
    public String type { get; set; }
    public String id { get; set; }
}

public class Meta
{
    public StorageData data { get; set; }
}
public class Self
{
    public string href { get; set; }
}

public class Links
{
    public Self self { get; set; }
    public Self related { get; set; }
    public Links links { get; set; }
    public Links webView { get; set; }
    

}

public class RootFolderData
{
    public string Id { get; set; }
}

public class Relationships
{
    public RootFolderData RootFolder { get; set; }
}

public class ProjectData
{
    public Relationships Relationships { get; set; }
}

public class ProjectResponse
{
    public ProjectData[] Data { get; set; }
}

public class DataToInsert
{
    public string pdfName { get; set; }
    public string parentId { get; set; }
    public string parentName { get; set; }
    public string projectId { get; set; }
    public string pdflastupdated { get; set; }

    public string companyNameofSubContractor { get; set; }
    public string officeAddress_1 { get; set; }
    public string lptcName { get; set; }
    public string lptcName_1 { get; set; }
    public string publicListed { get; set; }
    public string glc { get; set; }
    public string govtBody { get; set; }
    public string jv_or_partnership { get; set; }
    public string solePro_or_enterprise { get; set; }
    public string grade_or_class { get; set; }
    public string contractor { get; set; }
    public string consultant { get; set; }
    public string supplier { get; set; }
    public string g1 { get; set; }
    public string g2 { get; set; }
    public string g3 { get; set; }
    public string g4 { get; set; }
    public string g5 { get; set; }
    public string g6 { get; set; }
    public string g7 { get; set; }
    public string cce_001 { get; set; }
    public string cce_002 { get; set; }
    public string cce_003 { get; set; }
    public string cce_004 { get; set; }
    public string cce_020 { get; set; }
    public string cew_001 { get; set; }
    public string cew_002 { get; set; }
    public string cew_003 { get; set; }
    public string cew_004 { get; set; }
    public string cew_005 { get; set; }
    public string cew_020 { get; set; }
    public string cms_001 { get; set; }
    public string cms_002 { get; set; }
    public string cms_003 { get; set; }
    public string cms_004 { get; set; }
    public string cms_005 { get; set; }
    public string cms_006 { get; set; }
    public string cms_007 { get; set; }
    public string cms_020 { get; set; }
    public string cbs_020 { get; set; }
    public string cbs_001 { get; set; }
    public string cbs_002 { get; set; }
    public string cbs_003 { get; set; }
    public string cbs_004 { get; set; }
    public string cma_020 { get; set; }
    public string cma_001 { get; set; }
    public string cma_002 { get; set; }
    public string cma_003 { get; set; }
    public string cma_004 { get; set; }
    public string cma_005 { get; set; }
    public string cma_006 { get; set; }
    public string cme_020 { get; set; }
    public string cme_001 { get; set; }
    public string cme_002 { get; set; }
    public string cme_003 { get; set; }
    public string cme_004 { get; set; }
    public string cme_005 { get; set; }
    public string cme_006 { get; set; }
    public string cme_007 { get; set; }
    public string cme_008 { get; set; }
    public string cme_009 { get; set; }
    public string cme_010 { get; set; }
    public string csp_001 { get; set; }
    public string csp_002 { get; set; }
    public string csp_003 { get; set; }
    public string csp_004 { get; set; }
    public string csp_005 { get; set; }
    public string csp_006 { get; set; }
    public string csp_020 { get; set; }
    public string sup_001 { get; set; }
    public string sup_002 { get; set; }
    public string sup_003 { get; set; }
    public string sup_004 { get; set; }
    public string sup_005 { get; set; }
    public string sup_006 { get; set; }
    public string sup_007 { get; set; }
    public string sup_008 { get; set; }
    public string sup_009 { get; set; }
    public string sup_010 { get; set; }
    public string sup_011 { get; set; }
    public string sup_020 { get; set; }
    public string trim_parent_name { get; set; }
    public string company_description { get; set; }
    public string form_display_id { get; set; }
    public string form_id { get; set; }
    public string ramco_no { get; set; }
    public string company_details { get; set; }
    public string financial_details { get; set; }
    public string quality_system { get; set; }
    public string safety_health_and_env { get; set; }
    public string human_rights { get; set; }
}

public class FormData{
    public string assigneeId { get; set; }
    public string assigneeType { get; set; }
    public DateTime createdAt { get; set; }
    public string createdBy { get; set; }
    public string description { get; set; }
    public DateTime? dueDate { get; set; }
    public DateTime formDate { get; set; }
    public int formNum { get; set; }
    public FormTemplate formTemplate { get; set; }

    public List<PdfValues> pdfValues{get; set;}

    public string id {get; set;}
}

public class PdfValues{
    public string name { get; set; }
    public string value { get; set; }
}
public class FormTemplate
{
    public string id { get; set; }
    public string name { get; set; }
    public string projectId { get; set; }
    public string status { get; set; }
    public string templateType { get; set; }
}
 
    }

}
