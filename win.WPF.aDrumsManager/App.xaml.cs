using System.Windows;

namespace win.WPF.aDrumsManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ManagerBootstrapper bootstrapper = new ManagerBootstrapper();
            bootstrapper.Run();
        }
    }
}
