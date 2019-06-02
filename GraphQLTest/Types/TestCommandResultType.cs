using GraphQL.Types;
using GraphQLTest.Commands;

namespace GraphQLTest.Types
{
    public class TestCommandResultType : ObjectGraphType<TestCommandResult>
    {
        public TestCommandResultType()
        {
            Field(x => x.Id);
            Field(x => x.Name);
        }
    }
}
