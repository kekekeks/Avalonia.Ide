using Xunit;

namespace CompletionEngineTests.Manipulator
{
    public class ClosingTagsTests : ManipulatorTestBase
    {
        [Fact]
        public void DoNotCloseEmptyTag()
        {
            AssertInsertion("<$", "/", "</");
        }

        [Fact]
        public void CloseTagWithSlash()
        {
            AssertInsertion("<Tag$", "/", "<Tag/>");
        }

        [Fact]
        public void ConvertTagToSelfClosingWithSlash()
        {
            AssertInsertion("<Tag$></Tag>", "/", "<Tag/>");
        }


        [Fact]
        public void DoNotCloseTagWithAngleBracket()
        {
            // NOTE: Visual studio closes tags by itself, so we cannot implement this in completion engine
            // In visual studio result of such operation will be <Tag></Tag>
            AssertInsertion("<Tag$", ">", "<Tag>");
        }

    }
}
