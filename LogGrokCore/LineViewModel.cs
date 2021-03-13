using LogGrokCore.Controls;
using LogGrokCore.Data;

namespace LogGrokCore
{
    public class LineViewModel : BaseLogLineViewModel
    {
        private readonly string _sourceString;

        private readonly ParseResult _parseResult;

        public LineViewModel(int index, string sourceString, ILineParser parser, Selection markedLines)
            : base(index, markedLines)
        {
            _sourceString = sourceString;
            _parseResult = parser.Parse(sourceString);
        }

        public string this[int index] => GetValue(index);

        public string GetValue(int index)
        {
            var lineMeta = _parseResult.Get().ParsedLineComponents;
            return _sourceString.Substring(lineMeta.ComponentStart(index),
                    lineMeta.ComponentLength(index))
                .TrimEnd('\0').TrimEnd();
        }

        public override string ToString()
        {
            return _sourceString;
        }
    }
}