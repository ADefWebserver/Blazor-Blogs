using BlazorBlogs.Data.Models;

namespace BlazorBlogs;

public sealed class GeneralSettingsState
{
    public GeneralSettings Current { get; private set; } = new();
    public event EventHandler StateChanged;

    public void NotifyChanged(GeneralSettings settings)
    {
        Current = settings;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
