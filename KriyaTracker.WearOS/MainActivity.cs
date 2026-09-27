using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Graphics;
using Android.Views;
using System.Collections.Generic;
using Android.Hardware;

namespace KriyaTracker.WearOS
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private TextView _phaseText;
        private TextView _kriyasText;
        private TextView _inhaleText;
        private TextView _holdText;
        private TextView _exhaleText;
        private Button _startButton;
        private LinearLayout _historyLayout;
        
        private BreathingDetector _detector;
        private CancellationTokenSource? _uiCts;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            _phaseText = FindViewById<TextView>(Resource.Id.textPhase) ?? throw new InvalidOperationException("Phase text not found");
            _kriyasText = FindViewById<TextView>(Resource.Id.textKriyas) ?? throw new InvalidOperationException("Kriyas text not found");
            _inhaleText = FindViewById<TextView>(Resource.Id.textInhale) ?? throw new InvalidOperationException("Inhale text not found");
            _holdText = FindViewById<TextView>(Resource.Id.textHold) ?? throw new InvalidOperationException("Hold text not found");
            _exhaleText = FindViewById<TextView>(Resource.Id.textExhale) ?? throw new InvalidOperationException("Exhale text not found");
            _startButton = FindViewById<Button>(Resource.Id.buttonStart) ?? throw new InvalidOperationException("Start button not found");
            _historyLayout = FindViewById<LinearLayout>(Resource.Id.historyLayout) ?? throw new InvalidOperationException("History layout not found");

            Window?.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn);

            var sensorManager = (SensorManager)GetSystemService(SensorService)!;
            _detector = new BreathingDetector(sensorManager, this);
            
            var vibrator = (Vibrator)GetSystemService(VibratorService)!;
            _detector.OnHapticFeedback = (type) => {
                if (type == BreathingDetector.HapticType.START_SESSION)
                    vibrator.Vibrate(VibrationEffect.CreateOneShot(150, VibrationEffect.DefaultAmplitude));
                else if (type == BreathingDetector.HapticType.GOAL_REACHED)
                    vibrator.Vibrate(VibrationEffect.CreateWaveform(new long[] { 0, 500, 200, 500, 200, 500 }, -1));
            };

            _detector.OnSessionUpdate = () => {
                RunOnUiThread(() => UpdateHistoryUI());
            };

            _startButton.Click += (sender, e) => {
                _detector.TogglePause();
                if (!_detector.IsPaused)
                {
                    _startButton.Text = "Pause";
                    _startButton.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Color.ParseColor("#B71C1C"));
                }
                else
                {
                    _startButton.Text = "Play";
                    _startButton.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Color.ParseColor("#424242"));
                }
            };
        }

        protected override void OnResume()
        {
            base.OnResume();
            _detector.StartDetection();
            _uiCts = new CancellationTokenSource();
            _ = UpdateLoopAsync(_uiCts.Token);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _detector.StopDetection();
            _uiCts?.Cancel();
        }

        private async Task UpdateLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    
                    RunOnUiThread(() => {
                        var phase = _detector.GetCurrentPhase();
                        var isPaused = _detector.IsPaused;
                        
                        string phaseStr = phase switch {
                            BreathingDetector.BreathPhase.INHALING => "★ INHALING",
                            BreathingDetector.BreathPhase.HOLDING_TOP => "★ HOLDING",
                            BreathingDetector.BreathPhase.EXHALING => "★ EXHALING",
                            _ => isPaused ? "★ PAUSED" : "★ READY"
                        };
                        
                        Color phaseColor = phase switch {
                            BreathingDetector.BreathPhase.INHALING => Color.ParseColor("#42A5F5"), // Blue
                            BreathingDetector.BreathPhase.HOLDING_TOP => Color.ParseColor("#FFA726"), // Orange
                            BreathingDetector.BreathPhase.EXHALING => Color.ParseColor("#66BB6A"), // Green
                            _ => Color.ParseColor("#FFD700") // Yellow
                        };
                        
                        _phaseText.Text = phaseStr;
                        _phaseText.SetTextColor(phaseColor);
                        
                        if (_detector.ValidationError) {
                            _phaseText.Text = "⚠ " + _detector.SyncStatus;
                            _phaseText.SetTextColor(Color.Red);
                        } else if (_detector.LastBreathSuccessful && phase == BreathingDetector.BreathPhase.HOLDING_BOTTOM) {
                            _phaseText.Text = "✓ Perfect!";
                            _phaseText.SetTextColor(Color.Green);
                        }

                        _kriyasText.Text = $"Breaths: {_detector.GetBreathCount()}";

                        long curInhale = _detector.GetLiveInhaleMs(now);
                        long curHold = _detector.GetLiveHoldMs(now);
                        long curExhale = _detector.GetLiveExhaleMs(now);

                        _inhaleText.Text = $"I: {curInhale / 1000f:F1}s ";
                        _holdText.Text = $"H: {curHold / 1000f:F1}s ";
                        _exhaleText.Text = $"E: {curExhale / 1000f:F1}s ";
                        
                        _inhaleText.SetTextColor(phase == BreathingDetector.BreathPhase.INHALING ? phaseColor : Color.White);
                        _holdText.SetTextColor(phase == BreathingDetector.BreathPhase.HOLDING_TOP ? phaseColor : Color.White);
                        _exhaleText.SetTextColor(phase == BreathingDetector.BreathPhase.EXHALING ? phaseColor : Color.White);
                    });

                    await Task.Delay(50, token); // 50ms UI update rate
                }
            }
            catch (TaskCanceledException) { }
        }

        private void UpdateHistoryUI()
        {
            _historyLayout.RemoveAllViews();
            var inHistory = _detector.GetInhaleHistory();
            var exHistory = _detector.GetExhaleHistory();
            
            for (int i = inHistory.Count - 1; i >= 0; i--)
            {
                var row = new LinearLayout(this) { Orientation = Android.Widget.Orientation.Horizontal };
                row.SetGravity(GravityFlags.Center);
                row.SetPadding(0, 4, 0, 4);

                var paramNum = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { Weight = 1 };
                var numView = new TextView(this) { Text = $"#{i + 1}", TextSize = 12 };
                numView.SetTextColor(Color.Gray);
                numView.LayoutParameters = paramNum;

                var paramTime = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { Weight = 1 };
                
                var inView = new TextView(this) { Text = $"I:{inHistory[i] / 1000f:F1}s", TextSize = 12 };
                inView.SetTextColor(Color.ParseColor("#F44336"));
                inView.LayoutParameters = paramTime;

                var outView = new TextView(this) { Text = $"E:{exHistory[i] / 1000f:F1}s", TextSize = 12 };
                outView.SetTextColor(Color.ParseColor("#F44336"));
                outView.LayoutParameters = paramTime;

                row.AddView(numView);
                row.AddView(inView);
                row.AddView(outView);

                _historyLayout.AddView(row);
            }
        }
    }
}