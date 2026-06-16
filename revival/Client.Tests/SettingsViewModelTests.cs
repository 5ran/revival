using Client.ViewModels;
using Xunit;

namespace Client.Tests;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void SettingsViewModel_ExposesEmbeddedClientSettings()
    {
        var viewModel = new SettingsViewModel();

        Assert.NotNull(viewModel.ClientSettings);
    }
}
