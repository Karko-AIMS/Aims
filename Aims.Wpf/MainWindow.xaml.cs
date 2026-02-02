using Aims.Wpf.Services;

namespace Aims.Wpf;

public partial class MainWindow
{
    private readonly ApiClient _api = new();

    public MainWindow()
    {
        InitializeComponent();
    }
}
