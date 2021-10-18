﻿using System;
using System.Threading.Tasks;

using JankWorks.Core;

using JankWorks.Game.Local;
using JankWorks.Game.Diagnostics;
using JankWorks.Game.Configuration;

namespace JankWorks.Game.Hosting
{   
    public abstract class Host : Disposable
    {
        protected struct NewHostSceneRequest
        {
            public int SceneName;
            public HostScene Scene;
            public object InitState;
        }

        public abstract bool IsRemote { get; }

        public abstract bool IsConnected { get; }

        public abstract bool IsHostLoaded { get; }

        public float TicksPerSecond { get; protected set; }

        public TimeSpan Lag { get; protected set; }

        public Settings Settings { get; init; }

        protected Application Application { get; init; }

        protected Host(Application application, Settings settings)
        {
            this.Application = application;
            this.Settings = settings;
        }

        public abstract MetricCounter[] GetMetrics();

        public abstract Task DisposeAsync();
    }

        
}