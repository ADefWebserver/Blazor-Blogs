using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace BlazorBlogs
{
    public static class GoogleInterop
    {
        internal static ValueTask<object> gaTracking(
            IJSRuntime jsRuntime,
            string gaTrackingID)
        {
            return jsRuntime.InvokeAsync<object>(
                "gaFunctions.gaTracking",
                gaTrackingID);
        }
    }
}