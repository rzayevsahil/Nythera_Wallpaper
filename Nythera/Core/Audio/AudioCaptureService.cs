using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Nythera.Core.Audio;

public interface IAudioCaptureService
{
    float GetBassLevel();
    float GetMidLevel();
    float GetTrebleLevel();
    void Start();
    void Stop();
}

public class AudioCaptureService : IAudioCaptureService
{
    private WasapiLoopbackCapture? _capture;
    private float _currentBass;
    private float _currentMid;
    private float _currentTreble;

    public void Start()
    {
        try
        {
            _capture = new WasapiLoopbackCapture();
            _capture.DataAvailable += OnDataAvailable;
            _capture.StartRecording();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioCaptureService Error: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_capture != null)
        {
            _capture.StopRecording();
            _capture.DataAvailable -= OnDataAvailable;
            _capture.Dispose();
            _capture = null;
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        // V1 MVP: Simple amplitude analysis
        // Real implementation would use FFT (Fast Fourier Transform) to isolate Bass/Mid/Treble
        float max = 0;
        for (int index = 0; index < e.BytesRecorded; index += 2)
        {
            short sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index]);
            float sample32 = sample / 32768f;
            if (sample32 < 0) sample32 = -sample32;
            if (sample32 > max) max = sample32;
        }

        // Mocking frequencies for MVP based on overall amplitude with EMA smoothing
        float targetBass = max * 1.5f;
        float targetMid = max * 1.0f;
        float targetTreble = max * 0.8f;

        // Apply EMA filter: slow attack to filter out transient pops/clicks, faster decay
        if (targetBass > _currentBass)
            _currentBass = _currentBass * 0.90f + targetBass * 0.10f;
        else
            _currentBass = _currentBass * 0.70f + targetBass * 0.30f;

        if (targetMid > _currentMid)
            _currentMid = _currentMid * 0.90f + targetMid * 0.10f;
        else
            _currentMid = _currentMid * 0.70f + targetMid * 0.30f;

        if (targetTreble > _currentTreble)
            _currentTreble = _currentTreble * 0.90f + targetTreble * 0.10f;
        else
            _currentTreble = _currentTreble * 0.70f + targetTreble * 0.30f;
    }

    public float GetBassLevel() => _currentBass;
    public float GetMidLevel() => _currentMid;
    public float GetTrebleLevel() => _currentTreble;
}
