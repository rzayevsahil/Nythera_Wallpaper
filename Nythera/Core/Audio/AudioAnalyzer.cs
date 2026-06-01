using System;
using System.Threading;
using Nythera.Core.Audio.Models;

namespace Nythera.Core.Audio;

public class AudioAnalyzer
{
    private readonly IAudioCaptureService _captureService;
    private Timer? _timer;
    public event EventHandler<AudioReactiveState>? AnalyzedDataAvailable;

    public AudioAnalyzer(IAudioCaptureService captureService)
    {
        _captureService = captureService;
    }

    public void Start()
    {
        _captureService.Start();
        // Analyze 30 times a second
        _timer = new Timer(Analyze, null, 0, 33);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _captureService.Stop();
    }

    private void Analyze(object? state)
    {
        var data = new AudioReactiveState
        {
            Bass = _captureService.GetBassLevel(),
            Mid = _captureService.GetMidLevel(),
            Treble = _captureService.GetTrebleLevel()
        };

        AnalyzedDataAvailable?.Invoke(this, data);
    }
}
