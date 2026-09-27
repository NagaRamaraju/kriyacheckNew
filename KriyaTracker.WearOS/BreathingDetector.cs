using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Hardware;

namespace KriyaTracker.WearOS
{
    public class BreathingDetector : Java.Lang.Object, ISensorEventListener
    {
        public enum BreathPhase
        {
            IDLE, INHALING, HOLDING_TOP, EXHALING, HOLDING_BOTTOM
        }

        public enum HapticType
        {
            START_SESSION, GOAL_REACHED
        }

        private SensorManager _sensorManager;
        private Context _context;
        private Sensor? _accelerometer;
        
        private bool _isDetecting = false;
        private bool _isPaused = true;
        private int _breathCount = 0;

        private BreathPhase _currentPhase = BreathPhase.IDLE;
        private long _phaseStartTime = 0L;

        public long LastInhaleMs { get; private set; } = 0L;
        public long LastHoldMs { get; private set; } = 0L;
        public long LastExhaleMs { get; private set; } = 0L;

        private long _currentInhaleMs = 0L;
        private long _currentHoldMs = 0L;
        private long _currentExhaleMs = 0L;

        private List<long> _inhaleHistory = new List<long>();
        private List<long> _exhaleHistory = new List<long>();
        private int _maxHistorySize = 36;

        private List<float> _accelerationBuffer = new List<float>();
        private float _filteredValue = -1f;

        public string SyncStatus { get; private set; } = "Ready";
        public bool ValidationError { get; private set; } = false;
        public bool LastBreathSuccessful { get; private set; } = false;

        public Action? OnSessionUpdate { get; set; }
        public Action<HapticType>? OnHapticFeedback { get; set; }

        private readonly float _smoothingAlpha = 0.12f;
        private readonly float _movementThreshold = 0.006f;
        private readonly float _stillnessThreshold = 0.004f;
        private readonly long _minActivePhaseMs = 600L;
        private readonly int _trendWindowSize = 6;

        public BreathingDetector(SensorManager sensorManager, Context context)
        {
            _sensorManager = sensorManager;
            _context = context;
        }

        public void SetMaxHistorySize(int size)
        {
            _maxHistorySize = size;
            ResetCounters();
        }

        public int GetMaxHistorySize() => _maxHistorySize;

        public void StartDetection()
        {
            if (_isDetecting) return;
            _accelerometer = _sensorManager.GetDefaultSensor(SensorType.Accelerometer);
            if (_accelerometer != null)
            {
                _sensorManager.RegisterListener(this, _accelerometer, SensorDelay.Ui);
                _isDetecting = true;
            }
        }

        public void StopDetection()
        {
            _sensorManager.UnregisterListener(this);
            _isDetecting = false;
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;
            if (!_isPaused)
            {
                SyncStatus = "Start Inhaling";
                OnHapticFeedback?.Invoke(HapticType.START_SESSION);
                ResetPhase(BreathPhase.IDLE, CurrentTimeMillis());
            }
            else
            {
                SyncStatus = "Paused";
            }
            OnSessionUpdate?.Invoke();
        }

