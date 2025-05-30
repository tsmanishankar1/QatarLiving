using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
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

        public DocumentIntelligenceClient(IOptions<DocumentIntelligenceSettingsModel> settings)
        {
            _settings = settings.Value;
            _documentAnalysisClient = new DocumentAnalysisClient(
                new Uri(_settings.Endpoint),
                new AzureKeyCredential(_settings.ApiKey));
        }

        public async Task<CVData> ExtractCVDataAsync(Stream documentStream, string contentType, CancellationToken cancellationToken = default)
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

                // First try to extract from Documents if available
                if (result.Documents != null && result.Documents.Count > 0)
                {
                    var document = result.Documents[0];

                    // Try to extract fields from structured data
                    if (document.Fields.TryGetValue("Name", out var nameField) && nameField.Value != null)
                        cvData.Name = nameField.Value.AsString();

                    if (document.Fields.TryGetValue("Surname", out var surnameField) && surnameField.Value != null)
                        cvData.Surname = surnameField.Value.AsString();

                    if (document.Fields.TryGetValue("Email", out var emailField) && emailField.Value != null)
                        cvData.Email = emailField.Value.AsString();

                    if (document.Fields.TryGetValue("ContactNumber", out var contactField) && contactField.Value != null)
                        cvData.ContactNumber = contactField.Value.AsString();
                }
                // Fall back to paragraph parsing if Documents is null or empty
                else if (result.Paragraphs != null && result.Paragraphs.Count > 0)
                {
                    foreach (var paragraph in result.Paragraphs)
                    {
                        if (paragraph.Content == null) continue;

                        var content = paragraph.Content.Trim();

                        // Check if paragraph contains multiple fields
                        if (content.Contains("Firstname:") &&
                           (content.Contains("Last Name:") || content.Contains("Surname:")) &&
                           (content.Contains("Contact Number:") || content.Contains("Email:")))
                        {
                            // Extract all fields from compound paragraph
                            ExtractFieldsFromCompoundParagraph(content, cvData);
                        }
                        else
                        {
                            // Process single-field paragraphs
                            ProcessSingleFieldParagraph(content, cvData);
                        }
                    }
                }

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

        private void ProcessSingleFieldParagraph(string content, CVData cvData)
        {
            // Extract name
            if (content.StartsWith("Firstname:", StringComparison.OrdinalIgnoreCase) ||
                content.StartsWith("First name:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = content.Split(':', 2);
                if (parts.Length > 1)
                    cvData.Name = parts[1].Trim();
            }

            // Extract surname
            else if (content.Contains("Last Name", StringComparison.OrdinalIgnoreCase) ||
                     content.Contains("Surname", StringComparison.OrdinalIgnoreCase))
            {
                var parts = content.Split(':', 2);
                if (parts.Length > 1)
                    cvData.Surname = parts[1].Trim();
            }

            // Extract contact number
            else if (content.StartsWith("Contact Number:", StringComparison.OrdinalIgnoreCase) ||
                     content.StartsWith("Phone:", StringComparison.OrdinalIgnoreCase) ||
                     content.StartsWith("Tel:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = content.Split(':', 2);
                if (parts.Length > 1)
                    cvData.ContactNumber = parts[1].Trim();
            }

            // Extract email
            else if (content.StartsWith("Email:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = content.Split(':', 2);
                if (parts.Length > 1)
                    cvData.Email = parts[1].Trim();
            }
        }

        private void ExtractFieldsFromCompoundParagraph(string content, CVData cvData)
        {
            // Extract firstname using regex
            var firstNameMatch = Regex.Match(content, @"Firstname:\s*([^\s].*?)(?:\s+(?:Last Name|Surname|Contact Number|Email):|\s*$)", RegexOptions.IgnoreCase);
            if (firstNameMatch is { Success: true, Groups.Count: > 1 })
            {
                cvData.Name = firstNameMatch.Groups[1].Value.Trim();

                if (cvData.Name.Contains(" "))
                    cvData.Name = cvData.Name.Split(" ")[0];
            }

            // Extract surname/last name
            var surnameMatch = Regex.Match(content, @"(?:Last Name|Surname):\s*([^\s].*?)(?:\s+(?:Firstname|Contact Number|Email):|\s*$)", RegexOptions.IgnoreCase);
            if (surnameMatch is { Success: true, Groups.Count: > 1 })
            {
                cvData.Surname = surnameMatch.Groups[1].Value.Trim();
                if (cvData.Surname.Contains(" "))
                    cvData.Surname = cvData.Surname.Split(" ")[0];
            }

            // Extract contact number
            var contactMatch = Regex.Match(content, @"Contact Number:\s*([^\s].*?)(?:\s+(?:Firstname|Last Name|Surname|Email):|\s*$)", RegexOptions.IgnoreCase);
            if (contactMatch is { Success: true, Groups.Count: > 1 })
            {
                cvData.ContactNumber = contactMatch.Groups[1].Value.Trim();
            }

            // Extract email
            var emailMatch = Regex.Match(content, @"Email:\s*([^\s].*?)(?:\s+(?:Firstname|Last Name|Surname|Contact Number):|\s*$)", RegexOptions.IgnoreCase);
            if (emailMatch is { Success: true, Groups.Count: > 1 })
            {
                cvData.Email = emailMatch.Groups[1].Value.Trim();
                if (cvData.Email.Contains(" "))
                    cvData.Email = cvData.Email.Split(" ")[0];
            }

            // Fallback to simple email pattern if the above doesn't work
            if (string.IsNullOrEmpty(cvData.Email))
            {
                var simpleEmailMatch = Regex.Match(content, @"[\w\.-]+@[\w\.-]+\.\w+");
                if (simpleEmailMatch.Success)
                {
                    cvData.Email = simpleEmailMatch.Value;
                    if (cvData.Email.Contains(" "))
                        cvData.Email = cvData.Email.Split(" ")[0];
                }
            }

            // Fallback to phone number pattern
            if (!string.IsNullOrEmpty(cvData.ContactNumber)) return;
            var phoneMatch = Regex.Match(content, @"(?:\+\d{1,3}\s?)?(?:\(\d{1,4}\)\s?)?\d{1,4}[-\s]?\d{1,4}[-\s]?\d{1,4}");
            if (phoneMatch.Success)
            {
                cvData.ContactNumber = phoneMatch.Value;
            }
        }
    }
}