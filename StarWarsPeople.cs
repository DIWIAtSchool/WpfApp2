using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WpfApp2
{
    internal class StarWarsPeople
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("height")]
        public string Height { get; set; }
        [JsonPropertyName("mass")]
        public string Mass { get; set; }
        [JsonPropertyName("gender")]
        public string Gender { get; set; }
    }
}
