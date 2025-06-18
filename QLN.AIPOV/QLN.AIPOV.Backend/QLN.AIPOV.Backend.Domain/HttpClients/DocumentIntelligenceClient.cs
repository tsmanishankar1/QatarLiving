using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.Config;
using QLN.AIPOV.Backend.Application.Models.FormRecognition;
using System.Text.RegularExpressions;

namespace QLN.AIPOV.Backend.Domain.HttpClients
{
    public class DocumentIntelligenceClient : IDocumentIntelligenceClient
    {
        private readonly DocumentAnalysisClient _documentAnalysisClient;
        private readonly DocumentIntelligenceSettingsModel _settings;
        private readonly SearchClient _searchClient;
        private readonly IEmbeddingService _embeddingService;

        public DocumentIntelligenceClient(IOptions<DocumentIntelligenceSettingsModel> settings,
            SearchClient searchClient,
            IEmbeddingService embeddingService)
        {
            _settings = settings.Value;
            _documentAnalysisClient = new DocumentAnalysisClient(
                new Uri(_settings.Endpoint),
                new AzureKeyCredential(_settings.ApiKey));
            _searchClient = searchClient ??
                            throw new ArgumentNullException(nameof(searchClient), "Search client cannot be null.");
            _embeddingService = embeddingService;
        }

        public async Task<CVData> ExtractCVDataAsync(Stream documentStream, string contentType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Start the analysis process with proper content type
                var operation = await _documentAnalysisClient.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    _settings.ModelId,
                    documentStream,
                    cancellationToken: cancellationToken);

                // Get the result
                var result = operation.Value;

                // Create CV data object
                var cvData = new CVData { IsSuccessful = true };

                // Extract document content as full text
                if (result.Content != null)
                {
                    cvData.ResumeText = result.Content;
                }

                if (!result.KeyValuePairs.Any())
                    return new CVData
                    {
                        IsSuccessful = false,
                        ErrorMessage = "No key-value pairs found in the document."
                    };

                // pass extracted data to search index
                var document = new SearchDocument { { "id", Guid.NewGuid().ToString() } };
                foreach (var keyValuePair in result.KeyValuePairs)
                {
                    switch (keyValuePair.Key.Content)
                    {
                        case "Full Name" when keyValuePair.Value != null &&
                                              !string.IsNullOrEmpty(keyValuePair.Value.Content):
                            {
                                var fullNameContent = keyValuePair.Value.Content;
                                if (fullNameContent.Contains(" "))
                                {
                                    var fullNameParts = fullNameContent.Split(" ");
                                    document.Add("FirstName", fullNameParts[0]);
                                    document.Add("LastName", fullNameParts[^1]);
                                    cvData.Name = fullNameParts[0];
                                    cvData.Surname = fullNameParts[^1];
                                }

                                break;
                            }
                        case "Name" or "First Name" or "FirstName" when keyValuePair.Value != null &&
                                                                        !string.IsNullOrEmpty(
                                                                            keyValuePair.Value.Content):
                            {
                                document.Add("FirstName", keyValuePair.Value.Content);
                                cvData.Name = keyValuePair.Value.Content;
                            }
                            break;
                        case "LastName" or "Last Name" or "Surname" when keyValuePair.Value != null &&
                                                                         !string.IsNullOrEmpty(keyValuePair.Value
                                                                             .Content):
                            {
                                document.Add("LastName", keyValuePair.Value.Content);
                                cvData.Surname = keyValuePair.Value.Content;
                            }
                            break;
                        case "Email" or "Email Address" or "EmailAddress" when keyValuePair.Value != null &&
                                                                               !string.IsNullOrEmpty(keyValuePair.Value
                                                                                   .Content):
                            {
                                document.Add("Email", keyValuePair.Value.Content);
                                cvData.Email = keyValuePair.Value.Content;
                            }
                            break;
                        case "Mobile Number" or "MobileNumber" or "Cell" or "Cell No" or "CellNo" or "Contact Number"
                            or "ContactNumber" when keyValuePair.Value != null &&
                                                    !string.IsNullOrEmpty(keyValuePair.Value.Content):
                            {
                                document.Add("MobileNumber", keyValuePair.Value.Content);
                                cvData.ContactNumber = keyValuePair.Value.Content;
                            }
                            break;
                    }
                }

