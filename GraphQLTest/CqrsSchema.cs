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
            var commands = registeredTypes.Where(x =>
                !x.IsInterface &&
                x.GetTypeInfo().IsAssignableToGenericType(typeof(ICommandHandler<,>)) ||
                x.GetTypeInfo().IsAssignableToGenericType(typeof(IAsyncCommandHandler<,>))).ToList();
            
            
            foreach (var command in commands)
            {
                var descriptionAttribute = command.GetTypeInfo().GetCustomAttribute<DescriptionAttribute>();
                var genericType = command.GetInterfaces().Single(x => x.GetTypeInfo().IsAssignableToGenericType(typeof(ICommandHandler<,>)) || x.GetTypeInfo().IsAssignableToGenericType(typeof(IAsyncCommandHandler<,>)));
                var commandInputType = genericType.GetGenericArguments();
                var commandType = commandInputType[0];
                var returnType = commandInputType[1];

                var exposeAttribute = commandType.GetCustomAttribute<ExposeAttribute>();

                if (exposeAttribute == null)
                {
                    continue;
                }
                
                var inputTypeName = commandType.Name; // + "Input";
                var inputType = FindType(inputTypeName);
                
                if (inputType == null)
                {
                    var inputObjectType = typeof(InputObjectGraphType<>).MakeGenericType(commandType);
                    
                    inputType = (IGraphType)Activator.CreateInstance(inputObjectType);

                    inputType.Name = inputTypeName;
                    
                    RegisterType(inputType);
                }
                
                var returnTypeName = returnType.Name;
                var returnTypeGql = FindType(returnTypeName);
                
                if (returnTypeGql == null)
                {
                    var returnObjectType = typeof(ObjectGraphType<>).MakeGenericType(returnType);
                    
                    returnTypeGql = (IGraphType)Activator.CreateInstance(returnObjectType);
                    returnTypeGql.Name = returnTypeName;
                    
                    RegisterType(returnTypeGql);
                }


                var queryArgument = new QueryArgument(inputType);
                queryArgument.Name = "command";
                
                var commandQueryParameters = new List<QueryArgument>()
                {
                    queryArgument
                };

                var mutationName = CamelCase(new Regex("CommandHandler$").Replace(command.Name, ""));

                if (!Mutation.HasField(mutationName))
                {
                    var type = new FieldType
                    {
                        Type = returnTypeGql.GetType(), //.ToGraphType(),
                        ResolvedType = returnTypeGql,
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
                x.GetTypeInfo().IsAssignableToGenericType(typeof(IQueryHandler<,>)) ||
                x.GetTypeInfo().IsAssignableToGenericType(typeof(IAsyncQueryHandler<,>))).ToList();


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
