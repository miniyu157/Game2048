using MessagePack;
using System.IO;
using System.Text;
using System.Windows;

namespace Game2048
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string FileHeader = "KlxPiao.Game2048Config";
        private static readonly byte[] FileHeaderBytes = Encoding.UTF8.GetBytes(FileHeader);

        public static Config? Config { get; private set; }
        public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "Game2048.dat");

        public App()
        {
            Config = LoadFromBinary(ConfigPath);
        }

        public static void SaveToBinary(Config config, string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);

            writer.Write(FileHeaderBytes);    //写入文件头

            byte[] data = MessagePackSerializer.Serialize(config);
            writer.Write(data.Length);
            writer.Write(data);
        }

        public static Config? LoadFromBinary(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(stream);

                byte[] fileHeader = reader.ReadBytes(FileHeaderBytes.Length);
                if (!FileHeaderBytes.AsSpan().SequenceEqual(fileHeader)) //文件头不正确时
                {
                    return null;
                }

                int dataLength = reader.ReadInt32();
                byte[] data = reader.ReadBytes(dataLength);

                return MessagePackSerializer.Deserialize<Config>(data);
            }
            catch
            {
                return null;
            }
        }
    }
}
