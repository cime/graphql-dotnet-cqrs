using GraphQL.Types;
using GraphQLTest.Commands;

namespace GraphQLTest.Types
{
    public class TestCommandType : InputObjectGraphType<TestCommand>
    {
        public TestCommandType()
        {
            Field(x => x.Id);
        }
    }
}
