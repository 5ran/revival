using System;
using System.Threading.Tasks;

namespace Client.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    public DashboardViewModel()
    {
        OpenAutoEnchantCommand = new RelayCommand(_ =>
        {
            OpenAutoEnchantAction?.Invoke();
            return Task.CompletedTask;
        });

        OpenAutoAppraiseCommand = new RelayCommand(_ =>
        {
            OpenAutoAppraiseAction?.Invoke();
            return Task.CompletedTask;
        });

        OpenAutoAnglerCommand = new RelayCommand(_ =>
        {
            OpenAutoAnglerAction?.Invoke();
            return Task.CompletedTask;
        });

        OpenTreasureAppraiseCommand = new RelayCommand(_ =>
        {
            OpenTreasureAppraiseAction?.Invoke();
            return Task.CompletedTask;
        });

        OpenHuntDetectCommand = new RelayCommand(_ =>
        {
            OpenHuntDetectAction?.Invoke();
            return Task.CompletedTask;
        });

        OpenEditCommand = new RelayCommand(_ =>
        {
            OpenEditAction?.Invoke();
            return Task.CompletedTask;
        });
    }

    public Action? OpenAutoEnchantAction { get; set; }

    public Action? OpenAutoAppraiseAction { get; set; }

    public Action? OpenAutoAnglerAction { get; set; }

    public Action? OpenTreasureAppraiseAction { get; set; }

    public Action? OpenHuntDetectAction { get; set; }

    public Action? OpenEditAction { get; set; }

    public RelayCommand OpenAutoEnchantCommand { get; }

    public RelayCommand OpenAutoAppraiseCommand { get; }

    public RelayCommand OpenAutoAnglerCommand { get; }

    public RelayCommand OpenTreasureAppraiseCommand { get; }

    public RelayCommand OpenHuntDetectCommand { get; }

    public RelayCommand OpenEditCommand { get; }
}
