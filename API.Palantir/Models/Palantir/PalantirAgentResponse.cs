using Newtonsoft.Json;

namespace API.Palantir.Models.Palantir
{
    public class PalantirAgentResponse
    {
        [JsonProperty("agentMarkdownResponse")]
        public string? AgentMarkdownResponse { get; set; }

        [JsonProperty("sessionTraceId")]
        public string? SessionTraceId { get; set; }
    }
}
