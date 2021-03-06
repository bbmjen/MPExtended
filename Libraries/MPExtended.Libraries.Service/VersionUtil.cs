﻿#region Copyright (C) 2011-2012 MPExtended
// Copyright (C) 2011-2012 MPExtended Developers, http://mpextended.github.com/
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MPExtended.Libraries.Service.Internal;

namespace MPExtended.Libraries.Service
{
    /// <summary>
    /// Utility to parse version information from MPExtended and MediaPortal
    /// 
    /// We differentiate between 4 different versions for MPExtended:
    /// - The API version, defined by AssemblyVersion in GlobalVersion.cs. This is retrieved with GetVersion() and changes only
    ///   with new feature releases (minor/major). Only the first two numbers are relevant here.
    /// - The version name, defined by AssemblyInformationalVersion in GlobalVersion.cs. This is retrieved with GetVersioName()
    ///   and is different for each release. It isn't of a fixed format, and should only be displayed to the user and/or printed
    ///   to logs. Has no technical meaning.
    /// - The build version, defined by AssemblyFileVersion in GlobalVersion.cs. This is retrieved with GetBuildVersion() and
    ///   changes with each release. As opposed to AssemblyInformationalVersion, this number always increments and has a meaning.
    ///   It is the number that is used for checking for updates. 
    /// - The git build version, defined by AssemblyGitVersion in GitVersion.cs, only on this assembly. This is retrieved with
    ///   GetGitVersion() and changes with each build. 
    /// A full version string for logs can be retrieved with GetFullVersionString(). 
    /// </summary>
    public class VersionUtil
    {
        public enum MediaPortalVersion 
        {
            Unknown = 1,
            MP1_1 = 2,
            MP1_2 = 3,
            MP1_3_Alpha = 4,
        }

        public static Version GetVersion()
        {
            return GetVersion(Assembly.GetExecutingAssembly());
        }

        public static Version GetVersion(Assembly asm)
        {
            return asm.GetName().Version;
        }

        public static string GetVersionName()
        {
            return GetVersionName(Assembly.GetExecutingAssembly());
        }

        public static string GetVersionName(Assembly asm)
        {
            return System.Diagnostics.FileVersionInfo.GetVersionInfo(asm.Location).ProductVersion;
        }

        public static Version GetBuildVersion()
        {
            return GetBuildVersion(Assembly.GetExecutingAssembly());
        }

        public static Version GetBuildVersion(Assembly asm)
        {
            string ver = System.Diagnostics.FileVersionInfo.GetVersionInfo(asm.Location).FileVersion;
            return new Version(ver);
        }

        public static string GetGitVersion()
        {
            var attributes = (AssemblyGitVersionAttribute[])Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyGitVersionAttribute), true);
            if (attributes.Length == 0)
            {
                return null;
            }
            else
            {
                string fullHash = attributes.First().Commit;
                return fullHash.Substring(0, Math.Min(fullHash.Length, 7));
            }
        }

        public static string GetFullVersionString()
        {
            string gitVersion = GetGitVersion();
            if (gitVersion.Length > 0)
            {
                return String.Format("{0} (commit {1})", GetVersionName(), gitVersion);
            }
            else
            {
                return string.Format("{0} (build {1})", GetVersionName(), GetBuildVersion());
            }
        }

        public static MediaPortalVersion GetMediaPortalVersion()
        {
            Version v = GetCompleteMediaPortalVersion();
            if (v.Major == 1 && v.Minor == 1)
            {
                return MediaPortalVersion.MP1_1;
            }
            else if (v.Major == 1 && v.Minor == 2)
            {
                return MediaPortalVersion.MP1_2;
            }
            else if (v.Major == 1 && v.Minor == 3)
            {
                return MediaPortalVersion.MP1_3_Alpha;
            }

            return MediaPortalVersion.Unknown;
        }

        public static Version GetCompleteMediaPortalVersion()
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(GetMediaPortalAssemblyPath());
            return new Version(info.FileVersion);
        }

        public static Version GetMediaPortalBuildVersion()
        {
            return AssemblyName.GetAssemblyName(GetMediaPortalAssemblyPath()).Version;
        }

        private static string GetMediaPortalAssemblyPath()
        {
            string tv = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Team MediaPortal", "MediaPortal TV Server", "TvService.exe");
            if (File.Exists(tv))
            {
                return tv;
            }

            string mp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Team MediaPortal", "MediaPortal", "MediaPortal.exe");
            if (File.Exists(mp))
            {
                return mp;
            }

            Log.Error("Cannot find installed TvService.exe or MediaPortal.exe");
            return null;
        }
    }
}
