﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using ProgressControls;

namespace OpenDnsDiagnostic
{
    public class TestStatus
    {
        public string _displayName;
        public virtual string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                _displayName = value;
            }
        }
        public bool Finished;
        public bool FailedToStart;
        public Label Label;
        public ProgressIndicator ProgressIndicator;

        public TestStatus()
        {
            _displayName = null;
            FailedToStart = false;
            Finished = false;
            Label = null;
            ProgressIndicator = null;
        }

        public virtual void Stop()
        {
            Finished = true;
            if (ProgressIndicator != null)
            {
                ProgressIndicator.Stop();
                ProgressIndicator.Visible = false;
            }
            if (Label != null)
            {
                Label.Text = "Finished: " + Label.Text;
                Label.ForeColor = System.Drawing.Color.Gray;
            }
        }

        public void WriteSeparatorLine(StreamWriter sw)
        {
            sw.WriteLine("---------------------------------------------");
        }

        public virtual void WriteResult(StreamWriter sw)
        {
        }
    }

    public class ProcessStatus : TestStatus
    {
        public override string DisplayName
        {
            get
            {
                if (null != _displayName)
                    return _displayName;
                if (null == Args)
                    return Exe;
                if (null == Comment)
                    return Exe + " " + Args;
                return Exe + " " + Args + " " + Comment;
            }
            set
            {
                _displayName = value;
            }
        }
        public string Exe;
        public string Args;
        public string Comment;
        public Process Process;
        public string StdOut;
        public string StdErr;
        public bool AddNewlinesAfterEmptyLine;

        void Init(string exe, string args, string comment, bool addNewlinesAfterEmptyLine)
        {
            Exe = exe;
            Debug.Assert(null != exe);
            Args = args;
            Comment = comment;
            StdOut = "";
            StdErr = "";

            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            p.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataReceived);
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = Exe;
            p.StartInfo.Arguments = Args;
            AddNewlinesAfterEmptyLine = addNewlinesAfterEmptyLine;
            Process = p;
        }

        public ProcessStatus(string exe, string args, bool addNewlinesAfterEmptyLine)
            : base()
        {
            Init(exe, args, null, addNewlinesAfterEmptyLine);
        }

        public ProcessStatus(string exe, string args)
            : base()
        {
            Init(exe, args, null, false);
        }

        public ProcessStatus(string exe, string args, string comment)
            : base()
        {
            Init(exe, args, comment, false);
        }

        private void p_OutputDataReceived(object sender, DataReceivedEventArgs data)
        {
            string s = data.Data;
            if (!String.IsNullOrEmpty(s))
            {
                StdOut += s;
                StdOut += Environment.NewLine;
            } 
            else 
            {
                if (AddNewlinesAfterEmptyLine)
                    StdOut += Environment.NewLine;
            }                
        }

        private void p_ErrorDataReceived(object sender, DataReceivedEventArgs data)
        {
            string s = data.Data;
            if (!String.IsNullOrEmpty(s))
            {
                StdErr += s;
                StdErr += Environment.NewLine;
            }
            else
            {
                if (AddNewlinesAfterEmptyLine)
                    StdErr += Environment.NewLine;
            }
        }

        public virtual void Start()
        {
            Process.Start();
            Process.BeginOutputReadLine();
        }

        public override void WriteResult(StreamWriter sw)
        {
            WriteSeparatorLine(sw);
            sw.WriteLine("Results for: " + DisplayName);
            if (!String.IsNullOrEmpty(StdOut))
            {
                sw.WriteLine("stdout:");
                sw.WriteLine(StdOut);
            }

            if (!String.IsNullOrEmpty(StdErr))
            {
                sw.WriteLine("stderr:");
                sw.WriteLine(StdErr);
            }
        }
    }

    public class MultiProcessStatus : TestStatus
    {
        public List<ProcessStatus> Processes = new List<ProcessStatus>();
        public MultiProcessStatus(string displayName)
            : base()
        {
            DisplayName = displayName;
        }

        public override void WriteResult(StreamWriter sw)
        {
            foreach (var ps in Processes)
                ps.WriteResult(sw);
        }

        public bool AllFinished()
        {
            foreach (var ps in Processes)
            {
                if (!ps.Finished)
                    return false;
            }
            return true;
        }
    }

    public class DnsResolveStatus : TestStatus
    {
        public string Hostname;
        public IPAddress[] IPAddresses;
        public DnsResolveStatus(string hostname)
            : base()
        {
            Hostname = hostname;
        }

        public override string DisplayName
        {
            get
            {
                if (null != _displayName)
                    return _displayName;
                return "DNS lookup of " + Hostname;
            }
            set
            {
                base.DisplayName = value;
            }
        }

        public override void WriteResult(StreamWriter sw)
        {
            WriteSeparatorLine(sw);
            sw.WriteLine("Results for: DNS lookup of " + Hostname);
            if (null == IPAddresses)
            {
                sw.WriteLine();
                return;
            }
            foreach (var ip in IPAddresses)
            {
                var s = "  " + ip.ToString();
                sw.WriteLine(s);
            }
            sw.WriteLine();
        }
    }
}
