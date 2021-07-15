﻿using System;
using System.Diagnostics;

using Thread = System.Threading.Thread;
using ThreadPool = System.Threading.ThreadPool;

using JankWorks.Core;
using JankWorks.Audio;
using JankWorks.Graphics;
using JankWorks.Interface;

using JankWorks.Game.Configuration;
using JankWorks.Game.Assets;
using JankWorks.Game.Hosting;


using JankWorks.Game.Platform;

namespace JankWorks.Game.Local
{
    public sealed class Client : Disposable
    {
        private struct NewSceneRequest
        {
            public string? SceneName;
            public Host? Host;
            public object? InitState;
        }

        public Settings Settings { get; private set; }

        public TimeSpan Lag { get; private set; }

        public float UpdatesPerSecond { get; private set; }

        public float FramesPerSecond { get; private set; }

        private Application application;

        private Host host;

        private AssetManager assetManager;

        private Window window;

        private GraphicsDevice graphicsDevice;

        private AudioDevice audioDevice;

        private LoadingScreen? loadingScreen;

        private Scene scene;

        private volatile ClientState state;

        private ClientConfgiuration config;
        private ClientParameters parameters;

        private NewSceneRequest newSceneRequest;

        private Counter upsCounter;
        private Counter fpsCounter;

#pragma warning disable CS8618
        public Client(Application application, ClientConfgiuration config, Host host)
        {
            var second = TimeSpan.FromSeconds(1);
            this.upsCounter = new Counter(second);
            this.fpsCounter = new Counter(second);

            this.state = ClientState.Constructed;
            this.application = application;
            this.host = host;
            this.assetManager = application.RegisterAssetManager();

            var settings = application.GetClientSettings();
            config.Load(settings);
            this.config = config;
            this.Settings = settings;

            var parms = application.ClientParameters;
            this.parameters = parms;


            var winds = new WindowSettings()
            {
                Title = application.Name,
                Monitor = config.Monitor,
                ShowCursor = parms.ShowCursor,
                Style = config.WindowStyle,
                VideoMode = config.VideoMode,
                VSync = config.Vsync
            };

            var surfs = new SurfaceSettings()
            {
                ClearColour = parms.ClearColour,
                Size = config.VideoMode.Viewport.Size
            };

            this.window = Window.Create(winds);
            this.graphicsDevice = GraphicsDevice.Create(surfs, this.window);
            this.audioDevice = AudioDevice.Create();
        }
#pragma warning restore CS8618

        private void LoadScene()
        {
            var changeState = this.newSceneRequest;
            string scene = changeState.SceneName ?? throw new ApplicationException();
            Host host = this.newSceneRequest.Host ?? throw new ApplicationException();
            object? initstate = this.newSceneRequest.InitState;


            if (!object.ReferenceEquals(this.host, host))
            {
                this.host.Dispose();
                this.host = host;
            }


            if(this.host is RemoteHost remoteHost)
            {
                this.LoadSceneWithRemoteHost(scene, remoteHost, initstate);
            }
            else if(this.host is LocalHost localHost)
            {
                this.LoadSceneWithLocalHost(scene, localHost, initstate);
            }
            else
            {
                throw new NotImplementedException();
            }
            this.state = ClientState.EndLoadingScene;
        }

        private void LoadSceneWithRemoteHost(string scene, RemoteHost host, object? initState)
        {
            if (this.scene != null)
            {
                this.scene.PreDispose();
                this.scene.UnsubscribeInputs(this.window);
                this.scene.DisposeSoundResources(this.audioDevice);
                this.scene.DisposeGraphicsResources(this.graphicsDevice);
                this.scene.ClientDispose(this);
                this.scene.Dispose(this.application);
            }

            if(!host.IsConnected)
            {
                host.Connect();
            }

            host.LoadScene(scene, initState);

            this.scene = this.application.Scenes[scene]();

            this.scene.PreInitialise(initState);
            this.scene.Initialise(this.application, this.assetManager);
            this.scene.ClientInitialise(this, this.assetManager);
            this.scene.InitialiseGraphicsResources(this.graphicsDevice, this.assetManager);
            this.scene.InitialiseSoundResources(this.audioDevice, this.assetManager);
            this.scene.ClientInitialised(initState);

            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }

        private void LoadSceneWithLocalHost(string scene, LocalHost host, object? initState)
        {
            if (this.scene != null)
            {
                this.scene.UnsubscribeInputs(this.window);
                this.scene.DisposeSoundResources(this.audioDevice);
                this.scene.DisposeGraphicsResources(this.graphicsDevice);
                this.scene.ClientDispose(this);
            }

            this.scene = this.application.Scenes[scene]();

            if (!host.IsConnected)
            {
                host.Connect();
            }

            host.LoadScene(this.scene, initState);

            this.scene.ClientInitialise(this, this.assetManager);
            this.scene.InitialiseGraphicsResources(this.graphicsDevice, this.assetManager);
            this.scene.InitialiseSoundResources(this.audioDevice, this.assetManager);
            this.scene.ClientInitialised(initState);            
        }

