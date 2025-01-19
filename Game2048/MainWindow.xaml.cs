using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Game2048
{
    ///<summary>
    ///Interaction logic for MainWindow.xaml
    ///</summary>
    public partial class MainWindow : Window
    {
        #region theme toggle
        enum Theme
        {
            Light,
            Dark
        }

        private static readonly ResourceDictionary lightThemeDict = [];
        private static readonly ResourceDictionary darkThemeDict = [];

        static void LoadThemes()
        {
            lightThemeDict.Source = new Uri("LightTheme.xaml", UriKind.Relative);
            darkThemeDict.Source = new Uri("DarkTheme.xaml", UriKind.Relative);
        }

        static void SetTheme(Theme theme)
        {
            var resourceDictionary = theme == Theme.Light ? lightThemeDict : darkThemeDict;
            //清除现有资源并加载新的资源
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (ThemeComboBox.SelectedIndex)
            {
                case 0:
                    SetTheme(Theme.Light);
                    break;

                case 1:
                    SetTheme(Theme.Dark);
                    break;

                case 2:
                    using (var registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        if (registryKey != null && registryKey.GetValue("AppsUseLightTheme") is int value)
                        {
                            switch (value) //0 = Dark mode, 1 = Light mode
                            {
                                case 0:
                                    SetTheme(Theme.Dark);
                                    break;

                                case 1:
                                    SetTheme(Theme.Light);
                                    break;
                            }
                        }
                    }
                    break;
            }
        }
        #endregion

        #region textbox
        private void NumberTextBoxs_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down ||
                e.Key == Key.Home || e.Key == Key.End)
            {
                e.Handled = true;
            }
        }

        private void AddThresholdBut_Click(object sender, RoutedEventArgs e)
        {
            ApplyNumberTextBoxInput(ThresholdTextBox, minGameThreshold, maxGameThreshold, 2);
        }

        private void DecreaseThresholdBut_Click(object sender, RoutedEventArgs e)
        {
            ApplyNumberTextBoxInput(ThresholdTextBox, minGameThreshold, maxGameThreshold, 0.5f);
        }

        private void AddGridSizeBut_Click(object sender, RoutedEventArgs e)
        {
            ApplyNumberTextBoxInput(RowTextBox, minRow, maxRow, 1, 1);
            ApplyNumberTextBoxInput(ColTextBox, minCol, maxCol, 1, 1);
        }

        private void DecreaseGridSizeBut_Click(object sender, RoutedEventArgs e)
        {
            ApplyNumberTextBoxInput(RowTextBox, minRow, maxRow, 1, -1);
            ApplyNumberTextBoxInput(ColTextBox, minCol, maxCol, 1, -1);
        }

        /// <summary>
        /// 更正数字文本框格式并设置值。
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="set"></param>
        static void ApplyNumberTextBoxInput(TextBox textBox, int min, int max, ref int set)
        {
            //移除空白字符并限定范围
            string cleanedText = SpaceRegex().Replace(textBox.Text, "");
            int value = string.IsNullOrEmpty(cleanedText) ? min : Math.Clamp(int.Parse(cleanedText), min, max);
            textBox.Text = value.ToString();
            set = value;
        }

        /// <summary>
        /// 更正数字文本框格式。
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        static void ApplyNumberTextBoxInput(TextBox textBox, int min, int max, float multiple = 1, int offset = 0)
        {
            //移除空白字符并限定范围
            string cleanedText = SpaceRegex().Replace(textBox.Text, "");
            int value = string.IsNullOrEmpty(cleanedText) ? min : (int)Math.Clamp(int.Parse(cleanedText) * multiple + offset, min, max);
            textBox.Text = value.ToString();
        }

        private void NumberTextBoxs_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ColTextBox.Text != oriColText || RowTextBox.Text != oriRowText || ThresholdTextBox.Text != oriThresholdText)
            {
                TipTextBlock.Text = "修改临界值或格子大小时，重置游戏生效";
            }
            else
            {
                TipTextBlock.Text = "";
            }
        }

        private void NumberTextBoxs_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _); //只允许输入数字
        }

        string oriColText;
        string oriRowText;
        string oriThresholdText;
        #endregion

        #region theme color
        void InitializeColorPanel()
        {
            foreach (var x in ColorPanel.Children)
            {
                if (x is EleCho.WpfSuite.Controls.Button button)
                {
                    Color color = BrushToColor(button.Background);
                    button.HoverBackground = new SolidColorBrush(GetDarkerColor(color, 0.95));
                    button.PressedBackground = new SolidColorBrush(GetDarkerColor(color, 0.9));
                }
            }
        }

        static Color BrushToColor(Brush brush)
        {
            return ((SolidColorBrush)brush).Color;
        }

        //ColorPanel 中的按钮
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button but = (Button)sender;
            Color color = BrushToColor(but.Background);
            SetUIColor(color);
        }

        private void ColorTestButton_Click(object sender, RoutedEventArgs e)
        {
            ResetBut.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            int pow = 1;
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    grid[i, j] = (int)Math.Pow(2, pow++);
                }
            }

            UpdateUI();
        }

        /// <summary>
        /// 根据单元格值计算颜色。
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        static SolidColorBrush GetGradientBrush(int num)
        {
            if (num > gameThreshold)
                return new SolidColorBrush(blackColor);

            double ratio = Math.Log2(num) / Math.Log2(gameThreshold);

            byte r = (byte)(lightColor.R + (darkColor.R - lightColor.R) * ratio);
            byte g = (byte)(lightColor.G + (darkColor.G - lightColor.G) * ratio);
            byte b = (byte)(lightColor.B + (darkColor.B - lightColor.B) * ratio);

            var gradientColor = Color.FromArgb(255, r, g, b);
            return new SolidColorBrush(gradientColor);
        }

        void SetUIColor(Color color)
        {
            lightColor = color;
            darkColor = GetDarkerColor(lightColor, 0.32);

            EleCho.WpfSuite.Controls.Button[] buttons = [ResetBut, UndoBut, AutoBut];
            buttons.ToList().ForEach(x =>
            {
                x.Background = new SolidColorBrush(GetDarkerColor(lightColor, 1));
                x.HoverBackground = new SolidColorBrush(GetDarkerColor(lightColor, 0.95));
                x.PressedBackground = new SolidColorBrush(GetDarkerColor(lightColor, 0.9));
            });
            TitleBorder.Background = GetGradientBrush(1);

            UpdateUI();
        }

        static Color GetDarkerColor(Color lightColor, double factor = 0.7)
        {
            //限制 factor 在合理范围 [0, 1]
            factor = Math.Clamp(factor, 0, 1);

            //计算暗色
            byte r = (byte)(lightColor.R * factor);
            byte g = (byte)(lightColor.G * factor);
            byte b = (byte)(lightColor.B * factor);

            //返回新的颜色
            return Color.FromArgb(lightColor.A, r, g, b);
        }
        #endregion

        #region main button
        //重置游戏并应用网格大小和临界值
        private void ResetBut_Click(object sender, RoutedEventArgs e)
        {
            StopAutoPlay();

            oriColText = ColTextBox.Text;
            oriRowText = RowTextBox.Text;
            oriThresholdText = ThresholdTextBox.Text;
            ApplyNumberTextBoxInput(ColTextBox, minCol, maxCol, ref originalCol);
            ApplyNumberTextBoxInput(RowTextBox, minRow, maxRow, ref originalRow);
            ApplyNumberTextBoxInput(ThresholdTextBox, minGameThreshold, maxGameThreshold, ref gameThreshold);
            TipTextBlock.Text = "";

            step = 0;
            gameOverCount = 0;

            oldGridList.Clear(); //一并重置历史记录
            SetupGrid(originalRow, originalCol); //恢复网格大小
            InitializeGame();
        }

        private void UndoBut_Click(object sender, RoutedEventArgs e)
        {
            _ = oldGridList.TryPop(out int[,]? x);
            if (x != null && x is int[,] value)
            {
                if (value.Length != grid.Length) SetupGrid(row - 1, col - 1);

                grid = value;
                UpdateUI();
            }
        }

        private void AutoBut_Click(object sender, RoutedEventArgs e)
        {
            if (IsGameOver()) return;

            switch (AutoPlayButBlock.Text)
            {
                case "自动":
                    StartAutoPlay();
                    break;

                case "停止":
                    StopAutoPlay();
                    break;
            }
        }
        #endregion

        #region autoplay
        CancellationTokenSource cts = new();

        /// <summary>
        /// 开始自动游玩。
        /// </summary>
        void StartAutoPlay()
        {
            AutoPlayButBlock.Text = "停止";
            AutoPlayButIcon.Source = (ImageSource)FindResource("Icon_Autopause");
            cts = new CancellationTokenSource();
            _ = RunAutoTask(cts.Token);
        }

        /// <summary>
        /// 结束自动游玩。
        /// </summary>
        void StopAutoPlay()
        {
            AutoPlayButBlock.Text = "自动";
            AutoPlayButIcon.Source = (ImageSource)FindResource("Icon_Autoplay");
            cts.Cancel();
        }

        /// <summary>
        /// 自动游玩主方法。
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        async Task RunAutoTask(CancellationToken cts)
        {
            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(autoPlayInterval, cts);

                oldGridList.Push((int[,])grid.Clone());
                bool moved = random.Next(4) switch
                {
                    0 => Move(0, -1),  //up
                    1 => Move(0, 1),   //down
                    2 => Move(-1, 0),  //left
                    3 => Move(1, 0),   //right
                    _ => false
                };

                if (moved)
                {
                    AddRandomNum();
                    UpdateUI();

                    if (IsGameOver()) //游戏失败，停止运行
                    {
                        if (IsAutoPlayStrength)
                        {
                            for (int i = 1; i <= 10; i++) UndoBut.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            continue;
                        }
                        else
                        {
                            StopAutoPlay();
                            return;
                        }
                    }
                }
                else
                {
                    oldGridList.Pop();
                }
            }
        }
        #endregion

        #region app config
        private static bool TryParseGrid(string? s, int row, int col, out int[,] @out)
        {
            @out = new int[0, 0];

            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            var grid = new int[row, col];
            var numbers = s.Split(' ');

            if (numbers.Length != row * col)
            {
                return false;
            }

            int index = 0;

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    if (int.TryParse(numbers[index++], out int n))
                    {
                        grid[i, j] = n;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            @out = grid;
            return true;

        }

        private static bool TryParseGridList(string? s, int row, int col, out Stack<int[,]> @out)
        {
            @out = new Stack<int[,]>();

            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            string[] grids = s.Split(',');

            foreach (string gridStr in grids.Reverse())
            {
                if (TryParseGrid(gridStr.Trim(), row, col, out int[,] grid))
                {
                    @out.Push(grid);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        Config ParseFromText()
        {
            SaveSection saveSection = new();
            SettingSection settingSection = new();

            if (int.TryParse(App.Config["Setting:OriginalRow"], out int oriRow)) settingSection.OriginalRow = oriRow;
            if (int.TryParse(App.Config["Setting:OriginalCol"], out int oriCol)) settingSection.OriginalCol = oriCol;
            if (int.TryParse(App.Config["Setting:Row"], out int newRow)) settingSection.Row = newRow;
            if (int.TryParse(App.Config["Setting:Col"], out int newCol)) settingSection.Col = newCol;
            if (int.TryParse(App.Config["Setting:Threshold"], out int newThreshold)) settingSection.Threshold = newThreshold;
            if (bool.TryParse(App.Config["Setting:IsAutoPlayStrength"], out bool b1)) settingSection.IsAutoPlayStrength = b1;
            if (bool.TryParse(App.Config["Setting:IsExpandOnThreshold"], out bool b2)) settingSection.IsExpandOnThreshold = b2;
            if (int.TryParse(App.Config["Setting:Theme"], out int theme)) settingSection.Theme = theme;
            try
            {
                Color? themeColor = (Color)ColorConverter.ConvertFromString(App.Config["Setting:Color"]);
                settingSection.Color = themeColor;
            }
            catch (Exception)
            {
                settingSection.Color = null;
            }

            if (TryParseGridList(App.Config["Save:GridList"], (int)settingSection.Row, (int)settingSection.Col, out Stack<int[,]> l))
            {
                saveSection.GridList = l;
            }

            if (TryParseGrid(App.Config["Save:Grid"], (int)settingSection.Row, (int)settingSection.Col, out int[,] g))
            {
                saveSection.Grid = (int[,])g.Clone();
            }

            return new Config(saveSection, settingSection);
        }
        public static void SaveToBinary(Config config, string filePath)
        {
            // 将对象序列化为 JSON 字符串
            string json = JsonSerializer.Serialize(config);

            // 保存为二进制文件
            File.WriteAllBytes(filePath, System.Text.Encoding.UTF8.GetBytes(json));
        }

        public static Config LoadFromBinary(string filePath)
        {
            // 从文件中读取二进制数据并转换为 JSON 字符串
            string json = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(filePath));

            // 反序列化为对象
            return JsonSerializer.Deserialize<Config>(json);
        }
        bool LoadConfig()
        {
            if (App.Config == null || !App.Config.GetChildren().Any())
            {
                return false;
            }

            if (int.TryParse(App.Config["Setting:OriginalRow"], out int oriRow)) originalRow = oriRow;
            if (int.TryParse(App.Config["Setting:OriginalCol"], out int oriCol)) originalCol = oriCol;
            if (int.TryParse(App.Config["Setting:Row"], out int newRow)) row = newRow;
            if (int.TryParse(App.Config["Setting:Col"], out int newCol)) col = newCol;
            if (int.TryParse(App.Config["Setting:Threshold"], out int newThreshold)) gameThreshold = newThreshold;
            if (bool.TryParse(App.Config["Setting:IsAutoPlayStrength"], out bool b1)) AutoPlayStrengthCheckBox.IsChecked = b1;
            if (bool.TryParse(App.Config["Setting:IsExpandOnThreshold"], out bool b2)) ExpandOnThresholdCheckBox.IsChecked = b2;
            if (int.TryParse(App.Config["Setting:Theme"], out int theme)) ThemeComboBox.SelectedIndex = theme;
            try
            {
                Color themeColor = (Color)ColorConverter.ConvertFromString(App.Config["Setting:Color"]);
                SetUIColor(themeColor);
            }
            catch (Exception) { }

            if (TryParseGridList(App.Config["Save:GridList"], row, col, out Stack<int[,]> l))
            {
                oldGridList = l;
            }

            if (TryParseGrid(App.Config["Save:Grid"], row, col, out int[,] g))
            {
                grid = (int[,])g.Clone();
                UpdateUI();
            }
            else
            {
                return false;
            }

            SaveToBinary(ParseFromText(), Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\1.dat");

            return true;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            using var sw = new StreamWriter("appsetting.ini");
            string currentGrid = string.Join(' ', grid.Cast<int>());
            string gridList = string.Join(", ", oldGridList.Select(grid => string.Join(" ", grid.Cast<int>())));

            sw.WriteLine("[Save]");
            sw.WriteLine($"Grid = {currentGrid}");
            sw.WriteLine($"GridList = {gridList}");

            sw.WriteLine();

            sw.WriteLine("[Setting]");
            sw.WriteLine($"Theme = {ThemeComboBox.SelectedIndex}");
            sw.WriteLine($"IsAutoPlayStrength = {IsAutoPlayStrength}");
            sw.WriteLine($"IsExpandOnThreshold = {IsExpandOnThreshold}");
            sw.WriteLine($"Threshold = {gameThreshold}");
            sw.WriteLine($"Row = {row}");
            sw.WriteLine($"Col = {col}");
            sw.WriteLine($"OriginalRow = {originalRow}");
            sw.WriteLine($"OriginalCol = {originalCol}");
            sw.WriteLine($"Color = {lightColor}");
        }
        #endregion

        #region field
        private const int minRow = 1; //最小行数
        private const int minCol = 1; //最小列数
        private const int maxRow = 20; //最大行数
        private const int maxCol = 20; //最大列数
        private static int originalRow = 4;
        private static int originalCol = 4;
        private static int row = originalRow;
        private static int col = originalCol;

        private const int minGameThreshold = 4; //最小临界值
        private const int maxGameThreshold = 1073741824; //最大临界值
        private static int gameThreshold = 2048;

        private int[,] grid = new int[row, col];
        private readonly Random random = new();
        private Stack<int[,]> oldGridList = []; //历史记录

        private const int autoPlayInterval = 1;

        private static Color lightColor = Color.FromArgb(255, 255, 182, 193);
        private static Color darkColor = Color.FromArgb(255, 87, 49, 69);
        private static Color blackColor = Color.FromArgb(255, 38, 38, 38);

        private bool IsAutoPlayStrength => AutoPlayStrengthCheckBox.IsChecked ?? false;
        private bool IsExpandOnThreshold => ExpandOnThresholdCheckBox.IsChecked ?? false;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            InitializeColorPanel();
            LoadThemes();

            bool loadconfiged = LoadConfig();

            SetupGrid(row, col, true); //true表示不会进行网格扩容

            if (!loadconfiged)
            {
                grid = new int[row, col];
                InitializeGame();
            }
            else
            {
                //Mes
            }

            oriColText = originalCol.ToString();
            oriRowText = originalRow.ToString();
            oriThresholdText = gameThreshold.ToString();

            ColTextBox.Text = originalCol.ToString();
            RowTextBox.Text = originalRow.ToString();
            ThresholdTextBox.Text = gameThreshold.ToString();

            PreviewKeyDown += MainWindow_PreviewKeyDown;

            Closed += MainWindow_Closed;

            AutoPlayStrengthCheckBox.Click += (sender, e) => { UpdateScoreDetail(); }; //刷新显示底栏
        }

        /// <summary>
        /// 所有格子设置为0，并添加2和4。
        /// </summary>
        void InitializeGame()
        {
            for (int i = 0; i < row; i++)
                for (int j = 0; j < col; j++)
                    grid[i, j] = 0;

            AddRandomNum(2);
            AddRandomNum(4);

            UpdateUI();
        }

        /// <summary>
        /// 初始化网格行数列数。
        /// </summary>
        void SetupGrid(int newRow, int newCol, bool isInitialSetup = false)
        {
            if (!isInitialSetup) //首次启动时不执行
            {
                int[,] newGrid = new int[newRow, newCol];
                for (int i = 0; i < Math.Min(row, newRow); i++)
                    for (int j = 0; j < Math.Min(col, newCol); j++)
                        newGrid[i, j] = grid[i, j];

                grid = (int[,])newGrid.Clone();

                col = newCol;
                row = newRow;

                GameGrid.RowDefinitions.Clear();
                GameGrid.ColumnDefinitions.Clear();
            }

            for (int i = 0; i < newRow; i++)
            {
                GameGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            for (int i = 0; i < newCol; i++)
            {
                GameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            oldGridList.Push((int[,])grid.Clone());

            bool moved = e.Key switch
            {
                Key.Up or Key.W => Move(0, -1),
                Key.Down or Key.S => Move(0, 1),
                Key.Left or Key.A => Move(-1, 0),
                Key.Right or Key.D => Move(1, 0),
                _ => false
            };

            if (moved)
            {
                AddRandomNum();
                UpdateUI();
            }
            else
            {
                oldGridList.Pop(); //移动失败，出栈
            }
        }

        /// <summary>
        /// 根据 <see cref="IsGameOver"/> 更新标题。
        /// </summary>
        void UpdateTitle()
        {
            int score = CalculateScore();
            string scoreText = score < 0 ? "无法计算分数" : score.ToString();

            if (IsGameOver())
            {
                ScoreTitle.Text = "Game Over";
                ScoreTitle.FontSize = 20;

                ScoreContent.Text = $"Score: {scoreText}";
                ScoreContent.Visibility = Visibility.Visible;
            }
            else
            {
                ScoreTitle.Text = scoreText;
                ScoreTitle.FontSize = 24;

                ScoreContent.Visibility = Visibility.Collapsed;
            }

            UpdateScoreDetail();
        }

        /// <summary>
        /// 更新底栏。
        /// </summary>
        void UpdateScoreDetail()
        {
            ScoreDetailBlock.Text = $"步数: {step}";
            if (IsAutoPlayStrength) ScoreDetailBlock.Text += $", 自动游玩失败次数: {gameOverCount}";
            ScoreDetailBlock.Text += $", 已合成: {grid.Cast<int>().Max()}";
        }

        //定义移动函数，接受水平和垂直方向的偏移量作为参数
        bool Move(int dx, int dy)
        {
            // 标记是否发生了移动
            bool moved = false;
            // 标记哪些格子已经合并过
            bool[,] merged = new bool[row, col];

            // 根据垂直方向偏移量确定起始行和结束行，以及行的迭代步长
            int startRow = dy > 0 ? row - 1 : 0;
            int endRow = dy > 0 ? -1 : row;
            int stepRow = dy > 0 ? -1 : 1;

            // 根据水平方向偏移量确定起始列和结束列，以及列的迭代步长
            int startCol = dx > 0 ? col - 1 : 0;
            int endCol = dx > 0 ? -1 : col;
            int stepCol = dx > 0 ? -1 : 1;

            // 开始遍历行
            for (int i = startRow; i != endRow; i += stepRow)
            {
                // 开始遍历列
                for (int j = startCol; j != endCol; j += stepCol)
                {
                    // 如果当前格子的值为 0，跳过该格子
                    if (grid[i, j] == 0) continue;

                    int x = i, y = j;
                    // 只要目标位置有效且为 0，将当前格子的值移动到目标位置
                    while (IsValid(x + dy, y + dx) && grid[x + dy, y + dx] == 0)
                    {
                        grid[x + dy, y + dx] = grid[x, y];
                        grid[x, y] = 0;
                        x += dy;
                        y += dx;
                        moved = true;
                    }

                    // 如果目标位置有效，且与当前格子的值相等，且目标位置未合并过，则合并
                    if (IsValid(x + dy, y + dx) && grid[x + dy, y + dx] == grid[x, y] && !merged[x + dy, y + dx])
                    {
                        grid[x + dy, y + dx] = grid[x, y] * 2;
                        grid[x, y] = 0;
                        merged[x + dy, y + dx] = true;
                        moved = true;
                    }
                }
            }

            if (moved)
            {
                step++;
            }
            // 返回是否发生了移动
            return moved;
        }

        int step = 0;
        int gameOverCount = 0;

        /// <summary>
        /// 检查位置是否合法。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static bool IsValid(int x, int y) => x >= 0 && x < row && y >= 0 && y < col;

        /// <summary>
        /// 检查是否游戏结束。
        /// </summary>
        /// <returns></returns>
        bool IsGameOver()
        {
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    if (grid[i, j] == 0) return false; //存在空位，未结束

                    if (IsValid(i + 1, j) && grid[i + 1, j] == grid[i, j]) return false; //可垂直合并
                    if (IsValid(i, j + 1) && grid[i, j + 1] == grid[i, j]) return false; //可水平合并
                }
            }

            if (IsAutoPlayStrength) gameOverCount++;
            return true; //无空位或可合并，游戏结束
        }

        /// <summary>
        /// 按概率添加2或4到网格（不更新UI）
        /// </summary>
        /// <param name="num">指定一个数字</param>
        void AddRandomNum(int num = -1)
        {
            List<(int, int)> emptyCells = []; //统计空白格
            for (int i = 0; i < row; i++)
                for (int j = 0; j < col; j++)
                    if (grid[i, j] == 0)
                        emptyCells.Add((i, j));

            if (emptyCells.Count > 0)
            {
                var (row, col) = emptyCells[random.Next(emptyCells.Count)]; //随机挑选一个空白格
                grid[row, col] = num == -1 ? (random.Next(0, 10) < 9 ? 2 : 4) : num;
            }
        }

        /// <summary>
        /// 更新UI时一并更新标题。
        /// </summary>
        void UpdateUI()
        {
            if (IsExpandOnThreshold)
            {
                int maxLog2 = (int)Math.Log2(grid.Cast<int>().Max());
                int thresholdLog2 = (int)Math.Log2(gameThreshold);
                if (maxLog2 >= row - originalRow + thresholdLog2 && maxLog2 >= thresholdLog2)
                {
                    SetupGrid(row + 1, col + 1);
                    MessageBox.Show($"网格已被扩容为{col}x{row}，突破你的极限！");
                }
            }
            GameGrid.Children.Clear();

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    int num = grid[i, j];
                    if (num != 0)
                    {
                        TextBlock block = new()
                        {
                            Text = num.ToString(),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = Brushes.White,
                            FontSize = 24
                        };
                        Border border = new()
                        {
                            Background = GetGradientBrush(num),
                            CornerRadius = new CornerRadius(10),
                            Margin = new Thickness(2),
                            Child = block
                        };

                        Grid.SetRow(border, i);
                        Grid.SetColumn(border, j);

                        GameGrid.Children.Add(border);

                        border.SizeChanged += (sender, e) =>
                        {
                            //创建 FormattedText 对象
                            FormattedText formattedText = new(
                                block.Text,
                                System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                new Typeface(block.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                                block.FontSize,
                                Brushes.Black,
                                1.0);
                            double textWidth = formattedText.Width;
                            double borderWidth = border.ActualWidth;

                            if (textWidth > borderWidth)
                            {
                                block.FontSize = 24 * (borderWidth / formattedText.Width * 0.8);
                            }
                        };
                    }
                }
            }

            UpdateTitle();
        }

        /// <summary>
        /// 计算分数，若有单元格溢出则返回-1。
        /// </summary>
        /// <returns>所有格子的总和-6。</returns>
        int CalculateScore()
        {
            int sum = -6;
            foreach (int x in grid)
            {
                if (x >= int.MaxValue || x <= int.MinValue || x < 0) return -1;
                sum += x;
            }
            return sum;
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex SpaceRegex();

        private void GithubBut_Click(object sender, RoutedEventArgs e)
        {
            string link = (string)FindResource("GithubLink");
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }

        bool toggle = false;
        private async void SettingBut_Click(object sender, RoutedEventArgs e)
        {
            //switch (MainRight.Visibility)
            //{
            //    case Visibility.Visible:
            //        MainRight.Visibility = Visibility.Collapsed;

            //        RightColumnD.Width = new GridLength(0);
            //        Width = MainLeft.ActualWidth + 16;
            //        break;

            //    case Visibility.Collapsed:
            //        MainRight.Visibility = Visibility.Visible;

            //        RightColumnD.Width = new GridLength(270);
            //        Width = MainLeft.ActualWidth + 16 + 270;
            //        break;
            //}
            toggle = !toggle;
            double leftColumnDWidth = MainLeft.ActualWidth;
            double rightColumnDWidth = 270;

            switch (toggle)
            {
                case true:
                    LeftColumnD.Width = new GridLength(leftColumnDWidth); //锁定左面板大小

                    DoubleAnimation widthAnim = new()
                    {
                        From = Width,
                        To = leftColumnDWidth + 16,
                        Duration = TimeSpan.FromMilliseconds(1000),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    DoubleAnimation opacityAnim = new()
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(400),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    widthAnim.Completed += (a, b) =>
                    {
                        RightColumnD.Width = new GridLength(0);                   //隐藏右面板
                        LeftColumnD.Width = new GridLength(1, GridUnitType.Star); //解除锁定左面板大小
                    };

                    BeginAnimation(WidthProperty, widthAnim);
                    MainRight.BeginAnimation(OpacityProperty, opacityAnim);
                    break;

                case false:
                    LeftColumnD.Width = new GridLength(leftColumnDWidth);          //锁定左面板大小
                    RightColumnD.Width = new GridLength(rightColumnDWidth);        //显示右面板
                    DoubleAnimation widthAnim2 = new()
                    {
                        From = Width,
                        To = leftColumnDWidth + 16 + rightColumnDWidth,
                        Duration = TimeSpan.FromMilliseconds(1000),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    DoubleAnimation opacityAnim2 = new()
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(400),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    widthAnim2.Completed += (a, b) =>
                    {

                        LeftColumnD.Width = new GridLength(1, GridUnitType.Star); //解除锁定左面板大小
                    };

                    BeginAnimation(WidthProperty, widthAnim2);
                    await Task.Delay(400);
                    MainRight.BeginAnimation(OpacityProperty, opacityAnim2);
                    break;
            }
        }
    }
}