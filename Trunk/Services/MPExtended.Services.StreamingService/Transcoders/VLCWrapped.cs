﻿#region Copyright (C) 2011 MPExtended
// Copyright (C) 2011 MPExtended Developers, http://mpextended.codeplex.com/
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
using MPExtended.Services.StreamingService.Units;
using MPExtended.Services.StreamingService.Util;
using MPExtended.Services.StreamingService.Interfaces;
using MPExtended.Services.StreamingService.Code;

namespace MPExtended.Services.StreamingService.Transcoders
{
    internal class VLCWrapped : VLC
    {
        public override void AlterPipeline(Pipeline pipeline, Resolution outputSize, Reference<WebTranscodingInfo> einfo, int position, int? audioId, int? subtitleId)
        {
            base.AlterPipeline(pipeline, outputSize, einfo, position, audioId, subtitleId, EncoderUnit.LogStream.StandardOut);

            // setup output parsing
            VLCWrapperParsing logunit = new VLCWrapperParsing(einfo);
            pipeline.AddLogUnit(logunit, 6);
        }

        protected override string GenerateArguments(string input, string sout, string args)
        {
            return String.Format("\"{0}\" \"{1}\" {2}", input, sout, args);
        }
    }
}