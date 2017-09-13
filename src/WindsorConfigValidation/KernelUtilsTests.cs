using System;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using NUnit.Framework;

namespace WindsorConfigValidation
{
    public static partial class KernelUtilsTests
    {
        public sealed class Dependency_on_service_with_unknown_arg : ContainerFixture
        {
            private sealed class ServiceWithUnknownArg
            {
                public ServiceWithUnknownArg(int arg)
                {
                }
            }

            [Test]
            public void Should_be_invalid()
            {
                Assert.That(Kernel, HasInvalidComponent<ServiceWithUnknownArg>());
            }
        }

        public sealed class Dependency_on_service_with_unknown_arg_with_explicit_factory : ContainerFixture
        {
            private sealed class ServiceWithUnknownArg
            {
                public ServiceWithUnknownArg(int arg)
                {
                }
            }

            public Dependency_on_service_with_unknown_arg_with_explicit_factory()
            {
                Kernel.Register(Component.For<Func<int, ServiceWithUnknownArg>>().AsFactory());
            }

            [Test]
            public void Should_be_valid()
            {
                Assert.That(Kernel, HasValidComponent<ServiceWithUnknownArg>());
            }
        }

        public sealed class Dependency_on_factory_of_service_with_unknown_arg_implicit : ContainerFixture
        {
            private sealed class ServiceWithUnknownArg
            {
                public ServiceWithUnknownArg(int arg)
                {
                }
            }

            private sealed class ConsumingService
            {
                public ConsumingService(Func<int, ServiceWithUnknownArg> factory) { }
            }

            [Test]
            public void Service_with_unknown_arg_should_be_valid()
            {
                Assert.That(Kernel, HasValidComponent<ServiceWithUnknownArg>());
            }

            [Test]
            public void Consuming_service_should_be_valid()
            {
                Assert.That(Kernel, HasValidComponent<ConsumingService>());
            }
        }

        public sealed class Dependency_on_factory_of_service_with_unknown_arg_explicit : ContainerFixture
        {
            private sealed class ServiceWithUnknownArg
            {
                public ServiceWithUnknownArg(int arg)
                {
                }
            }

            private sealed class ConsumingService
            {
                public ConsumingService(Func<int, ServiceWithUnknownArg> factory)
                {
                }
            }

            public Dependency_on_factory_of_service_with_unknown_arg_explicit()
            {
                Kernel.Register(Component.For<Func<int, ServiceWithUnknownArg>>().AsFactory());
            }

            [Test]
            public void Service_with_unknown_arg_should_be_valid()
            {
                Assert.That(Kernel, HasValidComponent<ServiceWithUnknownArg>());
            }

            [Test]
            public void Consuming_service_should_be_valid()
            {
                Assert.That(Kernel, HasValidComponent<ConsumingService>());
            }
        }

        public sealed class Dependency_on_factory_of_service_missing_unknown_arg_with_implicit_factory : ContainerFixture
        {
            private sealed class ServiceWithUnknownArg
            {
                public ServiceWithUnknownArg(int arg)
                {
                }
            }

            private sealed class ConsumingService
            {
                public ConsumingService(Func<ServiceWithUnknownArg> factory) { }
            }

            [Test]
            public void Service_with_unknown_arg_should_be_invalid()
            {
                Assert.That(Kernel, HasInvalidComponent<ServiceWithUnknownArg>());
            }

            [Test]
            public void Consuming_service_with_improper_factory_should_be_invalid()
            {
                Assert.That(Kernel, HasInvalidComponent<ConsumingService>());
            }
        }

        public sealed class Dependency_on_factory_of_service_missing_unknown_arg_with_explicit_factory : ContainerFixture
        {
            private sealed class ServiceWithUnknownArg
            {
                public ServiceWithUnknownArg(int arg)
                {
                }
            }

            private sealed class ConsumingService
            {
                public ConsumingService(Func<ServiceWithUnknownArg> factory)
                {
                }
            }

            public Dependency_on_factory_of_service_missing_unknown_arg_with_explicit_factory()
            {
                Kernel.Register(Component.For<Func<int, ServiceWithUnknownArg>>().AsFactory());
            }

            [Test]
            public void Service_with_unknown_arg_should_be_valid()
            {
                Assert.That(Kernel, HasValidComponent<ServiceWithUnknownArg>());
            }

            [Test]
            public void Consuming_service_with_improper_factory_should_be_invalid()
            {
                Assert.That(Kernel, HasInvalidComponent<ConsumingService>());
            }
        }
    }
}