                // Extract work history from the document
                var workHistoryEntries = ExtractWorkHistory(result);
                if (workHistoryEntries.Count > 0)
                {
                    // Add only the most recent job (first in the list) as the WorkHistory complex type
                    document.Add("WorkHistory", workHistoryEntries[0]);

                    // Store all work history as a concatenated string in WorkExperience field
                    var workExperience = string.Join(" | ", workHistoryEntries.Select(wh =>
                        $"{wh["Company"]} - {wh["Position"]} ({wh["Period"]})"));
                    document.Add("WorkExperience", workExperience);
                }

                var resumeText = cvData.ResumeText;
                var skills = ExtractSkills(result);

                // Generate embeddings for the content
                //var contentEmbeddings = await _embeddingService.GenerateEmbeddingsAsync(resumeText, cancellationToken);
                //document.Add("contentVector", contentEmbeddings);

                //if (!string.IsNullOrEmpty(skills))
                //{
                //    var skillsEmbeddings = await _embeddingService.GenerateEmbeddingsAsync(skills, cancellationToken);
                //    document.Add("skillsVector", skillsEmbeddings);
                //    document.Add("Skills", skills);
                //}

                var response = await _searchClient.UploadDocumentsAsync(new[] { document }, cancellationToken: cancellationToken);

                if (response.Value.Results.Any(r => r.Succeeded == false))
                {
                    return new CVData
                    {
                        IsSuccessful = false,
                        ErrorMessage = $"Error indexing document: {response.Value.Results.First(r => r.Succeeded == false).ErrorMessage}"
                    };
                }

