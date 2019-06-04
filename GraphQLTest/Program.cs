using System;
using GraphQL;
using GraphQLTest.Commands;
using GraphQLTest.Queries;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
            
            result = schema.Execute(_ =>
            {
                _.Inputs = JsonConvert.SerializeObject(new { Command = new TestCommand() { Id = 123 } }, Formatting.None, new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() }).ToInputs();
                _.Query = @"mutation TestCommand ($command: TestCommand) {
                    result: test(command: $command) {
                        id
                        name
                    }
                }";
            });
            
            Console.WriteLine(result);
        }
    }
}
