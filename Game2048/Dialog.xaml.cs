using EleCho.WpfSuite.Helpers;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using WSButton = EleCho.WpfSuite.Controls.Button;

namespace Game2048
{
    /// <summary>
    /// Dialog.xaml 的交互逻辑
    /// </summary>
    public partial class Dialog : Window
    {
        public void UpdateColor() => InitializeColor();

        public static Dialog Instance { get; set; } = new();

        private Dialog()
        {
            Instance?.Close();
            Instance = this;

            InitializeComponent();
            InitializeOwnerEvent();
        }

        private void InitializeColor()
        {
            Color themeColor = MainWindow.ThemeColor;
            if (MainWindow.Instance.GetTheme() == MainWindow.Theme.Light)
            {
                themeColor = ColorUtil.GetDarkerColor(themeColor);
            }
            WindowOption.SetBorderColor(this, themeColor);
            foreach (WSButton button in ButtonStackPanel.Children)
            {
                button.BorderBrush = new SolidColorBrush(themeColor);
            }
        }

        private void InitializeOwnerEvent()
        {
            Owner = MainWindow.Instance;
            Owner.LocationChanged += OwnerSizeAndLocationChanged;
            Owner.SizeChanged += OwnerSizeAndLocationChanged;
        }

        public Dialog(object content, WSButton[]? buttons = null) : this()
        {
            UIElement element = content switch
            {
                UIElement uiElement => uiElement,
                _ => GetDialogTextBlock($"{content}")
            };

            ContentGrid.Children.Add(element);

            buttons ??= [];

            foreach (var button in buttons)
            {
                ButtonStackPanel.Children.Add(button);
            }
            InitializeColor();
        }

        public static void Show(object content, WSButton[]? buttons = null)
        {
            Dialog dialog = new(content, buttons);
            dialog.Show();
            dialog.Center();
        }

        /// <summary>
        /// 从资源中获取元素的副本。
        /// </summary>
        /// <param name="name">资源的名称。</param>
        /// <returns>从资源中获取的元素的副本。</returns>
        private static UIElement GetResourceElement(string name)
        {
            // 从当前实例的资源中获取指定名称的元素
            var sourceElement = Instance.Resources[name];

            // 将源元素序列化为 XAML 字符串
            string xaml = XamlWriter.Save(sourceElement);

            // 使用 StringReader 和 XmlReader 从 XAML 字符串加载新的 UIElement 副本
            using StringReader stringReader = new(xaml);
            using XmlReader xmlReader = XmlReader.Create(stringReader);

            // 反序列化 XAML 字符串并创建副本
            UIElement copiedElement = (UIElement)XamlReader.Load(xmlReader);

            // 返回创建的副本
            return copiedElement;
        }

        public static TextBlock GetDialogTextBlock(string str = "")
        {
            TextBlock textBlock = (TextBlock)GetResourceElement("DialogBlock");
            textBlock.Text = str;
            return textBlock;
        }

        public static WSButton GetDialogButton(string str = "", RoutedEventHandler? clickEventHandler = null)
        {
            WSButton button = (WSButton)GetResourceElement("DialogButton");
            button.Content = str;
            if (clickEventHandler != null)
            {
                button.Click += clickEventHandler;
            }

            return button;
        }

        private void OwnerSizeAndLocationChanged(object? sender, EventArgs e)
        {
            Center();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Owner.LocationChanged -= OwnerSizeAndLocationChanged;
            Owner.SizeChanged -= OwnerSizeAndLocationChanged;
        }

        private void OkBut_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Center()
        {
            Top = Owner.Top + (Owner.Height - Height) / 2;
            Left = Owner.Left + (Owner.Width - Width) / 2;
        }

        private void ContentStackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Height = MainStackPanel.ActualHeight + 100;
            Width = MainStackPanel.ActualWidth + 200;
        }

    }
}
