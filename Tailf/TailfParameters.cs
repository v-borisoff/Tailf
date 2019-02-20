namespace Tailf
{
    public class TailfParameters
    {   
        public string FilePath { get; set; }
        public int NumLines { get; set; }
        public string LineFilter { get; set; }
        public string LevelRegex { get; set; }
    }
}
