using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;

/// <summary>
/// Core audio passthrough engine that captures audio from a microphone and routes it to outputs.
/// Handles WASAPI initialization, buffering, and audio processing.
/// </summary>
class PassthroughEngine
{
    private readonly ILogger _logger;
    private readonly AudioDeviceManager _deviceManager;
    private readonly int _bufferMs;
    private readonly bool _tryExclusiveMode;
    private readonly int _prebufferFrames;

    // Audio components
    private WasapiCapture _capture;
    private WasapiOut _cableOut;
    private WasapiOut _monitorOut;
    private BufferedWaveProvider _cableBuffer;
    private BufferedWaveProvider _monitorBuffer;

    // Statistics
    private long _totalBytesProcessed;
    private int _frameCount;

    /// <summary>
    /// Creates a new instance of PassthroughEngine.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <param name="deviceManager">Device manager for locating audio devices.</param>
    /// <param name="bufferMs">Buffer size in milliseconds.</param>
    /// <param name="tryExclusiveMode">Whether to attempt exclusive audio mode.</param>
    /// <param name="prebufferFrames">Number of frames to prebuffer before playback.</param>
    public PassthroughEngine(ILogger logger, AudioDeviceManager deviceManager, 
        int bufferMs, bool tryExclusiveMode, int prebufferFrames)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        _bufferMs = bufferMs;
        _tryExclusiveMode = tryExclusiveMode;
        _prebufferFrames = prebufferFrames;
    }

    /// <summary>
    /// Initializes the audio passthrough system with the specified devices.
    /// </summary>
    /// <param name="micName">Name of the microphone device to capture from.</param>
    /// <param name="cableInName">Name of the cable input device to render to.</param>
    /// <param name="monitorName">Name of the monitor/speaker device (optional).</param>
    /// <param name="enableMonitoring">Whether to enable audio monitoring to speakers.</param>
    public void Initialize(string micName, string cableInName, string monitorName, bool enableMonitoring)
    {
        _logger.LogInformation("Starting microphone passthrough application");
        _logger.LogDebug("Configuration: mic='{MicName}', cable='{CableName}', monitor='{MonitorName}', " +
            "enableMonitoring={EnableMonitoring}, buffer={BufferMs}ms, exclusiveMode={ExclusiveMode}, prebuffer={PrebufferFrames}",
            micName, cableInName, monitorName, enableMonitoring, _bufferMs, _tryExclusiveMode, _prebufferFrames);

        // Find and validate devices
        var mic = _deviceManager.FindDevice(DataFlow.Capture, micName);
        var cableIn = _deviceManager.FindDevice(DataFlow.Render, cableInName);
        var monitor = enableMonitoring ? _deviceManager.FindDevice(DataFlow.Render, monitorName) : null;

        // Initialize capture
        InitializeCapture(mic);

        // Initialize outputs
        InitializeCableOutput(cableIn);
        if (enableMonitoring)
        {
            InitializeMonitorOutput(monitor);
        }

        // Set up audio processing
        SetupAudioDataHandler();
    }

    /// <summary>
    /// Starts the audio capture and passthrough.
    /// </summary>
    public void Start()
    {
        _logger.LogDebug("Starting audio capture");
        _capture.StartRecording();
    }

    /// <summary>
    /// Stops the audio capture and passthrough.
    /// </summary>
    public void Stop()
    {
        _logger.LogInformation("Shutdown requested");
        _logger.LogDebug("Stopping capture");
        _capture.StopRecording();
    }

    /// <summary>
    /// Releases all audio resources.
    /// </summary>
    public void Dispose()
    {
        _logger.LogDebug("Disposing resources");
        _cableOut?.Dispose();
        _monitorOut?.Dispose();
        _capture?.Dispose();
        _logger.LogInformation("Shutdown complete. Total: {FrameCount} frames, {BytesProcessed:N0} bytes processed", 
            _frameCount, _totalBytesProcessed);
    }

    /// <summary>
    /// Initializes WASAPI capture from the specified microphone device.
    /// </summary>
    private void InitializeCapture(MMDevice micDevice)
    {
        _logger.LogDebug("Initializing WASAPI capture");
        _capture = new WasapiCapture(micDevice);
        var format = _capture.WaveFormat;
        _logger.LogDebug("Audio format: {SampleRate}Hz, {Channels}ch, {BitsPerSample}bit, {Encoding}",
            format.SampleRate, format.Channels, format.BitsPerSample, format.Encoding);
    }

    /// <summary>
    /// Initializes the cable output with appropriate buffering and audio mode.
    /// </summary>
    private void InitializeCableOutput(MMDevice cableInDevice)
    {
        _logger.LogDebug("Creating buffer ({BufferMs}ms capacity)", _bufferMs);
        _cableBuffer = new BufferedWaveProvider(_capture.WaveFormat)
        {
            DiscardOnBufferOverflow = true,
            BufferDuration = TimeSpan.FromMilliseconds(_bufferMs)
        };

        _cableOut = InitializeAudioOutput(cableInDevice, _cableBuffer, "cable");
    }

    /// <summary>
    /// Initializes the monitor output with appropriate buffering and audio mode.
    /// </summary>
    private void InitializeMonitorOutput(MMDevice monitorDevice)
    {
        _logger.LogDebug("Monitoring enabled - initializing monitor output");
        _monitorBuffer = new BufferedWaveProvider(_capture.WaveFormat)
        {
            DiscardOnBufferOverflow = true,
            BufferDuration = TimeSpan.FromMilliseconds(_bufferMs)
        };

        _monitorOut = InitializeAudioOutput(monitorDevice, _monitorBuffer, "monitor");
    }

    /// <summary>
    /// Initializes a WASAPI output with the specified buffer and audio mode settings.
    /// Attempts exclusive mode if configured, falls back to shared mode on failure.
    /// </summary>
    private WasapiOut InitializeAudioOutput(MMDevice device, BufferedWaveProvider buffer, string deviceType)
    {
        WasapiOut output = null;

        if (_tryExclusiveMode)
        {
            try
            {
                _logger.LogDebug("Attempting exclusive mode for {DeviceType} output (10ms latency)", deviceType);
                output = new WasapiOut(device, AudioClientShareMode.Exclusive, false, 10);
                output.Init(buffer);
                _logger.LogDebug("Exclusive mode ({DeviceType}): SUCCESS", deviceType);
                return output;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Exclusive mode ({DeviceType}) failed: {Message}", deviceType, ex.Message);
                _logger.LogDebug("Falling back to shared mode (0ms latency hint)");
            }
        }

        // Fallback to shared mode
        _logger.LogDebug("Using shared mode (0ms latency hint) for {DeviceType}", deviceType);
        output = new WasapiOut(device, AudioClientShareMode.Shared, false, 0);
        output.Init(buffer);
        _logger.LogDebug("Shared mode ({DeviceType}): SUCCESS", deviceType);
        return output;
    }

    /// <summary>
    /// Sets up the audio data handler that processes incoming audio frames.
    /// Implements prebuffering logic and periodically logs statistics.
    /// </summary>
    private void SetupAudioDataHandler()
    {
        _logger.LogDebug("Setting up audio data handler");
        
        bool playbackStarted = false;
        int prebufferRemaining = _prebufferFrames;

        _capture.DataAvailable += (s, e) =>
        {
            _frameCount++;
            _totalBytesProcessed += e.BytesRecorded;

            // Route audio to outputs
            _cableBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            _monitorBuffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);

            // Implement prebuffering: wait for N frames before starting playback
            if (!playbackStarted)
            {
                int buffered = _prebufferFrames - prebufferRemaining;
                if (--prebufferRemaining <= 0)
                {
                    _logger.LogDebug("Pre-buffering complete ({BufferedFrames} frames buffered)", buffered + 1);
                    _logger.LogDebug("Starting playback");
                    _cableOut.Play();
                    _monitorOut?.Play();
                    playbackStarted = true;
                    _logger.LogInformation("Playback started - audio passthrough active");
                }
                else
                {
                    _logger.LogDebug("Pre-buffering: frame {CurrentFrame}/{TotalFrames}", buffered + 1, _prebufferFrames);
                }
            }

            // Log statistics every 100 frames
            if (_frameCount % 100 == 0)
            {
                var format = _capture.WaveFormat;
                double seconds = _totalBytesProcessed / (double)format.AverageBytesPerSecond;
                _logger.LogDebug("Stats: {FrameCount} frames, {BytesProcessed:N0} bytes, {Seconds:F1}s processed, buffer: {BufferDuration:F1}ms",
                    _frameCount, _totalBytesProcessed, seconds, _cableBuffer.BufferedDuration.TotalMilliseconds);
            }
        };
    }
}
