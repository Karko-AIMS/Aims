using Aims.Wpf.Services;
using System.Net.Http;
using System.Windows;

namespace Aims.Wpf;

public partial class MainWindow
{
    private readonly ApiClient _api = new();

    public MainWindow()
    {
        InitializeComponent();
        AppendLog($"BaseUrl: {_api.Http.BaseAddress}");
    }

    private async void PingButton_Click(object sender, RoutedEventArgs e)
    {
        PingButton.IsEnabled = false;
        ResultText.Text = "Calling...";

        try
        {
            var resp = await _api.Http.GetAsync("api/ping");
            var body = await resp.Content.ReadAsStringAsync();

            ResultText.Text = $"{(int)resp.StatusCode} {resp.StatusCode}";
            AppendLog($"GET /api/ping -> {ResultText.Text}");
            AppendLog(body);
        }
        catch (HttpRequestException ex)
        {
            ResultText.Text = "HTTP error";
            AppendLog("HTTP error: " + ex.Message);
        }
        catch (Exception ex)
        {
            ResultText.Text = "Error";
            AppendLog("Error: " + ex);
        }
        finally
        {
            PingButton.IsEnabled = true;
        }
    }

    private void AppendLog(string message)
    {
        LogText.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogText.ScrollToEnd();
    }
}
