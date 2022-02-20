using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Reservator.Services
{
    public class CountryConfig
    {
        public string Name { get; set; }
        public string Emoji { get; set; }
        public string Side { get; set; }
        public bool Major { get; set; }
    }

    public class CountryConfigContainer
    {
        [JsonPropertyName("countries")]
        public List<CountryConfig> Countries { get; set; }
    }
    
    public class CountryConfigService
    {
        public readonly CountryConfigContainer CountryConfig;

        public CountryConfigService()
        {
            const string fileName = "countries.json";
            var jsonString = File.ReadAllText(fileName);
            CountryConfig = JsonSerializer.Deserialize<CountryConfigContainer>(jsonString);
        }
    }
}