        public void ChangeScene(string scene, object? initsate = null) => this.ChangeScene(scene, this.host, initsate);     
        public void ChangeScene(string scene, Host host, object? initsate = null)
        {
            if (object.ReferenceEquals(this.host, host) && host.IsRemote && host.IsConnected)
            {
                throw new ArgumentException();
            }

            this.newSceneRequest = new NewSceneRequest()
            {
                Host = host,
                InitState = initsate,
                SceneName = scene
            };
            this.state = ClientState.BeginLoadingScene;
        }

        public void Run(string scene, object? initState = null) => this.Run(scene, this.host, initState);
        public void Run(string scene, Host host, object? initState = null)
        {
            this.graphicsDevice.Activate();

            var ls = this.application.RegisterLoadingScreen();

            if (ls != null)
            {
                ls.InitialiseResources(this.assetManager);
                ls.InitialiseGraphicsResources(this.graphicsDevice, this.assetManager);
                this.loadingScreen = ls;
            }
            this.window.Show();
            this.ChangeScene(scene, host, initState);
            this.Run();
        }
        private void Run()
        {
            var updateTime = TimeSpan.FromMilliseconds((1f / this.parameters.UpdateRate) * 1000);
            var frameTime = TimeSpan.FromMilliseconds((1f / this.config.FrameRate) * 1000);

            var timer = new Stopwatch();
            timer.Start();

            var accumulator = TimeSpan.Zero;
            var lag = TimeSpan.Zero;
            var lastrun = timer.Elapsed;

            this.upsCounter.Start();
            this.fpsCounter.Start();

            while (this.window.IsOpen)
            {
                var state = this.CheckForStateChange();

                TimeSpan now = timer.Elapsed;
                TimeSpan since = now - lastrun;
                accumulator += since;
                lag += since;
                this.Lag = lag;

                if (accumulator >= frameTime)
                {
                    while (lag >= updateTime)
                    {
                        var delta = (lag > updateTime) ? updateTime : lag;

                        this.Update(state, delta);

                        lag -= updateTime;
                    }

                    var frame = new Frame(accumulator.TotalMilliseconds / updateTime.TotalMilliseconds);
                    this.Render(state, frame, updateTime);

                    accumulator -= frameTime;
                }
                else
                {
                    var remaining = frameTime - accumulator;

                    if (remaining > TimeSpan.Zero)
                    {
                        PlatformApi.Instance.Sleep(remaining);
                    }
                }

                lastrun = now;
            }
        }

        private void Update(ClientState state, TimeSpan delta)
        {
            this.UpdatesPerSecond = this.upsCounter.Frequency;
            this.upsCounter.Count();

            this.window.ProcessEvents();
            if (state > ClientState.BeginLoadingScene)
            {
                this.loadingScreen?.Update(delta);
            }
            else
            {
                this.scene.Update(delta);
            }
        }

        private void Render(ClientState state, Frame frame, TimeSpan timeout)
        {
            this.UpdatesPerSecond = this.upsCounter.Frequency;            

            if (state > ClientState.BeginLoadingScene)
            {
                if (this.loadingScreen != null && this.graphicsDevice.Activate(timeout))
                {
                    try
                    {
                        this.loadingScreen.Render(this.graphicsDevice, frame);
                        this.graphicsDevice.Display();
                        this.upsCounter.Count();
                    }
                    finally
                    {
                        this.graphicsDevice.Deactivate();
                    }
                }
            }
            else
            {
                this.scene.Render(this.graphicsDevice, frame);
                this.graphicsDevice.Display();
                this.upsCounter.Count();
            }
        }

        private ClientState CheckForStateChange()
        {
            var state = this.state;

            switch (state)
            {
                case ClientState.WaitingOnHost:
                    
                    if(this.host.IsHostLoaded)
                    {
                        this.graphicsDevice.Activate();
                        state = ClientState.RunningScene;
                        this.state = state;

                        if(this.host.IsRemote)
                        {
                            this.scene.Initialised();
                        }
                        
                        this.scene.SubscribeInputs(this.window);
                    }
                   
                    break;

                case ClientState.EndLoadingScene:

                    state = ClientState.WaitingOnHost;
                    this.state = state;
                    this.host.NotifyClientLoaded();
                    this.newSceneRequest = default;
                    break;

                case ClientState.BeginLoadingScene:

                    this.graphicsDevice.Deactivate();
                    state = ClientState.LoadingScene;
                    this.state = state;

#pragma warning disable CS8602
#pragma warning disable CS8600
                    ThreadPool.QueueUserWorkItem((client) => ((Client)client).LoadScene(), this);                    
#pragma warning restore CS8600
#pragma warning restore CS8602

                    break;
            }

            return state;
        }

        protected override void Dispose(bool finalising)
        {
            var ls = this.loadingScreen;

            if (ls != null)
            {
                ls.DisposeGraphicsResources(this.graphicsDevice);
                ls.DisposeResources();
            }

            this.graphicsDevice.Dispose();
            this.window.Dispose();

            base.Dispose(finalising);
        }
    }

    // Order sensitive
    public enum ClientState : byte
    {
        Constructed = 0,

        RunningScene,

        BeginLoadingScene,

        LoadingScene,

        EndLoadingScene,

        WaitingOnHost
    }
}
