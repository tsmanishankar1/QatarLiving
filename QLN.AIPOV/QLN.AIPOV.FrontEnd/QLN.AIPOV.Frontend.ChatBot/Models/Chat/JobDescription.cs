using System.Text.Json.Serialization;

namespace QLN.AIPOV.Frontend.ChatBot.Models.Chat
{
    public class JobDescription
    {
        [JsonPropertyName("Job_Title")]
        public string? JobTitle { get; set; }

        [JsonPropertyName("Job_Purpose")]
        public string? JobPurpose { get; set; }

        [JsonPropertyName("Job_Duties_and_Responsibilities")]
        public string? JobDutiesAndResponsibilities { get; set; }

        [JsonPropertyName("Required_Qualifications")]
        public string? RequiredQualifications { get; set; }

        [JsonPropertyName("Preferred_Qualifications")]
        public string? PreferredQualifications { get; set; }

        [JsonPropertyName("Working_Conditions")]
        public string? WorkingConditions { get; set; }
    }
}
