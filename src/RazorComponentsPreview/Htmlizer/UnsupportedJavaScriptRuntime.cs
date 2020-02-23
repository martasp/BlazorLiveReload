using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace RazorComponentsPreview
{
    public class UnsupportedJavaScriptRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            throw new InvalidOperationException("JavaScript interop calls cannot be issued during server-side prerendering, because the page has not yet loaded in the browser. Prerendered components must wrap any JavaScript interop calls in conditional logic to ensure those interop calls are not attempted during prerendering.");
        }

        ValueTask<TValue> IJSRuntime.InvokeAsync<TValue>(string identifier, object[] args)
        {
            throw new InvalidOperationException("JavaScript interop calls cannot be issued during server-side prerendering, because the page has not yet loaded in the browser. Prerendered components must wrap any JavaScript interop calls in conditional logic to ensure those interop calls are not attempted during prerendering.");
        }
    }
}