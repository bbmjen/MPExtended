﻿#region Copyright (C) 2012 MPExtended
// Copyright (C) 2012 MPExtended Developers, http://mpextended.github.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

namespace MPExtended.Libraries.Service.Logging
{
    internal enum LogLevel
    {
        Trace = 1,
        Debug = 2,
        Info = 3,
        Warn = 4,
        Error = 5,
        Fatal = 6
    }

    internal class Logger
    {
        private ILogDestination[] destinations;

        public Logger(params ILogDestination[] destinations)
        {
            this.destinations = destinations;
        }

        public void LogLine(LogLevel level, string text, params object[] parameters)
        {
            string msg = String.Format(text, parameters);
            LogLine(level, msg);
        }

        public void LogLine(LogLevel level, string text, Exception ex)
        {
            LogLine(level, text);
            foreach (var dest in destinations)
            {
                if (level >= dest.MinimumLevel)
                {
                    dest.Output.WriteLine(ex.ToString());
                }
            }
        }

        public void LogLine(LogLevel level, string text)
        {
            string levelText = Enum.GetName(typeof(LogLevel), level).ToUpperInvariant();
            foreach (var dest in destinations)
            {
                if (level >= dest.MinimumLevel)
                {
                    dest.Output.WriteLine(String.Format(dest.LogFormat, DateTime.Now, Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId, levelText) + text);
                    dest.Output.Flush();
                }
            }
        }

        public void Flush()
        {
            foreach (var dest in destinations)
            {
                dest.Output.Flush();
            }
        }
    }
}
