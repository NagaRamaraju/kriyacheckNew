using KriyaTracker.Application.ViewModels;

namespace KriyaTracker.UI;

public partial class MainPage : ContentPage
{
	public MainPage(DashboardViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
