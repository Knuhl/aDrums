using Microsoft.Practices.Unity;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Prism.Modularity;
using Prism.Regions;
using Prism.Unity;
using win.WPF.aDrumsManager.Resources;
using win.WPF.aDrumsManager.Views;

namespace win.WPF.aDrumsManager
{
    public class ManagerBootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<ManagerShell>();
        }

        protected override void ConfigureServiceLocator()
        {
            base.ConfigureServiceLocator();
            Container.RegisterInstance(typeof(IDialogCoordinator), DialogCoordinator.Instance);
        }

        protected override void ConfigureModuleCatalog()
        {
            base.ConfigureModuleCatalog();
            ((ModuleCatalog) ModuleCatalog).AddModule(typeof(ManagerModule));
        }

        protected override void InitializeShell()
        {
            IRegionManager regionManager = Container.Resolve<IRegionManager>();
            
            regionManager.RegisterViewWithRegion(RegionNames.ComPorts, typeof(ComPortsView));
            regionManager.RegisterViewWithRegion(RegionNames.Content, typeof(DrumManagerView));

            Application.Current.MainWindow.Show();
        }
    }

    public class ManagerModule : IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IUnityContainer _container;

        public ManagerModule(IRegionManager regionManager, IUnityContainer container)
        {
            _regionManager = regionManager;
            _container = container;
        }

        public void Initialize()
        {

        }
    }
}
