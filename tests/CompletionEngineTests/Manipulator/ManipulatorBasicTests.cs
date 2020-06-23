using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Ide.CompletionEngine;
using Xunit;

namespace CompletionEngineTests.Manipulator
{

    public class ManipulatorBasicTests : ManipulatorTestBase
    {

        [Fact]
        public void DoesNotInsertToUnclosedTag()
        {
            AssertInsertion("<Alpha$><Alpha>", "Beta", "<AlphaBeta><Alpha>");
        }


        [Fact]
        public void DoesNotInsertToAnotherTag()
        {
            AssertInsertion("<Alpha$><Gamma>", "Beta", "<AlphaBeta><Gamma>");
        }

        [Fact]
        public void InsertsInClosingTagAtMiddle()
        {
            AssertInsertion("<Alpha$Beta></AlphaBeta>", "Phi", "<AlphaPhiBeta></AlphaPhiBeta>");
        }

        [Fact]
        public void InsertsInClosingTagAtStart()
        {
            AssertInsertion("<$Beta></Beta>", "Alpha", "<AlphaBeta></AlphaBeta>");
        }

        [Fact]
        public void InsertsInClosingTagAtEnd()
        {
            AssertInsertion("<Alpha$></Alpha>", "Beta", "<AlphaBeta></AlphaBeta>");
        }

        [Fact]
        public void InsertsTextAtEndTagWithSubtag()
        {
            AssertInsertion("<Alpha$><Foo></Foo></Alpha>", "Beta", "<AlphaBeta><Foo></Foo></AlphaBeta>");
        }

        [Fact]
        public void InsertsTextAtEndTagWithSubtagSelfClosed()
        {
            AssertInsertion("<Alpha$><Foo/></Alpha>", "Beta", "<AlphaBeta><Foo/></AlphaBeta>");
        }

        [Fact]
        public void DoesNotInsertWhenIncorrectNesting()
        {
            AssertInsertion("<Alpha$><Foo></Alpha>", "Beta", "<AlphaBeta><Foo></Alpha>");
        }

        [Fact]
        public void RemovesInClosingTagAtStart()
        {
            AssertReplacement("<$Alpha$Beta></AlphaBeta>", "", "<Beta></Beta>");
        }

        [Fact]
        public void RemovesInClosingTagAtMiddle()
        {
            AssertReplacement("<Alpha$Phi$Beta></AlphaPhiBeta>", "", "<AlphaBeta></AlphaBeta>");
        }

        [Fact]
        public void RemovesInClosingTagAtEnd()
        {
            AssertReplacement("<AlphaBeta$Omega$></AlphaBetaOmega>", "", "<AlphaBeta></AlphaBeta>");
        }


        [Fact]
        public void ReplacesInClosingTagAtStart()
        {
            AssertReplacement("<$Alpha$Beta></AlphaBeta>", "Gamma", "<GammaBeta></GammaBeta>");
        }

        [Fact]
        public void ReplacesInClosingTagAtMiddle()
        {
            AssertReplacement("<Alpha$Phi$Beta></AlphaPhiBeta>", "Gamma", "<AlphaGammaBeta></AlphaGammaBeta>");
        }

        [Fact]
        public void ReplacesInClosingTagAtEnd()
        {
            AssertReplacement("<AlphaBeta$Omega$></AlphaBetaOmega>", "Gamma", "<AlphaBetaGamma></AlphaBetaGamma>");
        }
    }
}
