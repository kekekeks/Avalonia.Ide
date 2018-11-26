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
    }
}