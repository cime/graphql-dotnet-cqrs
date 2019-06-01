using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GraphQL.Types;
using GraphQLParser.AST;
using StandardSharp.Common.Attributes;
using StandardSharp.Common.Commands;
using StandardSharp.Common.QueryHandler;
using Container = SimpleInjector.Container;

namespace GraphQLTest
{
    public class CqrsSchema : Schema
    {
        private readonly Container _container;

        public CqrsSchema(Container container)
        {
            Query = new ObjectGraphType();
            Mutation = new ObjectGraphType();
            
            _container = container;
            RegisterCommands();
            RegisterQueries();
        }

        private void RegisterCommands()
        {
            var registrations = _container.GetRootRegistrations();
            var registeredTypes = registrations.Select(x => x.ServiceType).ToList();
            var commandHandlerTypes = registeredTypes.Where(x =>
                !x.IsInterface &&
                x.GetTypeInfo().IsAssignableToGenericType(typeof(ICommandHandler<,>))).ToList();
            
            
            foreach (var commandHandlerType in commandHandlerTypes)
            {
                var descriptionAttribute = commandHandlerType.GetTypeInfo().GetCustomAttribute<DescriptionAttribute>();
                var genericType = commandHandlerType.GetInterfaces().Single(x => x.GetTypeInfo().IsAssignableToGenericType(typeof(ICommandHandler<,>)));
                var genericArguments = genericType.GetGenericArguments();
                var commandType = genericArguments[0];
                var resultType = genericArguments[1];

                var exposeAttribute = commandType.GetCustomAttribute<ExposeAttribute>();

                if (exposeAttribute == null)
                {
                    continue;
                }

                //
                // For each command here I would like to create a mutation that has 1 input of type commandType and returns the resultType
                // Example command can be found inside Commands/TestCommand.cs
                //
                // Each command can be resolved using: var result = (new "commandHandlerType"()).Handle("command instance");
                //

                var inputTypeName = commandType.Name; // + "Input";
                var inputType = FindType(inputTypeName);
                
                if (inputType == null)
                {
                    var inputObjectType = typeof(InputObjectGraphType<>).MakeGenericType(commandType);
                    
                    inputType = (IGraphType)Activator.CreateInstance(inputObjectType);

                    inputType.Name = inputTypeName;
                    
                    RegisterType(inputType);
                }
                
                var resultTypeName = resultType.Name;
                var resultGqlType = FindType(resultTypeName);
                
                if (resultGqlType == null)
                {
                    var returnObjectType = typeof(ObjectGraphType<>).MakeGenericType(resultType);
                    
                    resultGqlType = (IGraphType)Activator.CreateInstance(returnObjectType);
                    resultGqlType.Name = resultTypeName;
                    
                    RegisterType(resultGqlType);
                }


                var queryArgument = new QueryArgument(inputType);
                queryArgument.Name = "command";
                
                var commandQueryParameters = new List<QueryArgument>()
                {
                    queryArgument
                };

                var mutationName = CamelCase(new Regex("CommandHandler$").Replace(commandHandlerType.Name, ""));

                if (!Mutation.HasField(mutationName))
                {
                    var type = new FieldType
                    {
                        Type = resultGqlType.GetType(), //.ToGraphType(),
                        ResolvedType = resultGqlType,
                        Name = CamelCase(mutationName),
                        Description = descriptionAttribute?.Description,
                        Arguments = new QueryArguments(commandQueryParameters)
                    };

                    Mutation.AddField(type);
                }
            }
        }

        public void RegisterQueries()
        {
            var registrations = _container.GetRootRegistrations();
            var registeredTypes = registrations.Select(x => x.ServiceType).ToList();
            var queries = registeredTypes.Where(x =>
                !x.IsInterface &&
                x.GetTypeInfo().IsAssignableToGenericType(typeof(IQueryHandler<,>))).ToList();


            foreach (var query in queries)
            {
                
            }
        }
        
        private static string CamelCase(string s)
        {
            return s.Substring(0, 1).ToLower() + s.Substring(1);
        }
    }
}
