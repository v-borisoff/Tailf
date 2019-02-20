using System;

namespace Tailf
{
    public class TailEventArgs : EventArgs
    {
        public string Level { get; set; }
        public string Line { get; set; }
    }

}
