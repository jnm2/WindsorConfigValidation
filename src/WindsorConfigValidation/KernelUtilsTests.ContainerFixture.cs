using System;
using System.Linq;
using System.Reflection;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework.Constraints;

namespace WindsorConfigValidation
{
    partial class KernelUtilsTests
    {
        public abstract class ContainerFixture : IDisposable
        {
            private readonly IWindsorContainer container;

            protected IKernel Kernel => container.Kernel;

            protected ContainerFixture()
            {
                container = new WindsorContainer();
                container.AddFacility<TypedFactoryFacility>();

                foreach (var nestedType in GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                    container.Register(Component.For(nestedType));
            }

            protected Constraint HasInvalidComponent<TComponent>() => new ComponentValidityConstraint(typeof(TComponent), isValid: false);
            protected Constraint HasValidComponent<TComponent>() => new ComponentValidityConstraint(typeof(TComponent), isValid: true);

            private sealed class ComponentValidityConstraint : Constraint
            {
                private readonly Type componentType;
                private readonly bool isValid;

                public ComponentValidityConstraint(Type componentType, bool isValid)
                {
                    this.componentType = componentType;
                    this.isValid = isValid;
                }

                public override ConstraintResult ApplyTo<TActual>(TActual actual)
                {
                    if (!((object)actual is IKernel kernel)) throw new ArgumentException("Expected an IKernel", nameof(actual));

                    return new ConstraintResult(this, actual,
                        isValid != kernel.GetUnresolvableDependencies().Any(_ => _.handler.Supports(componentType)));
                }
            }

            public void Dispose()
            {
                container.Dispose();
            }
        }
    }
}
