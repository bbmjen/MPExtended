﻿#region Copyright (C) 2011 MPExtended
// Copyright (C) 2011 MPExtended Developers, http://mpextended.github.com/
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
using System.ServiceModel;
using MPExtended.Services.MetaService.Interfaces;

namespace MPExtended.Services.MetaService
{
    internal static class ServiceClientFactory
    {
        public static IMetaService Create(string url)
        {
            try
            {
                ChannelFactory<IMetaService> factory = new ChannelFactory<IMetaService>(
                    new BasicHttpBinding() { MaxReceivedMessageSize = 100000000 },
                    new EndpointAddress(String.Format("http://{0}:4322/MPExtended/MetaService", url))
                );

                IMetaService channel = factory.CreateChannel();
                if (!channel.TestConnection())
                {
                    return null;
                }

                return channel;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}