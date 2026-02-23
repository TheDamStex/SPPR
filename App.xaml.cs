using System.Windows;
using SPPR.Services;
using SPPR.ViewModels;

namespace SPPR;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var decisionService = new DecisionService();
        var trainingService = new TrainingService();
        var storageService = new KnowledgeStorageService();

        var mainViewModel = new MainViewModel(decisionService, trainingService, storageService);

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        mainWindow.Show();
    }
}
