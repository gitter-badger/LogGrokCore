﻿using LogGrokCore.Data.Virtualization;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogGrokCore.Data
{
    public class LineProvider : IItemProvider<string>
    {
        private readonly Func<Stream> _streamFactory;
        private readonly LineIndex _lineIndex;
        private readonly Encoding _encoding;

        public LineProvider(LineIndex lineIndex, Func<Stream> streamFactory, Encoding encoding)
        {
            _streamFactory = streamFactory;
            _lineIndex = lineIndex;
            _encoding = encoding;
        }

        public int Count => _lineIndex.Count;

        public IList<string> Fetch(int start, int count)
        {
            var (startOffset, _) = _lineIndex.GetLine(start);
            var (lastLineOffset, lastLineLength) = _lineIndex.GetLine(start + count - 1);

            var size = (int)(lastLineOffset + lastLineLength - startOffset);

            using var owner = MemoryPool<byte>.Shared.Rent(size);
            using var stream = _streamFactory();

            var span = owner.Memory.Span.Slice(0, size);

            stream.Read(span);
            var result = new List<string>(count);
            for (var i = start; i < start + count; i++)
            {
                var (offset, len) = _lineIndex.GetLine(start);
                var lineSpan = span.Slice((int)(offset - startOffset), len);
                result.Add(_encoding.GetString(lineSpan));
            }
            return result;
        }
    }
}