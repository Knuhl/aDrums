using MahApps.Metro.Controls.Dialogs;

namespace win.WPF.aDrumsManager.ViewModels
{
    public class DialogViewModelBase : ViewModelBase
    {
        protected readonly IDialogCoordinator DialogCoordinator;

        public DialogViewModelBase(IDialogCoordinator dialogCoordinator)
        {
            DialogCoordinator = dialogCoordinator;
        }

        protected virtual void ShowMessage(string message, string header)
        {
            DialogCoordinator.ShowMessageAsync(this, header, message);
        }
    }
}
