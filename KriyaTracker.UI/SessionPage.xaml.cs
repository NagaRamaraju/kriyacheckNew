using KriyaTracker.Application.ViewModels;

namespace KriyaTracker.UI;

public partial class SessionPage : ContentPage
{
    public SessionPage(SessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
