using StandardSharp.Common.Attributes;
using StandardSharp.Common.Commands;

namespace GraphQLTest.Commands
{
    public class TestCommandResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    [Expose]
    public class TestCommand : ICommand<TestCommandResult>
    {
        public int Id { get; set; }
    }

    public class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResult>
    {
        public TestCommandResult Handle(TestCommand command)
        {
            return  new TestCommandResult();
        }
    }
}
