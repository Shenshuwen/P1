namespace P1.Common
{
    public class ModuleConnectionOptions
    {
        public static ModuleConnectionOptions Instance { get; } = new();

        public string ModuleIp { get; set; } = "192.168.3.100";
        public int ModulePort { get; set; } = 10193;
    }
}
