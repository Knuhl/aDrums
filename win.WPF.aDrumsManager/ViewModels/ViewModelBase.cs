using Prism.Mvvm;
using Prism.Regions;

namespace win.WPF.aDrumsManager.ViewModels
{
    public class ViewModelBase : BindableBase, INavigationAware
    {
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public virtual void RaisePropertiesChanged() => RaisePropertyChanged(null);
    }
}
