using System;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.Search;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Threading;

namespace AzureSearchTemplate
{
    class Program
    {
        // Configurable names
        private const string DataSourceName = "[DataSource]";
        private const string IndexName = "[Index]";
        private const string SkillSetName = "[SkillSet]";
        private const string IndexerName = "[Indexer]";
        private const string SynonymMapName = "[Synonym]";
        private const string BlobContainerNameForImageStore = "[BlobContainer]";

        // Set this to true to see additional debugging information in the console.
        private static bool DebugMode = true;

        // Clients
        private static ISearchServiceClient _searchClient;
        private static HttpClient _httpClient = new HttpClient();
        private static string _searchServiceEndpoint;
        static void Main(string[] args)
        {
            string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

            _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
            _searchServiceEndpoint = String.Format("https://{0}.{1}", searchServiceName, _searchClient.SearchDnsSuffix);

            bool result = RunAsync().GetAwaiter().GetResult();
            if (!result && !DebugMode)
            {
                Console.WriteLine("Something went wrong.  Set 'DebugMode' to true in order to see traces.");
            }
            else if (!result)
            {
                Console.WriteLine("Something went wrong.");
            }
            else
            {
                Console.WriteLine("All operations were successful.");
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
        private static async Task<bool> RunAsync()
        {
            bool result = true;
            result = await DeleteIndexingResources();
            if (!result)
                return result;
            result = await CreateDataSource();
            if (!result)
                return result;
            result = await CreateSkillSet();
            if (!result)
                return result;
            //result = await CreateSynonyms();
            //if (!result)
            //    return result;
            result = await CreateIndex();
            if (!result)
                return result;
            result = await CreateIndexer();
            if (!result)
                return result;
            result = await CheckIndexerStatus();
            if (!result)
                return result;
            result = await QueryIndex();
            return result;
        }
        private static async Task<bool> DeleteIndexingResources()
        {
            Console.WriteLine("Deleting Data Source, Index, Indexer and SynonymMap if they exist...");
            try
            {
                await _searchClient.DataSources.DeleteAsync(DataSourceName);
                await _searchClient.Indexes.DeleteAsync(IndexName);
                await _searchClient.Indexers.DeleteAsync(IndexerName);
                await _searchClient.SynonymMaps.DeleteAsync(SynonymMapName);
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error deleting resources: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }
        private static async Task<bool> CreateDataSource()
        {
            Console.WriteLine("Creating Data Source...");
            try
            {
                DataSource dataSource = DataSource.AzureBlobStorage(
                    name: DataSourceName,
                    storageConnectionString: ConfigurationManager.AppSettings["DataSourceBlobStorageAccountConnectionString"],
                    containerName: ConfigurationManager.AppSettings["DataSourceBlobContainerName"],
                    description: "Blob Storage"
                );
                await _searchClient.DataSources.CreateAsync(dataSource);
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error creating data source: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }
        private static async Task<bool> CreateSkillSet()
        {
            Console.WriteLine("Creating Skill Set...");
            try
            {
                using (StreamReader r = new StreamReader("skillset.json"))
                {
                    string json = r.ReadToEnd();
                    string uri = String.Format("{0}/skillsets/{1}?api-version=2017-11-11-Preview", _searchServiceEndpoint, SkillSetName);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await _httpClient.PutAsync(uri, content);
                    if (DebugMode)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Create Skill Set response: \n{0}", responseText);
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error creating skillset: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }
        private static async Task<bool> CreateIndex()
        {
            Console.WriteLine("Creating Index...");
            try
            {
                using (StreamReader r = new StreamReader("index.json"))
                {
                    string json = r.ReadToEnd();
                    json = json.Replace("[SynonymMapName]", SynonymMapName);
                    string uri = String.Format("{0}/indexes/{1}?api-version=2017-11-11-Preview", _searchServiceEndpoint, IndexName);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await _httpClient.PutAsync(uri, content);
                    if (DebugMode)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Create Index response: \n{0}", responseText);
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error creating index: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }
        private static async Task<bool> CreateIndexer()
        {
            Console.WriteLine("Creating Indexer...");
            try
            {
                using (StreamReader r = new StreamReader("indexer.json"))
                {
                    string json = r.ReadToEnd();
                    json = json.Replace("[IndexerName]", IndexerName);
                    json = json.Replace("[DataSourceName]", DataSourceName);
                    json = json.Replace("[IndexName]", IndexName);
                    json = json.Replace("[SkillSetName]", SkillSetName);
                    string uri = String.Format("{0}/indexers/{1}?api-version=2017-11-11-Preview", _searchServiceEndpoint, IndexerName);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await _httpClient.PutAsync(uri, content);
                    if (DebugMode)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Create Indexer response: \n{0}", responseText);
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error creating indexer: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }
        private static async Task<bool> CheckIndexerStatus()
        {
            Console.WriteLine("Waiting for indexing to complete...");
            IndexerExecutionStatus requestStatus = IndexerExecutionStatus.InProgress;
            try
            {
                await _searchClient.Indexers.GetAsync(IndexerName);
                while (requestStatus.Equals(IndexerExecutionStatus.InProgress))
                {
                    Thread.Sleep(3000);
                    IndexerExecutionInfo info = await _searchClient.Indexers.GetStatusAsync(IndexerName);
                    requestStatus = info.LastResult.Status;
                    if (DebugMode)
                    {
                        Console.WriteLine("Current indexer status: {0}", requestStatus.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error retrieving indexer status: {0}", ex.Message);
                }
                return false;
            }
            return requestStatus.Equals(IndexerExecutionStatus.Success);
        }

        private static async Task<bool> QueryIndex()
        {
            Console.WriteLine("Querying Index...");
            try
            {
                ISearchIndexClient indexClient = _searchClient.Indexes.GetClient(IndexName);
                DocumentSearchResult searchResult = await indexClient.Documents.SearchAsync("*");
                Console.WriteLine("Query Results:");
                foreach (SearchResult result in searchResult.Results)
                {
                    foreach (string key in result.Document.Keys)
                    {
                        Console.WriteLine("{0}: {1}", key, result.Document[key]);
                    }
                }
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error querying index: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }
    }
}
