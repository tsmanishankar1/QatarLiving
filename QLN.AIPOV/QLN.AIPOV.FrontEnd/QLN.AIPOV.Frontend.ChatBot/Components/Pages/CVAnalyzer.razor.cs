using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using QLN.AIPOV.Frontend.ChatBot.Models.FormRecognition;
using QLN.AIPOV.Frontend.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.Frontend.ChatBot.Components.Pages
{
    public partial class CVAnalyzer : IDisposable
    {
        [Inject] public required ISnackbar Snackbar { get; set; }
        [Inject] public required ICvAnalyzerService CvAnalyzerService { get; set; }

        private CVData? _cvData;
        private bool _isLoading;
        private readonly CancellationTokenSource _cts = new();

        private async Task OnFileSelected(IBrowserFile file)
        {
            try
            {
                _isLoading = true;
                _cvData = null;
                StateHasChanged();

                _cvData = await CvAnalyzerService.AnalyzeCvAsync(file, _cts.Token);
                Snackbar.Add("CV analysis completed successfully", Severity.Success);
            }
            catch (OperationCanceledException)
            {
                Snackbar.Add("CV analysis was canceled", Severity.Warning);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error analyzing CV: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
