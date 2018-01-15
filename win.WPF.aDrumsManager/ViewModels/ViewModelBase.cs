using MahApps.Metro.Controls.Dialogs;
using Prism.Mvvm;
using Prism.Regions;

namespace win.WPF.aDrumsManager.ViewModels
{
    public class ViewModelBase : BindableBase, INavigationAware
    {
        protected readonly IDialogCoordinator DialogCoordinator;

        public ViewModelBase(IDialogCoordinator dialogCoordinator)
        {
            DialogCoordinator = dialogCoordinator;
        }

        protected virtual void ShowMessage(string message, string header)
        {
            DialogCoordinator.ShowMessageAsync(this, header, message);
        }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
    }
}
