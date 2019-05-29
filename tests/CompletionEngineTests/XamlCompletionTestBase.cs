using System;
using System.Linq;
using Avalonia.Ide.CompletionEngine;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using Avalonia.Ide.CompletionEngine.DnlibMetadataProvider;
using Xunit;

namespace CompletionEngineTests
{
    public class XamlCompletionTestBase
    {
        private static readonly string Prologue = @"<UserControl xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006'
        xmlns:local='clr-namespace:CompletionEngineTests.Models;assembly=CompletionEngineTests'>".Replace("'", "\"");


        private static Metadata Metadata = new MetadataReader(new DnlibMetadataProvider())
            .GetForTargetAssembly(typeof(XamlCompletionTestBase).Assembly.GetModules()[0].FullyQualifiedName);

        CompletionSet TransformCompletionSet(CompletionSet set)
        {
            if (set == null)
                return null;
            return new CompletionSet
            {
                StartPosition = set.StartPosition - Prologue.Length,
                Completions = set.Completions.Select(c => new Completion(c.DisplayText, c.InsertText, c.Description,
                    c.Kind, c.RecommendedCursorOffset - Prologue.Length)).ToList()
            };
        }

        protected CompletionSet GetCompletionsFor(string xaml)
        {
            xaml = Prologue + xaml;
            var engine = new CompletionEngine();
            var set = engine.GetCompletions(Metadata, xaml, xaml.Length);
            return TransformCompletionSet(set);
        }

        protected void AssertSingleCompletion(string xaml, string typed, string completion)
        {
            var comp = GetCompletionsFor(xaml + typed);
            if (comp == null)
                throw new Exception("No completions found");

            Assert.True(xaml.Length == comp.StartPosition, $"Invalid completion start position typed: {typed} expected: {completion}");

            Assert.Contains(comp.Completions, c => c.InsertText == completion);

            Assert.Single(comp.Completions, c => c.InsertText == completion);
        }
    }
}