﻿using System.IO;

using JankWorks.Core;
using JankWorks.Audio;

namespace JankWorks.Drivers.OpenAL.Audio.Decoders
{
    abstract class Decoder : Disposable
    {
        public abstract long TotalSamples { get; }

        public abstract int SampleRate { get; }

        public abstract int SampleSize { get; }

        public abstract int Channels { get; }

        public abstract bool EndOfStream { get; }

        protected int SampleBufferSize { get; init; }

        public abstract AudioFormat Format { get; }

        public Decoder(int sampleBufferSize)
        {
            this.SampleBufferSize = sampleBufferSize;
        }

        public abstract void Reset();

        public abstract void Load(ALBuffer buffer);

        public abstract bool Decode(ALBuffer buffer);

        public abstract void ChangeStream(Stream stream);
    }
}