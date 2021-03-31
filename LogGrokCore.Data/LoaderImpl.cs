﻿using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace LogGrokCore.Data
{
    public class LoaderImpl
    {
        private readonly int _bufferSize;
        private readonly ILineDataConsumer _lineDataConsumer;

        public LoaderImpl(int bufferSize, ILineDataConsumer lineDataConsumer)
        {
            _bufferSize = bufferSize;
            _lineDataConsumer = lineDataConsumer;
        }

        public void Load(Stream stream, ReadOnlySpan<byte> cr, ReadOnlySpan<byte> lf, CancellationToken cancellationToken)
        {
            var isInCrLfs = false;
            var crLength = cr.Length;

            var lineStartFromCurrentDataOffset = 0;

            var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
            var bufferSize = _bufferSize;
            var (rPattern, nPattern, minusPattern, andPattern) = GetPatterns(cr, lf);
            
            try
            {
                var haveFirstLine = false;
                var dataOffsetFromBufferStart = 0;
                long streamPosition = 0;
                long bufferStartPosition;
                while (!cancellationToken.IsCancellationRequested)
                {
                    bufferStartPosition = streamPosition - dataOffsetFromBufferStart;
                    var data = buffer.AsSpan(dataOffsetFromBufferStart,
                        bufferSize - dataOffsetFromBufferStart);
                    var bytesRead = stream.Read(data);
                    streamPosition += bytesRead;

                    while (true)
                    {
                        var i = 0;
                        while (i < bytesRead)
                        {
                            if (!isInCrLfs
                                && (dataOffsetFromBufferStart + i) % sizeof(ulong) == 0
                                && i < bufferSize - dataOffsetFromBufferStart - sizeof(ulong))
                            {
                                var longs = MemoryMarshal.Cast<byte, ulong>(data.Slice(i));

                                foreach (var longValue in longs)
                                {
                                    bool CheckValuePresence(ulong sourceData, ulong valuePattern)
                                    {
                                        var masked = sourceData ^ valuePattern;
                                        return ((masked - minusPattern) & ~masked & andPattern) != 0;
                                    }

                                    if (CheckValuePresence(longValue, rPattern) ||
                                        CheckValuePresence(longValue, nPattern))
                                    {
                                        break;
                                    }
                                    
                                    i += sizeof(ulong);
                                }
                            }

                            if (i >= bytesRead) break;
                            
                            var current = data.Slice(i, crLength);
                            if (current.SequenceEqual(cr) || current.SequenceEqual(lf))
                            {
                                isInCrLfs = true;
                            }
                            else if (isInCrLfs)
                            {
                                isInCrLfs = false;

                                var lineStartInBuffer =
                                    dataOffsetFromBufferStart
                                    + lineStartFromCurrentDataOffset;
                             
                                var isLineStart = _lineDataConsumer.AddLineData(
                                    bufferStartPosition + lineStartInBuffer,
                                    buffer.AsSpan().Slice(
                                        lineStartInBuffer, i + dataOffsetFromBufferStart - lineStartInBuffer));
                                lineStartFromCurrentDataOffset = i;
                                haveFirstLine = haveFirstLine || isLineStart;
                            }
                            i += crLength;
                        }
                        
                        var lineOffsetFromBufferStart =
                            dataOffsetFromBufferStart + lineStartFromCurrentDataOffset;

                        if (bytesRead < data.Length)
                        {
                            var bufferEndOffset = bytesRead + dataOffsetFromBufferStart;
                            FinishProcessing(lineOffsetFromBufferStart, bufferEndOffset, stream.Position);
                            return;
                        }

                        if (lineOffsetFromBufferStart > 0)
                        {
                            // found line(s) inside the buffer
                            // copy tail of buffer to new one
                            dataOffsetFromBufferStart = bufferSize - lineOffsetFromBufferStart;
                            lineStartFromCurrentDataOffset = - dataOffsetFromBufferStart;

                            var bufferSpan = buffer.AsSpan();
                            var rest = bufferSpan.Slice(lineOffsetFromBufferStart);

                            if (bufferSize <= _bufferSize || rest.Length >= _bufferSize)
                            {
                                rest.CopyTo(bufferSpan);
                            }
                            else
                            {
                                var oldBuffer = buffer;
                                buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
                                bufferSize = _bufferSize;
                                bufferSpan = buffer.AsSpan();
                                rest.CopyTo(bufferSpan);
                                ArrayPool<byte>.Shared.Return(oldBuffer);
                            }
                            break;
                        }

                        // did not found next line start, grow buffer
                        var newBuffer = ArrayPool<byte>.Shared.Rent(bufferSize * 2);
                        buffer.CopyTo(newBuffer.AsSpan());
                        ArrayPool<byte>.Shared.Return(buffer);

                        var lineOffsetFromBufferStart_ =
                            dataOffsetFromBufferStart + lineStartFromCurrentDataOffset;

                        dataOffsetFromBufferStart = bufferSize;

                        lineStartFromCurrentDataOffset =
                            lineOffsetFromBufferStart_ - dataOffsetFromBufferStart;

                        data = newBuffer.AsSpan(dataOffsetFromBufferStart, bufferSize);
                        bytesRead = stream.Read(data);
                        streamPosition += bytesRead;

                        bufferSize *= 2;
                        buffer = newBuffer;
                    }
                }
            
                void FinishProcessing(int lastLineOffsetFromBufferStart, int bufferEndOffset, long totalBytesRead)
                {
                    _ = _lineDataConsumer.AddLineData(
                        bufferStartPosition + lastLineOffsetFromBufferStart,
                        buffer.AsSpan().Slice(
                            lastLineOffsetFromBufferStart,
                            bufferEndOffset - lastLineOffsetFromBufferStart));
                    _lineDataConsumer.CompleteAdding(totalBytesRead);
                }
            }

            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private (ulong rPattern, ulong nPattern, ulong minusPattern, ulong andPattern) 
            GetPatterns(ReadOnlySpan<byte> r, ReadOnlySpan<byte> n)
        {
            ulong ToLongBe(byte[] value)
            {
                return BitConverter.ToUInt64(value.Reverse().ToArray(), value.Length - sizeof(ulong));
            }
            
            var patternLength = r.Length;
            
            var rPattern = new byte[8];
            var nPattern = new byte[8];
            var minusPattern = new byte[8];
            var andPattern = new byte[8];
            
            for (var idx = 0; idx < 8; idx += patternLength)
            {
                r.CopyTo(rPattern.AsSpan(idx));
                n.CopyTo(nPattern.AsSpan(idx));
                minusPattern[idx + patternLength - 1] = 0x01;
                andPattern[idx] = 0x80;
            }

            return (BitConverter.ToUInt64(rPattern, 0), 
                    BitConverter.ToUInt64(nPattern, 0), 
                    ToLongBe(minusPattern),
                    ToLongBe(andPattern));
        }
    }
}