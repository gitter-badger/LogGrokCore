using System;
using LogGrokCore.Controls;
using LogGrokCore.Data;

namespace LogGrokCore
{
    public class LineViewModel : BaseLogLineViewModel
    {
        private readonly string _sourceString;

        private readonly ParseResult _parseResult;
        private readonly string _transformResout;

        public LineViewModel(int index, string sourceString, ILineParser parser, Selection markedLines,
            TransformationPerformer transformationPerformer)
            : base(index, markedLines)
        {
            _sourceString = sourceString;
            _transformResout = transformationPerformer.Transform(sourceString); 
            _parseResult = parser.Parse(
                _transformResout);
        }

        public LinePartViewModel this[int index] => GetValue(index);

        private LinePartViewModel GetValue(int index)
        {
            var lineMeta = _parseResult.Get().ParsedLineComponents;
            var text = _transformResout.Substring(lineMeta.ComponentStart(index),
                lineMeta.ComponentLength(index));

            return new LinePartViewModel(text);
        }

        public override bool Equals(object? o)
        {
            if (o is LineViewModel other)
                return other.Index == Index && other._sourceString == _sourceString;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, _sourceString);
        }

        public override string ToString()
        {
            return _transformResout.TrimEnd();
        }
    }
}