        public bool IsPaused => _isPaused;
        public BreathPhase GetCurrentPhase() => _currentPhase;

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy)
        {
        }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Sensor?.Type == SensorType.Accelerometer && !_isPaused)
            {
                ProcessAccelerometerData(e.Values?.ToArray() ?? Array.Empty<float>());
            }
        }

        private void ProcessAccelerometerData(float[] values)
        {
            if (values.Length < 3) return;

            float magnitude = (float)Math.Sqrt(values[0] * values[0] + values[1] * values[1] + values[2] * values[2]);

            if (_filteredValue < 0) _filteredValue = magnitude;
            _filteredValue = _filteredValue + _smoothingAlpha * (magnitude - _filteredValue);

            _accelerationBuffer.Add(_filteredValue);
            if (_accelerationBuffer.Count > 100) _accelerationBuffer.RemoveAt(0);

            if (_accelerationBuffer.Count > _trendWindowSize)
            {
                DetectPhases(CurrentTimeMillis());
            }
        }

        private void DetectPhases(long now)
        {
            float current = _accelerationBuffer.Last();
            float prevTrend = _accelerationBuffer[_accelerationBuffer.Count - _trendWindowSize - 1];
            float diff = current - prevTrend;

            long phaseDuration = now - _phaseStartTime;

            switch (_currentPhase)
            {
                case BreathPhase.IDLE:
                    if (diff > _movementThreshold)
                    {
                        ResetPhase(BreathPhase.INHALING, now);
                    }
                    break;
                case BreathPhase.INHALING:
                    if (Math.Abs(diff) < _stillnessThreshold && phaseDuration > _minActivePhaseMs)
                    {
                        _currentInhaleMs = phaseDuration;
                        ResetPhase(BreathPhase.HOLDING_TOP, now);
                    }
                    break;
                case BreathPhase.HOLDING_TOP:
                    if (diff < -_movementThreshold)
                    {
                        _currentHoldMs = phaseDuration;
                        ResetPhase(BreathPhase.EXHALING, now);
                    }
                    break;
                case BreathPhase.EXHALING:
                    if ((Math.Abs(diff) < _stillnessThreshold || diff > _movementThreshold) && phaseDuration > _minActivePhaseMs)
                    {
                        _currentExhaleMs = phaseDuration;
                        ValidateBreath();
                        ResetPhase(BreathPhase.HOLDING_BOTTOM, now);
                    }
                    break;
                case BreathPhase.HOLDING_BOTTOM:
                    if (diff > _movementThreshold)
                    {
                        ValidationError = false;
                        LastBreathSuccessful = false;
                        ResetPhase(BreathPhase.INHALING, now);
                    }
                    break;
            }
        }

        private void ResetPhase(BreathPhase newPhase, long now)
        {
            _currentPhase = newPhase;
            _phaseStartTime = now;
            SyncStatus = newPhase switch
            {
                BreathPhase.INHALING => "Inhaling...",
                BreathPhase.HOLDING_TOP => "Holding...",
                BreathPhase.EXHALING => "Exhaling...",
                BreathPhase.HOLDING_BOTTOM => "Wait...",
                _ => SyncStatus
            };
            OnSessionUpdate?.Invoke();
        }

        private void ValidateBreath()
        {
            LastInhaleMs = _currentInhaleMs;
            LastHoldMs = _currentHoldMs;
            LastExhaleMs = _currentExhaleMs;

            bool isHoldValid = LastHoldMs >= 3000;
            bool isEqual = Math.Abs(LastInhaleMs - LastExhaleMs) <= 750;

            if (isHoldValid && isEqual)
            {
                _breathCount++;
                LastBreathSuccessful = true;
                ValidationError = false;
                SyncStatus = $"Perfect! Breath #{_breathCount}";

                _inhaleHistory.Add(LastInhaleMs);
                _exhaleHistory.Add(LastExhaleMs);

                if (_breathCount >= _maxHistorySize)
                {
                    SyncStatus = "Goal Reached!";
                    OnHapticFeedback?.Invoke(HapticType.GOAL_REACHED);
                }
            }
            else
            {
                LastBreathSuccessful = false;
                ValidationError = true;
                if (!isHoldValid)
                    SyncStatus = $"Hold too short! ({LastHoldMs / 1000f:F1}s)";
                else
                    SyncStatus = $"Uneven! I:{LastInhaleMs / 1000f:F1}s E:{LastExhaleMs / 1000f:F1}s";
            }
            OnSessionUpdate?.Invoke();
        }

        public void ResetCounters()
        {
            _breathCount = 0;
            _currentPhase = BreathPhase.IDLE;
            _inhaleHistory.Clear();
            _exhaleHistory.Clear();
            _accelerationBuffer.Clear();
            _filteredValue = -1f;
            LastInhaleMs = 0; LastHoldMs = 0; LastExhaleMs = 0;
            _currentInhaleMs = 0; _currentHoldMs = 0; _currentExhaleMs = 0;
            SyncStatus = "Ready";
            ValidationError = false;
            LastBreathSuccessful = false;
            OnSessionUpdate?.Invoke();
        }

        public int GetBreathCount() => _breathCount;
        public long GetAverageInhaleMs() => _inhaleHistory.Count == 0 ? 0 : (long)_inhaleHistory.Average();
        public long GetAverageExhaleMs() => _exhaleHistory.Count == 0 ? 0 : (long)_exhaleHistory.Average();

        public List<long> GetInhaleHistory() => _inhaleHistory.ToList();
        public List<long> GetExhaleHistory() => _exhaleHistory.ToList();

        public long GetLiveInhaleMs(long now) => _currentPhase == BreathPhase.INHALING ? now - _phaseStartTime : (_currentPhase > BreathPhase.INHALING ? _currentInhaleMs : 0);
        public long GetLiveHoldMs(long now) => _currentPhase == BreathPhase.HOLDING_TOP ? now - _phaseStartTime : (_currentPhase > BreathPhase.HOLDING_TOP ? _currentHoldMs : 0);
        public long GetLiveExhaleMs(long now) => _currentPhase == BreathPhase.EXHALING ? now - _phaseStartTime : (_currentPhase > BreathPhase.EXHALING ? _currentExhaleMs : 0);

        private long CurrentTimeMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
