

using System.Text.Json;
using Newtonsoft.Json;

// using Newtonsoft.Json;
using rps.Models;

namespace rps.Services
{
    public class GeneralService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GeneralService> _logger;
       
        public GeneralService(IHttpClientFactory httpClientFactory, ILogger<GeneralService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
           
        }
        
        // public async Task<List<CourseAllocation>> GetCourseAllocationsAsync(string recordType, int? departmentId)
        // {
        //     string apiUrl = $"https://edouniversity.edu.ng/api/v1/courseallocationsapi?record={recordType}&departmentId={departmentId}";
        //      string apiKey = "";
        //     var client = _httpClientFactory.CreateClient();
        //     client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        //     try
        //     {
        //         var response = await client.GetAsync(apiUrl);
        //         response.EnsureSuccessStatusCode();

        //         var content = await response.Content.ReadAsStringAsync();
        //         Console.WriteLine(content);
        //         return  JsonConvert.DeserializeObject<List<CourseAllocation>>(content);
                
        //     }
        //     catch (Exception ex)
        //     {
        //          _logger.LogError($"Error fetching course allocations: {ex.Message}");
        //         return new List<CourseAllocation>(); // Return empty list on failure
        //     }
        // }
    }
}