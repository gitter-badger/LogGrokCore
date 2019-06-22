﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LogGrokCore.Data
{
    public class Loader
    {
        private readonly LineIndex _lineIndex;
        private readonly Task _loadingTask;
        private const int BufferSize = 1024*1024;

        public Loader(Func<Stream> streamFactory)
        {
            var encoding = DetectEncoding(streamFactory());
            _lineIndex = new LineIndex();
            var lineProcessor = new LineProcessor(encoding);
            var loaderImpl = new LoaderImpl(BufferSize, _lineIndex, lineProcessor);
            _loadingTask = Task.Factory.StartNew(() => loaderImpl.Load(streamFactory(), encoding.GetBytes("\r"), encoding.GetBytes("\n")));

            LineProvider = new LineProvider(_lineIndex, streamFactory, encoding);
        }

        public LineProvider LineProvider { get; }

        public bool IsLoading => !_loadingTask.IsCompleted;

        private Encoding DetectEncoding(Stream stream)
        {
            using var reader = new StreamReader(stream);
            _ = reader.ReadLine();
            return reader.CurrentEncoding;
        }
    }
}