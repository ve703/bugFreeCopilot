using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;

namespace SeleniumTests
{
    /// <summary>
    /// Service for creating bug work items in Azure DevOps via REST API.
    /// </summary>
    public class AzureDevOpsBugCreator
    {
        private readonly string azureDevOpsUrl;
        private readonly string project;
        private readonly string personalAccessToken;
        private readonly string defaultAssignee;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureDevOpsBugCreator"/> class by reading configuration values from an appsettings.json file.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the appsettings.json file does not exist.</exception>
        /// <exception cref="Exception">Thrown if configuration is missing required fields.</exception>
        public AzureDevOpsBugCreator()
        {
            try
            {
                var configText = File.ReadAllText("appsettings.json");
                dynamic settings = JsonConvert.DeserializeObject(configText);
                azureDevOpsUrl = settings.AzureDevOpsUrl ?? throw new Exception("AzureDevOpsUrl missing in appsettings.json");
                project = settings.Project ?? throw new Exception("Project missing in appsettings.json");
                personalAccessToken = settings.PersonalAccessToken ?? throw new Exception("PersonalAccessToken missing in appsettings.json");
                defaultAssignee = settings.DefaultAssignee ?? "shahab@tecoholic.com";
            }
            catch (FileNotFoundException fnf)
            {
                Console.WriteLine($"Configuration file not found: {fnf.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a bug in Azure DevOps with a specified title and optional assignee.
        /// </summary>
        /// <param name="bugTitle">The title of the bug.</param>
        /// <param name="assignee">Email of the person to assign the bug to (optional).</param>
        public void CreateBug(string bugTitle, string assignee = null)
        {
            try
            {
                var client = new RestClient($"{azureDevOpsUrl}/{project}/_apis/wit/workitems/$Bug?api-version=6.0");
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json-patch+json");
                string authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"));
                request.AddHeader("Authorization", $"Basic {authToken}");

                // Collect bug fields
                var bugData = new[]
                {
                    new { op = "add", path = "/fields/System.Title", value = bugTitle },
                    new { op = "add", path = "/fields/System.Description", value = "Bug created automatically due to failed Selenium test." },
                    new { op = "add", path = "/fields/System.AssignedTo", value = assignee ?? defaultAssignee },
                    new { op = "add", path = "/fields/Microsoft.VSTS.TCM.ReproSteps", value = "See attached logs for detailed error." }
                };

                request.AddParameter("application/json-patch+json", JsonConvert.SerializeObject(bugData), ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    Console.WriteLine("Bug created successfully in Azure DevOps.");
                }
                else
                {
                    Console.WriteLine("Failed to create bug: " + response.ErrorMessage);
                    if (!string.IsNullOrEmpty(response.Content))
                        Console.WriteLine("Response content: " + response.Content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred in CreateBug: {ex.Message}");
            }
        }
    }
}
