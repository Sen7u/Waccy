using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Inkboard.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // 显式加载 XAML：当前 SDK 的 Roslyn 低于 Avalonia 12 分析器要求时，
        // 源生成器可能不生成 InitializeComponent。
        AvaloniaXamlLoader.Load(this);
    }
}
