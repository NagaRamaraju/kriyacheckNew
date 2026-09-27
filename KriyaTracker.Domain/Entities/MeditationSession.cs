using System;
using System.Collections.Generic;

namespace KriyaTracker.Domain.Entities
{
    public class MeditationSession
    {
        public Guid Id { get; set; }
        
        public DateTime StartTime { get; set; }
        
        public DateTime EndTime { get; set; }
        
        public int TotalKriyas { get; set; }
        
        public double TotalDurationMinutes { get; set; }
        
        public double TotalRetentionSeconds { get; set; }
        
        public double TotalBreathlessSeconds { get; set; }
        
        public double AverageKriyaDurationSeconds { get; set; }
        
        public double LongestRetentionSeconds { get; set; }
        
        public string Notes { get; set; }
    }
}
