using System;
using GraphQL;
using GraphQLTest.Commands;
using GraphQLTest.Queries;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace GraphQLTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var container = new Container();
            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
            container.RegisterCommandHandlersFromAssemblyOf<TestCommand>();
            container.RegisterQueryHandlersFromAssemblyOf<TestQuery>();
            container.Verify();
            
            var schema = new CqrsSchema(container);
            var result = schema.Execute(_ =>
            {
                _.Query = IntrospectionQuery.Query;
            });
            
            Console.WriteLine(result);
        }
    }
}