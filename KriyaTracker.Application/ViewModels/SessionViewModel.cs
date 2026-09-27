using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KriyaTracker.Domain.Entities;
using KriyaTracker.Domain.Interfaces;

namespace KriyaTracker.Application.ViewModels
{
    public partial class SessionViewModel : ObservableObject
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IWatchSyncService _watchSyncService;
        
        [ObservableProperty]
        private bool _isSessionActive;
        
        [ObservableProperty]
        private int _currentKriyaCount;
        
        [ObservableProperty]
        private string _currentPhase = "Ready";

        private DateTime _sessionStartTime;

        public SessionViewModel(ISessionRepository sessionRepository, IWatchSyncService watchSyncService)
        {
            _sessionRepository = sessionRepository;
            _watchSyncService = watchSyncService;
        }

        [RelayCommand]
        private async Task StartSessionAsync()
        {
            IsSessionActive = true;
            CurrentKriyaCount = 0;
            _sessionStartTime = DateTime.UtcNow;
            
            // Simulating a live session looping through phases
            // In a real app this would run on a timer or be driven by the watch
            CurrentPhase = "Inhale...";
            await Task.Delay(2000);
            CurrentPhase = "Hold...";
            await Task.Delay(2000);
            CurrentPhase = "Exhale...";
            await Task.Delay(2000);
            
            CurrentKriyaCount++;
            CurrentPhase = "Session Complete";
            IsSessionActive = false;
        }

        [RelayCommand]
        private async Task EndSessionAsync()
        {
            if (!IsSessionActive && CurrentKriyaCount == 0) return;
            
            IsSessionActive = false;
            var session = new MeditationSession
            {
                Id = Guid.NewGuid(),
                StartTime = _sessionStartTime,
                EndTime = DateTime.UtcNow,
                TotalKriyas = CurrentKriyaCount,
                TotalDurationMinutes = (DateTime.UtcNow - _sessionStartTime).TotalMinutes,
                Notes = "Manual phone session"
            };

            await _sessionRepository.AddAsync(session);
            await _watchSyncService.SendSessionAsync(session);
            
            CurrentPhase = "Saved!";
        }
    }
}
