using Microsoft.AspNetCore.Components;

namespace RazorComponentsPreview
{
    public class RenderedComponentInstance
    {
        private readonly TestRenderer _renderer;
        private readonly ContainerComponent _containerTestRootComponent;
        private int _testComponentId;
        private IComponent _testComponentInstance;

        internal RenderedComponentInstance(TestRenderer renderer, IComponent componentInstance)
        {
            _renderer = renderer;
            _containerTestRootComponent = new ContainerComponent(_renderer);
            _testComponentInstance = componentInstance;
        }

        public IComponent Instance => _testComponentInstance;

        public string GetMarkup()
        {
            return Htmlizer.GetHtml(_renderer, _testComponentId);
        }

        internal void SetParametersAndRender(ParameterView parameters)
        {
            _containerTestRootComponent.RenderComponentUnderTest(
                _testComponentInstance.GetType(), parameters);
            var foundTestComponent = _containerTestRootComponent.FindComponentUnderTest();
            _testComponentId = foundTestComponent.Item1;
            _testComponentInstance = (IComponent)foundTestComponent.Item2;
        }

    }
}
