using Microsoft.Extensions.Configuration;
using System.Windows;

namespace Game2048
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IConfiguration? Config { get; private set; }

        public App()
        {
            Config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddIniFile("appsetting.ini", optional: true, reloadOnChange: false)
                .Build();
        }
    }

}
