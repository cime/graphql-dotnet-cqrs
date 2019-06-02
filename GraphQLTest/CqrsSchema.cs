using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
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
            _container = container;

            Query = new ObjectGraphType()
            {
                Name = "Query"
            };
            Mutation = new ObjectGraphType()
            {
                Name = "Mutation"
            };
            //Mutation = new TestMutation();
            
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
                var inputGqlType = FindType(inputTypeName);
                
                if (inputGqlType == null)
                {
                    var inputObjectType = typeof(InputObjectGraphType<>).MakeGenericType(commandType);
                    
                    inputGqlType = (IInputObjectGraphType)Activator.CreateInstance(inputObjectType);

                    inputGqlType.Name = inputTypeName;

                    var addFieldMethod = inputGqlType.GetType().GetMethod("AddField");
                    var properties = commandType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                    foreach (var propertyInfo in properties)
                    {
                        addFieldMethod.Invoke(inputGqlType, new[]
                        {
                            new FieldType()
                            {
                                Name = CamelCase(propertyInfo.Name),
                                Type = propertyInfo.PropertyType.GetGraphTypeFromType()
                            }
                        });
                    }
                    
                    //RegisterType(inputGqlType);
                }
                
                var resultTypeName = resultType.Name;
                var resultGqlType = FindType(resultTypeName);
                
                if (resultGqlType == null)
                {
                    var returnObjectType = typeof(AutoRegisteringObjectGraphType<>).MakeGenericType(resultType);
                    
                    resultGqlType = (IGraphType)Activator.CreateInstance(returnObjectType, null);
                    resultGqlType.Name = resultTypeName;
                    
                    //RegisterType(resultGqlType);
                }


                var queryArgument = new QueryArgument(inputGqlType);
                queryArgument.Name = "command";
                
                var commandQueryParameters = new List<QueryArgument>()
                {
                    queryArgument
                };

                var mutationName = new Regex("Command$").Replace(commandType.Name, "");

                if (!Mutation.HasField(mutationName))
                {
                    var type = new FieldType
                    {
                        ResolvedType = resultGqlType,
                        Resolver = new CommandResolver(_container, commandHandlerType, commandType),
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
        
        public class CommandResolver : IFieldResolver
        {
            private readonly Container _container;
            private readonly Type _commandHandlerType;
            private readonly Type _commandType;

            public CommandResolver(Container container, Type commandHandlerType, Type commandType)
            {
                _container = container;
                _commandHandlerType = commandHandlerType;
                _commandType = commandType;
            }

            public object Resolve(ResolveFieldContext context)
            {
                var commandHandler = _container.GetInstance(_commandHandlerType);
                var command = context.GetArgument("command", _commandType);

                var handleMethodInfo =
                    _commandHandlerType.GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public);

                return handleMethodInfo.Invoke(commandHandler, new[] { command });
            }
        }
    }
}
