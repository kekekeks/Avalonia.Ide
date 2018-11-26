using Xunit;

namespace CompletionEngineTests
{
    public class BasicTests : XamlCompletionTestBase
    {
        [Fact]
        public void ClosingTagShouldBeProperlyCompleted()
        {
            AssertSingleCompletion("<UserControl><Button><Button.Styles><Style/></Button.Styles><", "/", "/Button>");
        }
    }
}