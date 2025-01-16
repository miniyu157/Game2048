using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (themeComboBox.SelectedIndex)
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

        static void SetTheme(Theme theme)
        {
            var resourceDictionary = new ResourceDictionary();
            switch (theme)
            {
                case Theme.Light:
                    resourceDictionary.Source = new Uri("LightTheme.xaml", UriKind.Relative);
                    break;
                case Theme.Dark:
                    resourceDictionary.Source = new Uri("DarkTheme.xaml", UriKind.Relative);
                    break;
            }

            //清除现有资源并加载新的资源
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
        }
        #endregion

        #region textbox

        static void NumberInput(TextBox textBox, int min, int max, ref int set)
        {
            //移除空白字符并限定范围
            string cleanedText = SpaceRegex().Replace(textBox.Text, "");

            if (!int.TryParse(cleanedText, out int value) || value < min || value > max)
            {
                value = Math.Clamp(value, min, max);
            }

            set = value;
            textBox.Text = value.ToString();
        }

        private void NumberTextBoxs_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                switch (textBox.Name)
                {
                    case "colTextBox":
                        NumberInput(colTextBox, minCol, maxCol, ref originalCol);
                        break;

                    case "rowTextBox":
                        NumberInput(rowTextBox, minRow, maxRow, ref originalRow);
                        break;

                    case "thresholdTextBox":
                        NumberInput(thresholdTextBox, minGameThreshold, int.MaxValue, ref gameThreshold);
                        break;
                }
            }
        }

        private void NumberTextBoxs_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _); //只允许输入数字
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            LightColor = Color.FromArgb(255, 193, 230, 181);
            DarkColor = Color.FromArgb(255, 56, 66, 52);
            EleCho.WpfSuite.Controls.Button[] buttons = [resetBut, undoBut, autoBut];
            buttons.ToList().ForEach(x =>
            {
                x.Background = GetGradientBrush(1);
                x.HoverBackground = GetGradientBrush(2);
                x.PressedBackground = GetGradientBrush(4);
            });
            titleBorder.Background = GetGradientBrush(1);

            SetupGrid(row, col, true);
            InitializeGame();

            PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private const int minRow = 1; //最小行数(用户输入)
        private const int minCol = 1; //最小列数(用户输入)
        private const int maxRow = 20; //最大行数(用户输入)
        private const int maxCol = 20; //最大列数(用户输入)
        private static int originalRow = 4;
        private static int originalCol = 4;
        private static int row = originalRow;
        private static int col = originalCol;

        private const int minGameThreshold = 4; //最小临界值
        private static int gameThreshold = 2048;

        private int[,] grid = new int[row, col];
        private readonly Random random = new();
        private readonly Stack<int[,]> oldGridList = []; //历史记录

        private const int autoPlayInterval = 10;

        private static Color LightColor = Color.FromArgb(255, 255, 182, 193);
        private static Color DarkColor = Color.FromArgb(255, 87, 49, 69);
        private static Color GrayColor = Color.FromArgb(255, 38, 38, 38);

        /// <summary>
        /// 所有格子设置为0，并添加2和4。
        /// </summary>
        void InitializeGame()
        {
            for (int i = 0; i < row; i++)
                for (int j = 0; j < col; j++)
                    grid[i, j] = 0;

            //AddRandomNum(2);
            //AddRandomNum(4);

            for (int i = 1; i <= grid.Length; i++)           //test colors
                AddRandomNum((int)Math.Pow(2, i));

            UpdateUI();
        }

        /// <summary>
        /// 初始化网格行数列数。
        /// </summary>
        void SetupGrid(int newRow, int newCol, bool isInitialSetup = false)
        {
            if (!isInitialSetup)
            {
                int[,] newGrid = new int[newRow, newCol];
                for (int i = 0; i < Math.Min(row, newRow); i++)
                    for (int j = 0; j < Math.Min(col, newCol); j++)
                        newGrid[i, j] = grid[i, j];

                grid = (int[,])newGrid.Clone();

                col = newCol;
                row = newRow;

                gameGrid.RowDefinitions.Clear();
                gameGrid.ColumnDefinitions.Clear();
            }

            for (int i = 0; i < newRow; i++)
            {
                gameGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            for (int i = 0; i < newCol; i++)
            {
                gameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
        }

        private void ResetBut_Click(object sender, RoutedEventArgs e)
        {
            StopAutoPlay();

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

            switch (autoBut.Content)
            {
                case "自动":
                    StartAutoPlay();
                    break;

                case "停止":
                    StopAutoPlay();
                    break;
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.W)
            {
                switch (e.Key)
                {
                    case Key.Z:
                        undoBut.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        break;
                }
                return;
            }

            oldGridList.Push((int[,])grid.Clone());

            bool moved = e.Key switch
            {
                Key.Up or Key.W => Move(0, -1),
                Key.Down => Move(0, 1),
                Key.Left => Move(-1, 0),
                Key.Right => Move(1, 0),
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

        CancellationTokenSource cts = new();

        /// <summary>
        /// 开始自动游玩。
        /// </summary>
        void StartAutoPlay()
        {
            autoBut.Content = "停止";
            cts = new CancellationTokenSource();
            _ = RunAutoTask(cts.Token);
        }

        /// <summary>
        /// 结束自动游玩。
        /// </summary>
        void StopAutoPlay()
        {
            autoBut.Content = "自动";
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
                        gameOverCount++;

                        autoBut.Content = "自动";

                        if (autoPlayStrengthCheckBox.IsChecked ?? false)
                        {
                            for (int i = 1; i <= 10; i++)
                                undoBut.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                            autoBut.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }

                        break;
                    }
                }
                else
                {
                    oldGridList.Pop();
                }
            }
        }

        /// <summary>
        /// 根据 <see cref="IsGameOver"/> 更新标题。
        /// </summary>
        void UpdateTitle()
        {
            if (IsGameOver())
            {
                scoreTitle.Text = "Game Over";
                scoreContent.Text = $"Score: {CalculateScore()}";
                scoreContent.Visibility = Visibility.Visible;
                scoreTitle.FontSize = 20;
                return;
            }
            else
            {
                scoreTitle.FontSize = 24;
                scoreContent.Visibility = Visibility.Collapsed;
            }
        }

        // 定义移动函数，接受水平和垂直方向的偏移量作为参数
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
            // 返回是否发生了移动
            return moved;
        }

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


        int setpCount = 0;
        int gameOverCount = 0;

        /// <summary>
        /// 更新UI时一并更新标题。
        /// </summary>
        void UpdateUI()
        {
            Title = $"步数: {setpCount++}, 失败计数: {gameOverCount}, 已合成: {grid.Cast<int>().Max()}";

            if (expandOnThresholdCheckBox.IsChecked ?? true)
            {
                int maxLog2 = (int)Math.Log2(grid.Cast<int>().Max());
                int thresholdLog2 = (int)Math.Log2(gameThreshold);
                if (maxLog2 >= row - originalRow + thresholdLog2 && maxLog2 >= thresholdLog2)
                {
                    SetupGrid(row + 1, col + 1);
                    MessageBox.Show($"网格已被扩容为{col}x{row}，突破你的极限！");
                }
            }
            gameGrid.Children.Clear();

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
                            FontSize = 24,
                        };
                        Border border = new()
                        {
                            Background = GetGradientBrush(num),
                            CornerRadius = new CornerRadius(10),
                            Margin = new Thickness(2),
                            Child = block
                        };

                        //// 要测量的字体
                        //string text = "Hello, World!";
                        //Typeface typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                        //double fontSize = 12;

                        //// 创建 FormattedText 对象
                        //FormattedText formattedText = new FormattedText(
                        //    text,
                        //    System.Globalization.CultureInfo.CurrentCulture,
                        //    FlowDirection.LeftToRight,
                        //    typeface,
                        //    fontSize,
                        //    Brushes.Black);

                        //// 获取字体的宽度和高度
                        //double textWidth = formattedText.Width;
                        //double textHeight = formattedText.Height;


                        Grid.SetRow(border, i);
                        Grid.SetColumn(border, j);
                        gameGrid.Children.Add(border);
                        try
                        {
                            scoreTitle.Text = CalculateScore().ToString();
                        }
                        catch (Exception)
                        {
                            scoreTitle.Text = "无法计算分数";
                        }
                        UpdateTitle();
                    }
                }
            }
        }

        /// <summary>
        /// 计算分数。
        /// </summary>
        /// <returns>所有格子的总和-6.</returns>
        int CalculateScore() => grid.Cast<int>().Sum() - 6;

        /// <summary>
        /// 根据单元格值计算颜色。
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        static SolidColorBrush GetGradientBrush(int num)
        {
            if (num > gameThreshold)
                return new SolidColorBrush(GrayColor);

            double ratio = Math.Log2(num) / Math.Log2(gameThreshold);

            byte r = (byte)(LightColor.R + (DarkColor.R - LightColor.R) * ratio);
            byte g = (byte)(LightColor.G + (DarkColor.G - LightColor.G) * ratio);
            byte b = (byte)(LightColor.B + (DarkColor.B - LightColor.B) * ratio);

            var gradientColor = Color.FromArgb(255, r, g, b);
            return new SolidColorBrush(gradientColor);
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex SpaceRegex();

    }
}