                cvData.IsSuccessful = true;
                return cvData;
            }
            catch (Exception ex)
            {
                return new CVData
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Error analyzing document: {ex.Message}"
                };
            }
        }

        private List<SearchDocument> ExtractWorkHistory(AnalyzeResult result)
        {
            var workHistoryList = new List<SearchDocument>();
            var inWorkHistorySection = false;
            SearchDocument? currentJob = null;
            var lastLabel = string.Empty;

            // First try to identify work history section
            if (result.Paragraphs == null) return workHistoryList;

            for (var i = 0; i < result.Paragraphs.Count; i++)
            {
                var paragraph = result.Paragraphs[i];
                if (paragraph.Content == null) continue;
                var content = paragraph.Content.Trim();

                // Check for section headers
                if (Regex.IsMatch(content, @"^(EMPLOYMENT HISTORY|FULLTIME EMPLOYMENT HISTORY)$",
                        RegexOptions.IgnoreCase))
                {
                    inWorkHistorySection = true;
                    continue;
                }
                // Check for end of work history section
                else if (inWorkHistorySection && Regex.IsMatch(content,
                             @"^(EDUCATION|OTHER EMPLOYMENT INFORMATION|EDUCATION & TRAINING)$",
                             RegexOptions.IgnoreCase))
                {
                    inWorkHistorySection = false;
                    continue;
                }

                if (!inWorkHistorySection) continue;

                // Check for labels
                if (content.Equals("Company", StringComparison.OrdinalIgnoreCase))
                {
                    // Start a new job entry
                    if (currentJob != null)
                    {
                        workHistoryList.Add(currentJob);
                    }

                    currentJob = new SearchDocument();
                    lastLabel = "Company";
                }
                else if (content.Equals("Period", StringComparison.OrdinalIgnoreCase))
                {
                    lastLabel = "Period";
                }
                else if (content.Equals("Position", StringComparison.OrdinalIgnoreCase))
                {
                    lastLabel = "Position";
                }
                else if (content.Equals("Duties", StringComparison.OrdinalIgnoreCase))
                {
                    lastLabel = "Duties";
                }
                // Handle values (content after a label)
                else if (!string.IsNullOrEmpty(lastLabel) && currentJob != null)
                {
                    // Skip "For a more detailed description..." lines
                    if (content.StartsWith("For a more detailed description", StringComparison.OrdinalIgnoreCase))
                    {
                        lastLabel = string.Empty;
                        continue;
                    }

                    if (lastLabel == "Company")
                    {
                        currentJob["Company"] = content;
                    }
                    else if (lastLabel == "Period")
                    {
                        currentJob["Period"] = content;
                    }
                    else if (lastLabel == "Position")
                    {
                        currentJob["Position"] = content;
                    }
                    else if (lastLabel == "Duties")
                    {
                        // For duties, collect multiple lines
                        var dutiesList = new List<string> { content };
                        var nextIndex = i + 1;

                        // Continue until next label or section break
                        while (nextIndex < result.Paragraphs.Count)
                        {
                            var nextPara = result.Paragraphs[nextIndex];
                            if (nextPara.Content == null) break;

                            var nextContent = nextPara.Content.Trim();

                            if (nextContent.StartsWith("Confidential"))
                            {
                                nextIndex++;
                                continue;
                            }

                            // Check if we've reached a new label or section
                            if (nextContent.Equals("Company", StringComparison.OrdinalIgnoreCase) ||
                                nextContent.Equals("Period", StringComparison.OrdinalIgnoreCase) ||
                                nextContent.Equals("Position", StringComparison.OrdinalIgnoreCase) ||
                                nextContent.Equals("Duties", StringComparison.OrdinalIgnoreCase) ||
                                nextContent.StartsWith("For a more detailed", StringComparison.OrdinalIgnoreCase) ||
                                Regex.IsMatch(nextContent,
                                    @"^(EDUCATION|OTHER EMPLOYMENT INFORMATION|EDUCATION & TRAINING)$",
                                    RegexOptions.IgnoreCase))
                            {
                                break;
                            }

                            // Add this line as part of duties
                            dutiesList.Add(nextContent);
                            i = nextIndex; // Skip ahead since we've processed this paragraph
                            nextIndex++;
                        }

                        currentJob["Duties"] = string.Join(" | ", dutiesList.Where(d => !string.IsNullOrWhiteSpace(d)));
                        lastLabel = string.Empty; // Reset after collecting duties
                    }

                    // Reset the label after handling value
                    if (lastLabel != "Duties") lastLabel = string.Empty;
                }
            }

            // Add the last job if we have one
            if (currentJob != null)
            {
                workHistoryList.Add(currentJob);
            }

            return workHistoryList;
        }

        private string ExtractSkills(AnalyzeResult result)
        {
            var skillsList = new List<string>();
            var inSkillsSection = false;

            if (result.Paragraphs == null) return string.Empty;

            foreach (var paragraph in result.Paragraphs)
            {
                if (paragraph.Content == null) continue;
                var content = paragraph.Content.Trim();

                // Detect skills section headers
                if (Regex.IsMatch(content,
                        @"^(SKILLS|PRINCIPLE EXPOSURE & STRENGTHS|APPLICATIONS & LANGUAGES|CORE COMPETENCIES|TECHNICAL SKILLS)$",
                        RegexOptions.IgnoreCase))
                {
                    inSkillsSection = true;
                    continue;
                }
                // End of skills section
                else if (inSkillsSection && Regex.IsMatch(content,
                             @"^(EMPLOYMENT|EXPERIENCE|WORK HISTORY|EDUCATION|OTHER EMPLOYMENT INFORMATION|EDUCATION & TRAINING|ADDITIONAL INFORMATION)$",
                             RegexOptions.IgnoreCase))
                {
                    inSkillsSection = false;
                    continue;
                }

                if (inSkillsSection && !string.IsNullOrWhiteSpace(content))
                {
                    // Clean up bullet points and other markers
                    content = Regex.Replace(content, @"^[•o\-\*]\s*", "").Trim();

                    // If the content has commas, it might be a comma-separated list of skills
                    if (content.Contains(','))
                    {
                        var skills = content.Split(',')
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s));
                        skillsList.AddRange(skills);
                    }
                    else
                    {
                        // Otherwise add the whole line as a skill
                        if (!string.IsNullOrEmpty(content))
                        {
                            skillsList.Add(content);
                        }
                    }
                }
            }

            return string.Join(" | ", skillsList);
        }
    }
}