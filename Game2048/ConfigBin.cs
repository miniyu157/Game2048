using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Game2048
{

    [Serializable] // 必须标记为可序列化
    public class Config
    {
        public Config(SaveSection? save, SettingSection? setting)
        {
            Save = save;
            Setting = setting;
        }

        public SaveSection? Save { get; set; }
        public SettingSection? Setting { get; set; }
    }

    [Serializable]
    public class SaveSection
    {
        public int[,]? Grid { get; set; }
        public Stack<int[,]>? GridList { get; set; }
    }

    [Serializable]
    public class SettingSection
    {
        public int? Theme { get; set; }
        public bool? IsAutoPlayStrength { get; set; }
        public bool? IsExpandOnThreshold { get; set; }
        public int? Threshold { get; set; }
        public int? Row { get; set; }
        public int? Col { get; set; }
        public int? OriginalRow { get; set; }
        public int? OriginalCol { get; set; }
        public Color? Color { get; set; }
    }

}
