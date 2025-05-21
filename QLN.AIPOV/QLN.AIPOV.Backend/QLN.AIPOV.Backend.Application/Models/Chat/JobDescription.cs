using System.Text.Json.Serialization;

namespace QLN.AIPOV.Backend.Application.Models.Chat
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class JobDescription
    {
        [JsonPropertyName("Job_Title")]
        public required string JobTitle { get; set; }

        [JsonPropertyName("Job_Purpose")]
        public required string JobPurpose { get; set; }

        [JsonPropertyName("Job_Duties_and_Responsibilities")]
        public required string JobDutiesAndResponsibilities { get; set; }

        [JsonPropertyName("Required_Qualifications")]
        public required string RequiredQualifications { get; set; }

        [JsonPropertyName("Preferred_Qualifications")]
        public required string PreferredQualifications { get; set; }

        [JsonPropertyName("Working_Conditions")]
        public required string WorkingConditions { get; set; }
    }
}
