using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.Core;
using Castle.Facilities.TypedFactory;
using Castle.Facilities.TypedFactory.Internal;
using Castle.MicroKernel;
using Castle.MicroKernel.SubSystems.Naming;

namespace WindsorConfigValidation
{
    public static class KernelUtils
    {
        // This method signature design is a work in progress till I get everything working- public APIs should not return tuples.
        // Also the internals of the method will be refactored into a much more readable structure.

        public static IReadOnlyCollection<(DependencyModel dependency, IHandler handler)>
            GetUnresolvableDependencies(this IKernel kernel)
        {
            if (!kernel.GetFacilities().OfType<TypedFactoryFacility>().Any())
                throw new NotImplementedException("TODO: test without typed factory facility");


            var allHandlers = ((INamingSubSystem)kernel.GetSubSystem(SubSystemConstants.NamingKey)).GetAllHandlers();


            // Index factory methods by component type
            var factoryMethodsByComponentType = new Dictionary<Type, List<MethodInfo>>();
            var implicitFactories = new HashSet<Type>();

            foreach (var handler in allHandlers)
                VisitModelHierarchy(handler.ComponentModel);

            void VisitModelHierarchy(ComponentModel model)
            {
                var factoryMap = (Dictionary<MethodInfo, FactoryMethod>)model.ExtendedProperties[TypedFactoryFacility.FactoryMapCacheKey];
                if (factoryMap != null)
                {
                    foreach (var (method, kind) in factoryMap)
                    {
                        if (kind != FactoryMethod.Resolve) continue;
                        if (method.DeclaringType.IsSubclassOf(typeof(Delegate)) && method.Name != "Invoke") continue;

                        var componentType = method.ReturnType; // See DefaultDelegateComponentSelector.GetComponentType

                        if (!factoryMethodsByComponentType.TryGetValue(componentType, out var methods))
                            factoryMethodsByComponentType.Add(componentType, methods = new List<MethodInfo>());
                        methods.Add(method);
                    }
                }

                foreach (var dependency in model.Dependencies)
                {
                    var handlers = kernel.GetHandlers(dependency.TargetItemType);
                    if (handlers.Length != 0)
                    {
                        foreach (var dependencyHandler in handlers)
                            VisitModelHierarchy(dependencyHandler.ComponentModel);
                    }
                    else
                    {
                        var dependencyHandler = ((IKernelInternal)kernel).LoadHandlerByType(dependency.DependencyKey, dependency.TargetItemType, new Arguments());
                        if (dependencyHandler != null)
                        {
                            implicitFactories.Add(dependency.TargetItemType);
                            VisitModelHierarchy(
                                kernel.ComponentModelBuilder.BuildModel(
                                    dependencyHandler.ComponentModel.ComponentName,
                                    new[] { dependency.TargetItemType },
                                    dependency.TargetItemType,
                                    dependencyHandler.ComponentModel.ExtendedProperties));
                        }
                    }
                }
            }


            // Search for invalid dependencies

            var actuallyUnresolvable = new List<(DependencyModel dependency, IHandler handler)>();
            var visitedHandlers = new HashSet<IHandler>();

            foreach (var handler in allHandlers)
                FindActuallyUnresolvableDependencies(handler);

            void FindActuallyUnresolvableDependencies(IHandler handler)
            {
                if (handler.CurrentState == HandlerState.Valid || !visitedHandlers.Add(handler)) return;

                var allTypedFactoryResolveMethods = (List<MethodInfo>)null;

                foreach (var dependency in handler.ComponentModel.Dependencies)
                {
                    if (dependency.IsOptional || dependency.HasDefaultValue) continue;

                    var dependencyHandler = kernel.GetHandler(dependency.TargetItemType);
                    if (dependencyHandler != null)
                    {
                        FindActuallyUnresolvableDependencies(dependencyHandler);
                    }
                    else
                    {
                        // Lazy load all typed factory resolve methods that produce the handler's service types
                        if (allTypedFactoryResolveMethods == null)
                        {
                            allTypedFactoryResolveMethods = new List<MethodInfo>();

                            foreach (var componentType in handler.ComponentModel.Services)
                                if (factoryMethodsByComponentType.TryGetValue(componentType, out var resolveMethods))
                                    allTypedFactoryResolveMethods.AddRange(resolveMethods);
                        }

                        // A dependency is valid if the the dependent service is only referred to via typed factories
                        // and every typed factory requires this dependency in the resolve method.
                        if (allTypedFactoryResolveMethods.Count == 0
                            || allTypedFactoryResolveMethods.Any(m => m.GetParameters().All(p => p.ParameterType != dependency.TargetItemType)))
                        {
                            actuallyUnresolvable.Add((dependency, handler));
                        }
                    }
                }
            }

            return actuallyUnresolvable;
        }
    }
}
