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
using System.Linq;
using System.Text;

namespace MPExtended.Libraries.SQLitePlugin
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate)]
    public class AllowSQLSortAttribute : Attribute
    {
        private string SQLText;

        public AllowSQLSortAttribute()
        {
            this.SQLText = "%fullsqlname %order";
        }

        public AllowSQLSortAttribute(string SQL)
        {
            this.SQLText = SQL;
        }

        public string GetSQLText(SQLFieldMapping mapping)
        {
            return SQLText
                .Replace("%fullsqlname", mapping.FullSQLName)
                .Replace("%table", mapping.Table)
                .Replace("%field", mapping.Field)
                .Replace("%property", mapping.PropertyName);
        }
    }
}
