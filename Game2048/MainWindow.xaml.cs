
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
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

        private void AddAutoPlayIntervalBut_Click(object sender, RoutedEventArgs e)
        {
            ApplyNumberTextBoxInput(AutoPlayIntervalTextBox, minAutoPlayInterval, maxAutoPlayInterval, ref autoPlayInterval, 1, 50);
        }

        private void DecreaseAutoPlayIntervalBut_Click(object sender, RoutedEventArgs e)
        {
            ApplyNumberTextBoxInput(AutoPlayIntervalTextBox, minAutoPlayInterval, maxAutoPlayInterval, ref autoPlayInterval, 1, -50);
        }

        private void AutoPlayIntervalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyNumberTextBoxInput(AutoPlayIntervalTextBox, minAutoPlayInterval, maxAutoPlayInterval, ref autoPlayInterval);
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
        static void ApplyNumberTextBoxInput(TextBox textBox, int min, int max, ref int set, float multiple = 1, int offset = 0)
        {
            //移除空白字符并限定范围
            string cleanedText = SpaceRegex().Replace(textBox.Text, "");
            int value = string.IsNullOrEmpty(cleanedText) ? min : (int)Math.Clamp(int.Parse(cleanedText) * multiple + offset, min, max);
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
                oldGridList.Push((int[,])grid.Clone());

                OptimalDirection[] optimalDirections = GetOptimalDirections();
                OptimalDirection optimalDirection = optimalDirections[random.Next(optimalDirections.Length)];

                Debug.WriteLine(string.Join(" or ", optimalDirections));

                (int dx, int dy) = optimalDirection switch
                {
                    OptimalDirection.Up => (0, -1),     //up
                    OptimalDirection.Down => (0, 1),    //down
                    OptimalDirection.Left => (-1, 0),   //left
                    OptimalDirection.Right => (1, 0),   //right
                    _ => (0, 0)
                };
                bool moved = Move(ref grid, dx, dy);

                if (moved)
                {
                    step++;

                    AddRandomNum();
                    UpdateUI();

                    if (IsGameOver()) //游戏失败，停止运行
                    {
                        StopAutoPlay();
                        return;
                    }
                    await Task.Delay(autoPlayInterval, cts);
                }
                else
                {
                    oldGridList.Pop();
                }
            }
        }
        #endregion

        #region app config
        Config GetConfig()
        {
            SaveSection? saveSection = IsLoadSave ? new(
                grid,
                oldGridList,
                step) : null;

            SettingSection settingSection = new(
                originalRow,
                originalCol,
                row,
                col,
                gameThreshold,
                autoPlayInterval,
                IsExpandOnThreshold,
                ThemeComboBox.SelectedIndex,
                ColorUtil.ColorToString(lightColor),
                IsLoadSave);

            return new Config(saveSection, settingSection);
        }
        bool LoadConfig()
        {
            if (App.Config == null) return false;

            SettingSection settingSection = App.Config.Setting;
            originalRow = settingSection.OriginalRow;
            originalCol = settingSection.OriginalCol;
            row = settingSection.Row;
            col = settingSection.Col;
            gameThreshold = settingSection.Threshold;
            autoPlayInterval = settingSection.AutoPlayInterval;
            ExpandOnThresholdCheckBox.IsChecked = settingSection.IsExpandOnThreshold;
            ThemeComboBox.SelectedIndex = settingSection.Theme;
            lightColor = ColorUtil.StringToColor(settingSection.Color);
            LoadSaveCheckBox.IsChecked = settingSection.LoadSave;

            SaveSection? saveSection = App.Config.Save;
            if (saveSection != null)
            {
                grid = saveSection.Grid;
                oldGridList = saveSection.GridList;
                step = saveSection.Step;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            App.SaveToBinary(GetConfig(), App.ConfigPath);
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Game2048.config"), GetConfig().ToString());
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

        private static int autoPlayInterval = 200;
        private const int minAutoPlayInterval = 1;
        private const int maxAutoPlayInterval = 1000;

        private static Color lightColor = Color.FromArgb(255, 255, 182, 193);
        private static Color darkColor = Color.FromArgb(255, 87, 49, 69);
        private static Color blackColor = Color.FromArgb(255, 38, 38, 38);

        private bool IsExpandOnThreshold => ExpandOnThresholdCheckBox.IsChecked ?? false;
        private bool IsLoadSave => LoadSaveCheckBox.IsChecked ?? false;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Left = SystemParameters.PrimaryScreenWidth / 2 - Width;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;

            InitializeColorPanel();
            LoadThemes();

            bool isLoad = LoadConfig();
            if (isLoad)
            {
                SetupGrid(row, col, false);
                SetUIColor(lightColor);
            }
            else
            {
                SetupGrid(row, col, true);
                grid = new int[row, col];
                InitializeGame();
            }

            oriColText = originalCol.ToString();
            oriRowText = originalRow.ToString();
            oriThresholdText = gameThreshold.ToString();

            ColTextBox.Text = originalCol.ToString();
            RowTextBox.Text = originalRow.ToString();
            ThresholdTextBox.Text = gameThreshold.ToString();
            AutoPlayIntervalTextBox.Text = autoPlayInterval.ToString();

            PreviewKeyDown += MainWindow_PreviewKeyDown;
            Closed += MainWindow_Closed;

            //Loaded += MainWindow_Loaded;
            AutoPlayIntervalTextBox.TextChanged += AutoPlayIntervalTextBox_TextChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            toggle = true;
            Width = MainLeft.ActualWidth;
            RightColumnD.Width = new GridLength(0);
            MainRight.Opacity = 0;
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
            if (e.Key == Key.Z)
            {
                UndoBut.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }

            oldGridList.Push((int[,])grid.Clone());

            bool moved = e.Key switch
            {
                Key.Up or Key.W => Move(ref grid, 0, -1),
                Key.Down or Key.S => Move(ref grid, 0, 1),
                Key.Left or Key.A => Move(ref grid, -1, 0),
                Key.Right or Key.D => Move(ref grid, 1, 0),
                _ => false
            };

            if (moved)
            {
                step++;
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
            ScoreDetailBlock.Text += $", 已合成: {grid.Cast<int>().Max()}";
        }

        //定义移动函数，接受水平和垂直方向的偏移量作为参数
        static bool Move(ref int[,] grid, int dx, int dy)
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

            // 返回是否发生了移动
            return moved;
        }

        int step = 0;

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
                        Duration = TimeSpan.FromMilliseconds(800),
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
                        Duration = TimeSpan.FromMilliseconds(800),
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

        private void TipBut_Click(object sender, RoutedEventArgs e)
        {
            _ = CalculateSituationScore(grid, true);

            MessageBox.Show(GetOptimalDirectionStr());
        }

        enum OptimalDirection
        {
            Up, Down, Left, Right
        }

        /// <summary>
        /// 模拟移动网格计算水平和竖直方向的最高分数。
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        int CalculateMoveScore(int[,] grid, int dx, int dy)
        {
            int[,] gridCopy = (int[,])grid.Clone();
            Move(ref gridCopy, dx, dy);

            (int upGridVerScore, int upGridHorScore) = CalculateSituationScore(gridCopy);

            if (gridCopy.Cast<int>().SequenceEqual(grid.Cast<int>()))
            {
                return 0;
            }
            else
            {
                return Math.Max(upGridVerScore, upGridHorScore);
            }
        }

        /// <summary>
        /// 获取最佳移动方向的分析过程。
        /// </summary>
        /// <returns></returns>
        string GetOptimalDirectionStr()
        {
            (int verScore, int horScore) = CalculateSituationScore(grid);

            double up = CalculateMoveScore(grid, 0, -1) * (verScore == 0 ? 0.9 : 1); //如果当前不可合成，则减少下一步合成分数的权重
            double down = CalculateMoveScore(grid, 0, 1) * (verScore == 0 ? 0.9 : 1);
            double left = CalculateMoveScore(grid, -1, 0) * (horScore == 0 ? 0.9 : 1);
            double right = CalculateMoveScore(grid, 1, 0) * (horScore == 0 ? 0.9 : 1);

            double upScore = verScore + up;
            double downScore = verScore + down;
            double leftScore = horScore + left;
            double rightScore = horScore + right;

            StringBuilder sb = new();
            sb.AppendLine($"Base: Hor {horScore}, Ver {verScore}");
            sb.AppendLine($"Move: Up {up}, Down {down}, Left {left}, Right {right}");
            sb.AppendLine($"Comprehensive: Up {upScore}, Down {downScore}, Left {leftScore}, Right {rightScore}");
            sb.AppendLine($"=> {string.Join(" or ", GetOptimalDirections())}");

            return sb.ToString();
        }

        /// <summary>
        /// 获取最佳移动方向。
        /// </summary>
        /// <returns></returns>
        OptimalDirection[] GetOptimalDirections()
        {
            (int verScore, int horScore) = CalculateSituationScore(grid);

            double up = CalculateMoveScore(grid, 0, -1) * (verScore == 0 ? 0.9 : 1); //如果当前不可合成，则减少下一步合成分数的权重
            double down = CalculateMoveScore(grid, 0, 1) * (verScore == 0 ? 0.9 : 1);
            double left = CalculateMoveScore(grid, -1, 0) * (horScore == 0 ? 0.9 : 1);
            double right = CalculateMoveScore(grid, 1, 0) * (horScore == 0 ? 0.9 : 1);

            double upScore = verScore + up;
            double downScore = verScore + down;
            double leftScore = horScore + left;
            double rightScore = horScore + right;

            // 存储方向和对应分数的字典
            var directionScores = new Dictionary<OptimalDirection, double>
            {
                { OptimalDirection.Up, upScore },
                { OptimalDirection.Down, downScore },
                { OptimalDirection.Left, leftScore },
                { OptimalDirection.Right, rightScore }
            };

            // 找到最大分数
            double maxScore = directionScores.Values.Max();

            // 返回所有具有最大分数的方向
            return directionScores
                .Where(kv => kv.Value == maxScore)
                .Select(kv => kv.Key)
                .ToArray();
        }

        /// <summary>
        /// 评估当前网格，返回局势分数。
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="highLight"></param>
        /// <returns></returns>
        (int, int) CalculateSituationScore(int[,] grid, bool highLight = false)
        {
            HashSet<Point> verMergeablePoints = []; //可合并
            HashSet<Point> horMergeablePoints = [];

            int verScore = 0;
            int horScore = 0;

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    if (grid[i, j] == 0) continue; // 当前格为 0，无需延申
                    if (verMergeablePoints.Contains(new Point(j, i))) continue; // 当前格如果已经可合并，无需延申

                    int distanceMax = row - i - 1; // 可延申距离的上限

                    for (int distance = 1; distance <= distanceMax; distance++) // 从 1 开始延申
                    {
                        int next = i + distance;

                        if (!IsValid(next, j)) break; // 越界检查

                        if (grid[next, j] == grid[i, j]) // 可合并
                        {
                            Point p1 = new(j, i);
                            Point p2 = new(j, next);

                            if (highLight)
                            {
                                LightGrid(p1);
                                LightGrid(p2);
                            }
                            verMergeablePoints.Add(p1);
                            verMergeablePoints.Add(p2);
                            verScore += grid[i, j];
                            verScore += grid[next, j];
                            break; // 合并后无需继续延申
                        }
                        else if (grid[next, j] != 0) // 遇到阻碍
                        {
                            break;
                        }
                        // 如果是 0，则自动继续下一轮循环
                    }
                }
            }

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    if (grid[i, j] == 0) continue; // 当前格为 0，无需延申
                    if (horMergeablePoints.Contains(new Point(j, i))) continue; // 当前格如果是被合并目标，无需延申

                    int distanceMax = col - j - 1; // 可延申距离的上限

                    for (int distance = 1; distance <= distanceMax; distance++) // 从 1 开始延申
                    {
                        int next = j + distance;

                        if (!IsValid(i, next)) break; // 越界检查

                        if (grid[i, next] == grid[i, j]) // 可合并
                        {
                            Point p1 = new(j, i);
                            Point p2 = new(next, i);

                            if (highLight)
                            {
                                LightGrid(p1);
                                LightGrid(p2);
                            }
                            horMergeablePoints.Add(p1);
                            horMergeablePoints.Add(p2);
                            horScore += grid[i, j];
                            horScore += grid[i, next];
                            break; // 合并后无需继续延申
                        }
                        else if (grid[i, next] != 0) // 遇到阻碍
                        {
                            break;
                        }
                        // 如果是 0，则自动继续下一轮循环
                    }
                }
            }

            return (verScore, horScore);
        }

        void LightGrid(Point point)
        {
            Border? border = (Border?)GameGrid.Children.Cast<UIElement>()
                .FirstOrDefault(e => Grid.GetRow(e) == point.Y && Grid.GetColumn(e) == point.X);

            if (border != null)
            {
                border.Background = Brushes.White;
                ((TextBlock)border.Child).Foreground = Brushes.Black;
                border.BorderBrush = Brushes.Black;
                border.BorderThickness = new Thickness(2);
            }
        }

    }
}