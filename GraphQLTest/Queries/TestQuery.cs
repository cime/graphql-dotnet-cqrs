using StandardSharp.Common.Attributes;
using StandardSharp.Common.QueryHandler;

namespace GraphQLTest.Queries
{
    public class TestQueryResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    [Expose]
    public class TestQuery : IQuery<TestQueryResult>
    {
        public int Id { get; set; }
    }

    public class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResult>
    {
        public TestQueryResult Handle(TestQuery query)
        {
            return new TestQueryResult()
            {
                Id = query.Id,
                Name = "Test " + query.Id
            };
        }
    }
}