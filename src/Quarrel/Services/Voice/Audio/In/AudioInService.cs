﻿using GalaSoft.MvvmLight.Ioc;
using Quarrel.ViewModels.Services.Voice.Audio.In;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Render;

namespace Quarrel.Services.Voice.Audio.In
{

    public class AudioInService : IAudioInService
    {
        #region Public Properties

        // TODO: Public set
        public string DeviceId { get; private set; }
        public bool Muted { get; private set; }

        #endregion

        #region Variables

        private AudioGraph _Graph;
        private AudioFrameOutputNode _FrameOutputNode;
        private int _Quantum;
        private bool _IsSpeaking = false;

        #endregion

        #region Events

        public event EventHandler<float[]> DataRecieved;

        public event EventHandler<int> SpeakingChanged;

        #endregion

        #region Constructors

        [PreferredConstructor]
        public AudioInService()
        {

        }

        public AudioInService(string deviceId)
        {

        }

        #endregion

        #region Methods

        public async void CreateGraph(string deviceId = null)
        {
            // Get Default Settings
            var graphResult = await AudioGraph.CreateAsync(GetDefaultGraphSettings());
            if (graphResult.Status != AudioGraphCreationStatus.Success)
            {
                // Cannot create graph
                return;
            }
            _Graph = graphResult.Graph;

            // Create frameOutputNode
            _FrameOutputNode = _Graph.CreateFrameOutputNode(_Graph.EncodingProperties);

            _Quantum = 0;
            _Graph.QuantumStarted += _Graph_QuantumStarted;

            // Get Device
            if (string.IsNullOrEmpty(deviceId) || deviceId == "Default")
            {
                deviceId = MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default);
            }
            DeviceInformation selectedDevice = await DeviceInformation.CreateFromIdAsync(deviceId);

            CreateAudioDeviceInputNodeResult nodeResult = await _Graph.CreateDeviceInputNodeAsync(MediaCategory.Communications, GetDefaultNodeSettings().EncodingProperties, selectedDevice);
            if (nodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // TODO: Handle no mic permission

                // Cannot create device input node
                _Graph.Dispose();
                return;
            }

            // Connect Nodes
            nodeResult.DeviceInputNode.AddOutgoingConnection(_FrameOutputNode);

            // Finalize
            DeviceId = deviceId;

            // Begin play
            _FrameOutputNode.Start();
            _Graph.Start();
        }

        public void Mute()
        {
            Muted = true;
        }

        public void Unmute()
        {
            Muted = false;
        }

        public void ToggleMute()
        {
            Muted = !Muted;
        }

        public void Dispose()
        {
            _Graph.Dispose();
        }
        #endregion

        #region Helper Methods

        private void _Graph_QuantumStarted(AudioGraph sender, object args)
        {
            // If odd quantum
            if (++_Quantum % 2 == 0)
            {
                try
                {
                    // Record frame
                    AudioFrame frame = _FrameOutputNode.GetFrame();
                    ProcessFrameOutput(frame);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        private unsafe void ProcessFrameOutput(AudioFrame frame)
        {
            if (Muted)
                return;

            #region GetPCM
            float[] dataInFloats;
            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacityInBytes);

                float* dataInFloat = (float*)dataInBytes;
                dataInFloats = new float[capacityInBytes / sizeof(float)];

                for (int i = 0; i < capacityInBytes / sizeof(float); i++)
                {
                    dataInFloats[i] = dataInFloat[i];
                }
            }

            #endregion

            #region Parse PCM

            double decibels = 0f;
            foreach (var sample in dataInFloats)
            {
                decibels += Math.Abs(sample);
            }
            decibels = 20 * Math.Log10(decibels / dataInFloats.Length);
            if (decibels < -40)
            {
                if (_IsSpeaking)
                {
                    SpeakingChanged(this, 0);
                    _IsSpeaking = false;
                }
            }
            else
            {
                if (!_IsSpeaking)
                {
                    SpeakingChanged(this, 1);
                    _IsSpeaking = true;
                }

                DataRecieved?.Invoke(null, dataInFloats);
            }

            #endregion
        }

        private AudioGraphSettings GetDefaultGraphSettings()
        {
            AudioGraphSettings graphsettings = new AudioGraphSettings(AudioRenderCategory.Communications);
            graphsettings.EncodingProperties = new AudioEncodingProperties();
            graphsettings.EncodingProperties.Subtype = "Float";
            graphsettings.EncodingProperties.SampleRate = 48000;
            graphsettings.EncodingProperties.ChannelCount = 2;
            graphsettings.EncodingProperties.BitsPerSample = 32;
            graphsettings.EncodingProperties.Bitrate = 3072000;
            return graphsettings;
        }

        private AudioGraphSettings GetDefaultNodeSettings()
        {
            AudioGraphSettings nodesettings = new AudioGraphSettings(AudioRenderCategory.Communications);
            nodesettings.EncodingProperties = AudioEncodingProperties.CreatePcm(48000, 2, 32);
            nodesettings.DesiredSamplesPerQuantum = 960;
            nodesettings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.ClosestToDesired;
            return nodesettings;
        }

        #endregion
    }

    #region Dependencies

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]

    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    #endregion
}
