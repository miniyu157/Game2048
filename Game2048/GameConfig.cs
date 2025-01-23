using MessagePack;
using System.Windows.Media;

namespace Game2048
{
    public static class ColorUtil
    {
        public static string ColorToString(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static Color StringToColor(string colorString)
        {
            return (Color)ColorConverter.ConvertFromString(colorString);
        }
    }

    [MessagePackObject]
    public class Config(SaveSection? save, SettingSection setting)
    {
        [Key(0)]
        public SaveSection? Save { get; set; } = save;

        [Key(1)]
        public SettingSection Setting { get; set; } = setting;

        public override string ToString()
        {
            string save = Save == null ? "null" : Save.ToString();
            return $"Save={{{save}}}, Setting={{{Setting}}}";
        }
    }

    [MessagePackObject]
    public class SaveSection(int[,] grid, Stack<int[,]> gridList, int step)
    {
        [Key(0)]
        public int[,] Grid { get; set; } = grid;

        [Key(1)]
        public Stack<int[,]> GridList { get; set; } = gridList;

        [Key(2)]
        public int Step { get; set; } = step;

        public override string ToString()
        {
            string grid = Grid == null ? "null" : $"[{string.Join(",", Grid.Cast<int>())}]";
            string gridList = GridList == null ? "null" : string.Join(" ", GridList.Select(x => $"[{string.Join(',', x.Cast<int>())}]"));
            return $"Grid={grid}, GridList={gridList}, Step={Step}";
        }
    }

    [MessagePackObject]
    public class SettingSection(int originalRow, int originalCol, int row, int col, int threshold, int autoPlayInterval, bool isExpandOnThreshold, int theme, string color, bool loadSave)
    {
        [Key(0)]
        public int OriginalRow { get; set; } = originalRow;

        [Key(1)]
        public int OriginalCol { get; set; } = originalCol;

        [Key(2)]
        public int Row { get; set; } = row;

        [Key(3)]
        public int Col { get; set; } = col;

        [Key(4)]
        public int Threshold { get; set; } = threshold;

        [Key(5)]
        public int AutoPlayInterval { get; set; } = autoPlayInterval;

        [Key(6)]
        public bool IsExpandOnThreshold { get; set; } = isExpandOnThreshold;

        [Key(7)]
        public int Theme { get; set; } = theme;

        [Key(8)]
        public string Color { get; set; } = color;

        [Key(9)]
        public bool LoadSave { get; set; } = loadSave;

        public override string ToString()
        {
            return $"OriginalRow={OriginalRow}, OriginalCol={OriginalCol}, Row={Row}, Col={Col}, Threshold={Threshold}, AutoPlayInterval={AutoPlayInterval}, IsExpandOnThreshold={IsExpandOnThreshold}, Theme={Theme}, Color={Color}, LoadSave={LoadSave}";
        }
    }
}
