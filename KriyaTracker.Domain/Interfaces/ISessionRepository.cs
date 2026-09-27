using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KriyaTracker.Domain.Entities;

namespace KriyaTracker.Domain.Interfaces
{
    public interface ISessionRepository
    {
        Task<MeditationSession> GetByIdAsync(Guid id);
        Task<IEnumerable<MeditationSession>> GetAllAsync();
        Task AddAsync(MeditationSession session);
        Task UpdateAsync(MeditationSession session);
        Task DeleteAsync(Guid id);
    }
}
