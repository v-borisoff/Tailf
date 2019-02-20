using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace Tailf
{
    public class Tail
    {
        ManualResetEvent _resetEvent;
        public event EventHandler<TailEventArgs> Changed;

        const string default_level = "INFO";
        private string _currentLevel = default_level;

        const int pollInterval = 100;
        const int bufSize = 4096;

        string previous = "";
        private long prevLen = -1;
       
        private string _filePath;
        private int _numLines;

        private bool requestForExit = false;

        private Regex _lineFilterRegex;
        private Regex _levelRegex;

        public Tail(TailfParameters parameters)
        {
            _filePath = parameters.FilePath;
            _numLines = parameters.NumLines;

            if (!string.IsNullOrEmpty(parameters.LineFilter))
                _lineFilterRegex = new Regex(parameters.LineFilter);

            if (!string.IsNullOrEmpty(parameters.LevelRegex))
                _levelRegex = new Regex(parameters.LevelRegex, RegexOptions.Compiled | RegexOptions.Multiline);

            _resetEvent = new ManualResetEvent(false);
        }


        public void Run()
        {
            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException("File does not exist:" + _filePath);
            }
            var fileInfo = new FileInfo(_filePath);
            prevLen = fileInfo.Length;
            MakeTail(_numLines, _filePath);

            ThreadPool.QueueUserWorkItem(new WaitCallback(q => EnterMainLoop()));
        }

        private void EnterMainLoop()
        {
            while (!requestForExit)
            {
                fw_Changed();
                Thread.Sleep(pollInterval);
            }
            _resetEvent.Set();
        }

        public void Stop()
        {
            requestForExit = true;
            _resetEvent.WaitOne();
        }

        void fw_Changed()
        {
            FileInfo fi = new FileInfo(_filePath);
            if( fi.Exists )
            {
                if (fi.Length != prevLen)
                {
                    if (fi.Length < prevLen)
                    { 
                        //assume truncated!
                        prevLen = 0;
                    }
                    using (var stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
                    {
                        stream.Seek(prevLen, SeekOrigin.Begin);
                        if (_lineFilterRegex != null)
                        {
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                var all = sr.ReadToEnd();
                                var lines = all.Split('\n');

                                var lastIndex = lines.Length - 1;

                                for (var i = 0; i < lines.Length; i++)
                                {
                                    var line = lines[i].TrimEnd('\r');

                                    if(i != lastIndex)
                                        OnChanged(line + Environment.NewLine);
                                    else
                                        OnChanged(line);
                                }
                            }
                        }
                        else
                        {
                            char[] buffer = new char[bufSize];
                            StringBuilder current = new StringBuilder();
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                int nRead;
                                do
                                {
                                    nRead = sr.ReadBlock(buffer, 0, bufSize);
                                    for (int i = 0; i < nRead; ++i)
                                    {
                                        if (buffer[i] == '\n' || buffer[i] == '\r')
                                        {
                                            if (current.Length > 0)
                                            {
                                                string line = string.Concat(previous, current);

                                                if (_lineFilterRegex.IsMatch(line))
                                                {
                                                    OnChanged(string.Concat(line, Environment.NewLine));
                                                }
                                            }
                                            current = new StringBuilder();
                                        }
                                        else
                                        {
                                            current.Append(buffer[i]);
                                        }
                                    }
                                } while (nRead > 0);
                                if (current.Length > 0)
                                {
                                    previous = current.ToString();
                                }
                            }
                        }

                    }
                }
                prevLen = fi.Length;
            }
            
        }
        private void MakeTail(int nLines,string path)
        {
            List<string> lines = new List<string>();
            using (var stream = new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Delete|FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(stream))
            {
                string line;
                while( null != (line=sr.ReadLine() ) )
                {
                    if (_lineFilterRegex != null)
                    {
                        if (_lineFilterRegex.IsMatch(line))
                        {
                            EnqueueLine(nLines, lines, line);
                        }
                    }
                    else
                    {
                        EnqueueLine(nLines, lines, line);
                    }
                }
            }

            foreach (var l in lines)
            {
                OnChanged(l);
            }
            
        }

        private static void EnqueueLine(int nLines, List<string> lines, string line)
        {
            if (lines.Count >= nLines)
            {
                lines.RemoveAt(0);
            }
            lines.Add(string.Concat(Environment.NewLine, line));
        }

        private void OnChanged(string l)
        {
            if (null == Changed)
                return;

            if (null == _levelRegex)
            {
                Changed(this, new TailEventArgs() { Line = l, Level = _currentLevel });
                return;
            }

            var match = _levelRegex.Match(l);

            if (null == match || !match.Success)
            {
                Changed(this, new TailEventArgs() { Line = l, Level = _currentLevel });
                return;
            }

            _currentLevel = match.Groups["level"].Value;

            Changed(this, new TailEventArgs() { Line = l, Level = _currentLevel });
        }
    }
}
