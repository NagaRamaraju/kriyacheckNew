using System;
using System.Threading.Tasks;
using KriyaTracker.Domain.Entities;

namespace KriyaTracker.Domain.Interfaces
{
    public interface IWatchSyncService
    {
        Task SendSessionAsync(MeditationSession session);
        
        Task ReceiveSessionAsync();
        
        event EventHandler<MeditationSession> OnSessionReceived;
    }
}
