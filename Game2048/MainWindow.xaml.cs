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
        public MainWindow()
        {
            InitializeComponent();

            InitializeGame();

            KeyDown += MainWindow_KeyDown;

            resetBut.Click += ResetBut_Click;
            undoBut.Click += UndoBut_Click;
            autoBut.Click += AutoBut_Click;
        }

        private int[,] grid = new int[4, 4];
        private readonly Random random = new();
        private readonly Stack<int[,]> oldGridList = []; //历史记录

        /// <summary>
        /// 所有格子设置为0，并添加2和4。
        /// </summary>
        private void InitializeGame()
        {
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    grid[i, j] = 0;

            AddRandomNum(2);
            AddRandomNum(4);
            UpdateUI();
        }

        private void ResetBut_Click(object sender, RoutedEventArgs e)
        {
            StopAutoPlay();

            oldGridList.Clear(); //一并重置历史记录
            InitializeGame();
        }

        private void UndoBut_Click(object sender, RoutedEventArgs e)
        {
            _ = oldGridList.TryPop(out int[,]? u);
            if (u != null)
            {
                grid = (int[,])u.Clone();
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

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            oldGridList.Push((int[,])grid.Clone());

            bool moved = e.Key switch
            {
                Key.Up => Move(0, -1),
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

        private CancellationTokenSource cts = new();

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
                await Task.Delay(10, cts);

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
                        autoBut.Content = "自动";

                        for (int i = 1; i <= 10; i++)
                            undoBut.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                        autoBut.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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
            bool[,] merged = new bool[4, 4];

            // 根据垂直方向偏移量确定起始行和结束行，以及行的迭代步长
            int startRow = dy > 0 ? 3 : 0;
            int endRow = dy > 0 ? -1 : 4;
            int stepRow = dy > 0 ? -1 : 1;

            // 根据水平方向偏移量确定起始列和结束列，以及列的迭代步长
            int startCol = dx > 0 ? 3 : 0;
            int endCol = dx > 0 ? -1 : 4;
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
        static bool IsValid(int x, int y) => x >= 0 && x < 4 && y >= 0 && y < 4;

        /// <summary>
        /// 检查是否游戏结束。
        /// </summary>
        /// <returns></returns>
        bool IsGameOver()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
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
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
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
            GameGrid.Children.Clear();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int num = grid[i, j];
                    if (num != 0)
                    {
                        TextBlock block = new()
                        {
                            Text = num == 0 ? "" : num.ToString(),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 24,
                            Foreground = Brushes.White
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

                        scoreTitle.Text = CalculateScore().ToString();
                        UpdateTitle();
                    }
                }
            }
        }

        /// <summary>
        /// 计算分数。
        /// </summary>
        /// <returns>所有格子的总和-6.</returns>
        int CalculateScore()
        {
            int score = -6;
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    score += grid[i, j];
            return score;
        }

        /// <summary>
        /// 根据单元格值计算颜色。
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        static SolidColorBrush GetGradientBrush(int num)
        {
            var lightPink = Color.FromArgb(255, 255, 182, 193); //浅粉色
            var deepPink = Color.FromArgb(255, 87, 49, 69);  //深粉色

            if (num > 2048)
                return new SolidColorBrush(Color.FromArgb(255, 38, 38, 38));

            double ratio = Math.Log2(num) / Math.Log2(2048);

            byte r = (byte)(lightPink.R + (deepPink.R - lightPink.R) * ratio);
            byte g = (byte)(lightPink.G + (deepPink.G - lightPink.G) * ratio);
            byte b = (byte)(lightPink.B + (deepPink.B - lightPink.B) * ratio);

            var gradientColor = Color.FromArgb(255, r, g, b);
            return new SolidColorBrush(gradientColor);
        }

    }
}