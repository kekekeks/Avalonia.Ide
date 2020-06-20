using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Ide.CompletionEngine
{
    /// <summary>
    /// Allows IDE to manipulate text typed by user, without completion session
    /// I.E. close xml tags, rename end tag, itp.
    /// </summary>
    public class TextManipulator
    {
        private readonly Metadata _completionMetadata;
        private readonly string _text;
        private readonly int _position;
        private readonly XmlParser _state;
        private readonly int _parserOffset = 0;
        private ReadOnlyMemory<char> _remainingText;

        public TextManipulator(Metadata completionMetadata, string text, int position)
        {
            _completionMetadata = completionMetadata;
            _text = text;
            _position = position;

            
            var fullText = text.AsMemory();
            var textToParse = fullText;

            // To improve performance parse only last tag
            if(text.Length > 0)
            {
                var parserStart = position;
                if (parserStart >= text.Length)
                {
                    parserStart = text.Length - 1;
                }
                parserStart = text.LastIndexOf('<', parserStart);
                if(parserStart < 0)
                {
                    parserStart = 0;
                }

                int parserEnd;
                if(text.Length > position)
                {
                    parserEnd = position;
                }
                else
                {
                    parserEnd = position - 1;
                }

                _parserOffset = parserStart;
                textToParse = textToParse.Slice(parserStart, parserEnd - parserStart);

                _remainingText = fullText.Slice(parserEnd);
            }
            

            _state = XmlParser.Parse(textToParse);
        }

        public IList<TextManipulation> ManipulateText(ITextChange textChange)
        {
            IList<TextManipulation> maniplations = new List<TextManipulation>();
            switch (_state.State)
            {
                case XmlParser.ParserState.StartElement:
                case XmlParser.ParserState.AfterAttributeValue:
                case XmlParser.ParserState.InsideElement:
                    TryCloseTag(textChange, maniplations);
                    break;
            
            }

            return maniplations.OrderByDescending(n => n.Start).ToList();
        }

        private void TryCloseTag(ITextChange textChange, IList<TextManipulation> manipulations)
        {
            if(textChange.NewText == "/" && !string.IsNullOrEmpty(_state.TagName))
            {
                if (IsTagAlreadyClosed())
                {
                    return;
                }

                manipulations.Add(TextManipulation.Insert(_position + 1, $">"));
            }
        }

        private bool IsTagAlreadyClosed()
        {
            if (_remainingText.Length > 1)
            {
                var remainingSpan = _remainingText.Span;
                for (int i = 1; i < _remainingText.Length; i++)
                {
                    var nextChar = remainingSpan[i];
                    if (!char.IsWhiteSpace(nextChar))
                    {
                        if (nextChar == '>')
                        {
                            return true;
                        }
                        break;
                    }
                }
            }

            return false;
        }
    }
}
