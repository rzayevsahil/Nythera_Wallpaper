using System;
using Nythera.Core.Audio.Models;

namespace Nythera.Core.Audio;

public class AudioReactiveController
{
    private readonly AudioAnalyzer _analyzer;
    public event EventHandler<float>? NeonFlashTriggered;

    public AudioReactiveController(AudioAnalyzer analyzer)
    {
        _analyzer = analyzer;
        _analyzer.AnalyzedDataAvailable += OnAudioDataAvailable;
    }

    public void Start()
    {
        _analyzer.Start();
    }

    public void Stop()
    {
        _analyzer.Stop();
    }

    private void OnAudioDataAvailable(object? sender, AudioReactiveState state)
    {
        // MVP: Send data to Wallpaper Engine / Renderer
        // Example logic: if Bass > threshold, increase light intensity
        if (state.Bass > 0.2f) // Lowered threshold for MVP testing
        {
            NeonFlashTriggered?.Invoke(this, state.Bass);
        }
    }
}
