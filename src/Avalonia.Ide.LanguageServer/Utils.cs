using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PimpMyAvalonia.LanguageServer
{
    public static class Utils
    {
        public static int PositionToOffset(Position position, ReadOnlySpan<char> data)
        {
            return PositionToOffset(position.Line, position.Character, data);
        }

        public static int PositionToOffset(int line, int character, ReadOnlySpan<char> data)
        {
            int position = 0;
            for (int i = 0; i < line; i++)
            {
                position = FindNextLine(data, position);
            }
            position += character;
            return position;
        }

        public static Position OffsetToPosition(int offset, ReadOnlySpan<char> data)
        {
            int character = 0;
            int line = 0;

            if (offset != 0 && data.Length < offset)
            {
                offset = data.Length - 1;
            }

            void NewLine()
            {
                line++;
                character = 0;
            }

            int i = 0;
            for (; i < offset; i++)
            {
                if (data[i] == '\n')
                {
                    NewLine();
                }
                else if (data[i] == '\r')
                {
                    bool hasRn = i + 1 < data.Length && data[i + 1] == '\n';
                    if (hasRn)
                    {
                        i++;
                    }
                    NewLine();
                }
                else
                {
                    character++;
                }
            }

            //bool checkIfEndedOnNewLine = data.Length > i;
            //if (checkIfEndedOnNewLine)
            //{
            //    if (data[i] == '\n')
            //    {
            //        NewLine();
            //    }
            //    else if (data[i] == '\r')
            //    {
            //        NewLine();
            //    }
            //}


            return new Position(line, character);
        }

        public static Position AddPosition(Position start, string text)
        {
            int foundCharacters = start.Character;
            int foundLines = 0;

            void NextLine()
            {
                foundLines++;
                foundCharacters = 0;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\r')
                {
                    bool hasRn = text.Length > i + 1 && text[i] == '\n';
                    if (hasRn)
                    {
                        i++;
                    }
                    NextLine();
                    continue;
                }
                else if (text[i] == '\n')
                {
                    NextLine();
                    continue;
                }
                else
                {
                    foundCharacters++;
                }
            }

            return new Position(start.Line + foundLines, foundCharacters);
        }

        private static int FindNextLine(ReadOnlySpan<char> data, int position)
        {
            while (position < data.Length)
            {
                if (data[position] == '\n')
                {
                    position++;
                    return position;
                }
                else if (data[position] == '\r')
                {
                    bool foundRN = false;
                    if (data.Length > position + 1)
                    {
                        if (data[position + 1] == '\n')
                        {
                            foundRN = true;
                        }
                    }

                    if (foundRN)
                    {
                        position += 2;
                        return position;
                    }
                    else
                    {
                        position += 1;
                        return position;
                    }
                }
                else
                {
                    position += 1;
                }
            }

            return position;
        }
    }
}
