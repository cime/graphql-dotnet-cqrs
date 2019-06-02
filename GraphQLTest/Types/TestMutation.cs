using GraphQL.Types;

namespace GraphQLTest.Types
{
    public class TestMutation : ObjectGraphType<object>
    {
        public TestMutation()
        {
            Name = "Mutation";

            Field<TestCommandResultType>(
                "test",
                arguments: new QueryArguments(new QueryArgument<TestCommandType>() {Name = "command"}),
                resolve: context =>
                {
                    return null; 
                });
        }
    }
}