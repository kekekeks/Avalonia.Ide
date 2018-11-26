using System.Linq;
using Xunit;

namespace CompletionEngineTests
{
    public class KnownBugTests : XamlCompletionTestBase
    {
        [Fact]
        public void CompletionShouldRecognizeDoubleTransition()
        {
            AssertSingleCompletion("<", "DoubleTra", "DoubleTransition");
        }

        [Fact]
        public void CompletionShouldShowPropertiesFromBaseClasses()
        {
            AssertSingleCompletion("<local:EmptyClassDerivedFromGenericClassWithDouble ", "Generic", "GenericProperty=\"\"");
        }

        [Theory,
            InlineData("Animations")
        ]
        public void StylePropertiesShouldBeShown(string propertyName)
        {
            AssertSingleCompletion("<UserControl><UserControl.Styles><Style><Style.", propertyName.Substring(0, 1),
                propertyName);
        }

        [Theory,
            InlineData("Item")]
        public void NonStylePropertiesShouldNotBeShownOnStyle(string propertyName)
        {
            var comp = GetCompletionsFor("<UserControl><UserControl.Styles><Style><Style." +
                                         propertyName.Substring(0, 1));
            if (comp == null)
                return;
            Assert.Empty(comp.Completions.Where(c => c.InsertText.StartsWith(propertyName)));
        }
    }
}