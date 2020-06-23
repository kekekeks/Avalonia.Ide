using Xunit;

namespace CompletionEngineTests.Manipulator
{
    public class ClosingTagsTests : ManipulatorTestBase
    {
        [Fact]
        public void CloseEmptyTag()
        {
            AssertInsertion("<Tag$", "/", "<Tag/>");
        }

        [Fact]
        public void DoesNotCloseTags()
        {
            // NOTE: Visual studio closes tags by itself, so we cannot implement this in completion engine
            // In visual studio result of such operation will be <Tag></Tag>
            AssertInsertion("<Tag$", ">", "<Tag>");
        }

    }
}
