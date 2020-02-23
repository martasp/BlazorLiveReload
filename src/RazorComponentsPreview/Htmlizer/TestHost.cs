using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RazorComponentsPreview
{
    public class TestHost
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly Lazy<TestRenderer> _renderer;
        private readonly Lazy<IServiceProvider> _serviceProvider;

        public TestHost(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;

            _serviceProvider = new Lazy<IServiceProvider>(() =>
            {
                return _serviceCollection.BuildServiceProvider();
            });

            _renderer = new Lazy<TestRenderer>(() =>
            {
                var loggerFactory = Services.GetService<ILoggerFactory>() ?? new NullLoggerFactory();
                return new TestRenderer(Services, loggerFactory);
            });
        }

        public IServiceProvider Services => _serviceProvider.Value;

        public void AddService<T>(T implementation)
            => AddService<T, T>(implementation);

        public void AddService<TContract, TImplementation>(TImplementation implementation) where TImplementation: TContract
        {
            if (_renderer.IsValueCreated)
            {
                throw new InvalidOperationException("Cannot configure services after the host has started operation");
            }

            _serviceCollection.AddSingleton(typeof(TContract), implementation);
        }

        public void WaitForNextRender(Action trigger)
        {
            var task = Renderer.NextRender;
            trigger();
            task.Wait(millisecondsTimeout: 1000);

            if (!task.IsCompleted)
            {
                throw new TimeoutException("No render occurred within the timeout period.");
            }
        }

        public RenderedComponentInstance AddComponent(IComponent ComponentInstance) 
        {
            var result = new RenderedComponentInstance(Renderer, ComponentInstance);
            result.SetParametersAndRender(ParameterView.Empty);

            return result;
        }

        private TestRenderer Renderer => _renderer.Value;
    }
}
