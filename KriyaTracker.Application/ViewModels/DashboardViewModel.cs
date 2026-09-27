using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KriyaTracker.Domain.Entities;
using KriyaTracker.Domain.Interfaces;

namespace KriyaTracker.Application.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ISessionRepository _sessionRepository;

        [ObservableProperty]
        private int _todayTotalKriyas;

        [ObservableProperty]
        private double _todayPracticeTime;

        public ObservableCollection<MeditationSession> RecentSessions { get; } = new();

        public DashboardViewModel(ISessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
            _ = LoadDashboardDataAsync();
        }

        [RelayCommand]
        private async Task LoadDashboardDataAsync()
        {
            var sessions = await _sessionRepository.GetAllAsync();
            RecentSessions.Clear();
            
            int totalKriyas = 0;
            double totalTime = 0;

            foreach (var session in sessions)
            {
                RecentSessions.Add(session);
                totalKriyas += session.TotalKriyas;
                totalTime += session.TotalDurationMinutes;
            }

            TodayTotalKriyas = totalKriyas;
            TodayPracticeTime = totalTime;
        }
    }
}
