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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MPExtended.Applications.WebMediaPortal.Code;
using MPExtended.Applications.WebMediaPortal.Models;
using MPExtended.Libraries.Client;
using MPExtended.Libraries.Service;
using MPExtended.Libraries.Service.Util;
using MPExtended.Services.MediaAccessService.Interfaces;
using MPExtended.Services.StreamingService.Interfaces;

namespace MPExtended.Applications.WebMediaPortal.Controllers
{
    public class StreamController : BaseController
    {
        // This is the timeout after which streams are automatically killed (in seconds)
        private const int STREAM_TIMEOUT_DIRECT = 10;
        private const int STREAM_TIMEOUT_PROXY = 300;
		
        private static List<string> PlayerOpenedBy = new List<string>();
        private static Dictionary<string, string> RunningStreams = new Dictionary<string, string>();
        
        // Make this a static property to avoid seeding it with the same time for CreatePlayer() and GenerateStream()
        private static Random randomGenerator = new Random();

        //
        // Streaming
        private int? GetProvider(WebStreamMediaType type)
        {
            switch (type)
            {
                case WebStreamMediaType.File:
                    return Settings.ActiveSettings.FileSystemProvider;
                case WebStreamMediaType.Movie:
                    return Settings.ActiveSettings.MovieProvider;
                case WebStreamMediaType.MusicAlbum:
                case WebStreamMediaType.MusicTrack:
                    return Settings.ActiveSettings.MusicProvider;
                case WebStreamMediaType.Picture:
                    return Settings.ActiveSettings.PicturesProvider;
                case WebStreamMediaType.Recording:
                case WebStreamMediaType.TV:
                    return 0;
                case WebStreamMediaType.TVEpisode:
                case WebStreamMediaType.TVSeason:
                case WebStreamMediaType.TVShow:
                    return Settings.ActiveSettings.TVShowProvider;
                default:
                    // this cannot happen
                    return 0;
            }
        }

        [ServiceAuthorize]
        public ActionResult Download(WebStreamMediaType type, string item)
        {
            // Create URL to GetMediaItem
            Log.Debug("User wants to download type={0}; item={1}", type, item);
            var queryString = HttpUtility.ParseQueryString(String.Empty); // you can't instantiate that class manually for some reason
            queryString["clientDescription"] = String.Format("WebMediaPortal download (user {0})", HttpContext.User.Identity.Name);
            queryString["type"] = ((int)type).ToString();
            queryString["itemId"] = item;
            string rootUrl = type == WebStreamMediaType.TV || type == WebStreamMediaType.Recording ? MPEServices.HttpTASStreamRoot : MPEServices.HttpMASStreamRoot;
            UriBuilder fullUri = new UriBuilder(rootUrl + "GetMediaItem?" + queryString.ToString());

            // Check stream type
            StreamType streamMode = Settings.ActiveSettings.StreamType;
            if (streamMode == StreamType.DirectWhenPossible)
            {
                streamMode = NetworkInformation.IsOnLAN(HttpContext.Request.UserHostAddress) ? StreamType.Direct : StreamType.Proxied;
            }

            // If we connect to the services at localhost, actually give the extern IP address to users
            if (fullUri.Host == "localhost" || fullUri.Host == "127.0.0.1")
            {
                fullUri.Host = NetworkInformation.GetIPAddresses().First();
            }

            // Do the actual streaming
            if (streamMode == StreamType.Proxied)
            {
                Log.Debug("Proxying download at {0}", fullUri.ToString());
                GetStreamControl(type).AuthorizeStreaming();
                ProxyStream(fullUri.ToString());
            }
            else if (streamMode == StreamType.Direct)
            {
                Log.Debug("Redirecting user to download at {0}", fullUri.ToString()); 
                GetStreamControl(type).AuthorizeRemoteHostForStreaming(HttpContext.Request.UserHostAddress);
                return Redirect(fullUri.ToString());
            }
            return new EmptyResult();
        }

        private ActionResult GenerateStream(WebStreamMediaType type, string itemId, string transcoder, int starttime, string continuationId)
        {
            // Check if there is actually a player requested for this stream
            if (!PlayerOpenedBy.Contains(Request.UserHostAddress))
            {
                Log.Warn("User {0} (host {1}) requested a stream but hasn't opened a player page - denying access to stream", HttpContext.User.Identity.Name, Request.UserHostAddress);
                return new HttpUnauthorizedResult();
            }

            // Generate random identifier, and continuationId if needed
            string identifier = "webmediaportal-" + randomGenerator.Next(10000, 99999);
            if (continuationId == null)
            {
                continuationId = "none-provided-" + randomGenerator.Next(10000, 99999).ToString();
            }

            // Kill previous stream, but only if we expect it to be still running (avoid useless calls in non-seek and proxied cases)
            if (RunningStreams.ContainsKey(continuationId))
            {
                Log.Debug("Killing off old streaming for continuation {0} with identifier {1} first", continuationId, RunningStreams[continuationId]);
                GetStreamControl(type).FinishStream(RunningStreams[continuationId]);
            }

            // Check stream mode, generate timeout setting and dump all info we got
            StreamType streamMode = Settings.ActiveSettings.StreamType;
            if (streamMode == StreamType.DirectWhenPossible)
            {
                streamMode = NetworkInformation.IsOnLAN(HttpContext.Request.UserHostAddress) ? StreamType.Direct : StreamType.Proxied;
            }
            int timeout = streamMode == StreamType.Direct ? STREAM_TIMEOUT_DIRECT : STREAM_TIMEOUT_PROXY;
            Log.Debug("Starting stream type={0}; itemId={1}; transcoder={2}; starttime={3}; continuationId={4}", type, itemId, transcoder, starttime, continuationId);
            Log.Debug("Stream is for user {0} from host {1}, has identifier {2} and is using mode {3} with timeout {4}s", 
                HttpContext.User.Identity.Name, Request.UserHostAddress, identifier, streamMode, timeout);

            // Start the stream
            string clientDescription = String.Format("WebMediaPortal (user {0})", HttpContext.User.Identity.Name);
            using (var scope = WCFClient.EnterOperationScope(GetStreamControl(type)))
            {
                WCFClient.SetHeader("forwardedFor", HttpContext.Request.UserHostAddress);
                if (!GetStreamControl(type).InitStream((WebStreamMediaType)type, GetProvider(type), itemId, clientDescription, identifier, timeout))
                {
                    Log.Error("Streaming: InitStream failed");
                    return new EmptyResult();
                }
            }

            // save stream
            RunningStreams[continuationId] = identifier;
            string url = GetStreamControl(type).StartStream(identifier, transcoder, starttime);
            if (String.IsNullOrEmpty(url))
            {
                Log.Error("Streaming: StartStream failed");
                return new EmptyResult();
            }
            Log.Debug("Stream started successfully and is at {0}", url);

            // Do the actual streaming
            if (streamMode == StreamType.Proxied)
            {
				GetStreamControl(type).AuthorizeStreaming();
                ProxyStream(url);
            }
            else if (streamMode == StreamType.Direct)
            {
                GetStreamControl(type).AuthorizeRemoteHostForStreaming(HttpContext.Request.UserHostAddress);
                return Redirect(url);
            }

            // kill stream (doesn't matter much if this doesn't happen, WSS kills streams automatically nowadays)
            Log.Debug("Finished stream {0}", identifier);
            RunningStreams.Remove(continuationId);
            if (!GetStreamControl(type).FinishStream(identifier))
            {
                Log.Error("Streaming: FinishStream failed");
            }
            return new EmptyResult();
        }

        protected void ProxyStream(string sourceUrl)
        {
            byte[] buffer = new byte[65536]; // we don't actually read the full buffer each time, so a big size is ok
            int read;

            // do request
            Log.Trace("Proxying stream from {0} with buffer size {1}", sourceUrl, buffer.Length);
            WebRequest request = WebRequest.Create(sourceUrl);
            request.Headers.Add("X-Forwarded-For", HttpContext.Request.UserHostAddress);
            WebResponse response = request.GetResponse();
            Stream sourceStream = response.GetResponseStream();

            // set headers and disable buffer
            HttpContext.Response.Buffer = false;
            HttpContext.Response.BufferOutput = false;
            HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
            HttpContext.Response.ContentType = response.Headers["Content-Type"] == null ? "video/MP2T" : response.Headers["Content-Type"];
            foreach (string header in response.Headers.Keys)
            {
                if (header.StartsWith("Content-"))
                {
                    HttpContext.Response.AddHeader(header, response.Headers[header]);
                }
                else if (header.StartsWith("X-Content-")) // We set the Content-Length header with the X- prefix because WCF removes the normal header
                {
                    HttpContext.Response.AddHeader(header.Substring(2), response.Headers[header]);
                }
            }

            // stream to output
            try
            {
                while (HttpContext.Response.IsClientConnected && (read = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    HttpContext.Response.OutputStream.Write(buffer, 0, read);
                    HttpContext.Response.OutputStream.Flush(); // TODO: is this needed?
                }
            }
            catch (HttpException ex)
            {
                Log.Warn(String.Format("HttpException while proxying stream {0}", sourceUrl), ex);
            }
        }

        //
        // Stream wrapper URLs
        public ActionResult TV(string item, string transcoder, int starttime = 0, string continuationId = null)
        {
            return GenerateStream(WebStreamMediaType.TV, item, transcoder, starttime, continuationId);
        }

        public ActionResult Movie(string item, string transcoder, int starttime = 0, string continuationId = null)
        {
            return GenerateStream(WebStreamMediaType.Movie, item, transcoder, starttime, continuationId);
        }

        public ActionResult TVEpisode(string item, string transcoder, int starttime = 0, string continuationId = null)
        {
            return GenerateStream(WebStreamMediaType.TVEpisode, item, transcoder, starttime, continuationId);
        }

        public ActionResult Recording(string item, string transcoder, int starttime = 0, string continuationId = null)
        {
            return GenerateStream(WebStreamMediaType.Recording, item, transcoder, starttime, continuationId);
        }

        public ActionResult MusicTrack(string item, string transcoder, int starttime = 0, string continuationId = null)
        {
            return GenerateStream(WebStreamMediaType.MusicTrack, item, transcoder, starttime, continuationId);
        }

        //
        // Player
        private WebTranscoderProfile GetProfile(IWebStreamingService streamControl, string defaultProfile)
        {
            // get transcoding profile
            string profileName = null;

            if (Request.QueryString["transcoder"] != null)
                profileName = Request.QueryString["transcoder"];
            if (Request.Form["transcoder"] != null)
                profileName = Request.Form["transcoder"];
            if (profileName == null)
                profileName = defaultProfile;
            return streamControl.GetTranscoderProfileByName(profileName);
        }

        private IWebStreamingService GetStreamControl(WebStreamMediaType type)
        {
            if (type == WebStreamMediaType.TV || type == WebStreamMediaType.Recording)
            {
                return MPEServices.TASStreamControl;
            }
            else
            {
                return MPEServices.MASStreamControl;
            }
        }

        internal ActionResult CreatePlayer(IWebStreamingService streamControl, PlayerViewModel model, List<StreamTarget> targets, WebTranscoderProfile profile)
        {
            // save stream request
            if (!PlayerOpenedBy.Contains(Request.UserHostAddress))
            {
                PlayerOpenedBy.Add(Request.UserHostAddress);
            }

            // get all transcoder profiles
            List<string> profiles = new List<string>();
            foreach (StreamTarget target in targets)
            {
                profiles = profiles.Concat(streamControl.GetTranscoderProfilesForTarget(target.Name).Select(x => x.Name)).ToList();
            }

            // get view properties
            VideoPlayer player = targets.First(x => x.Name == profile.Target).Player;
            string viewName = Enum.GetName(typeof(VideoPlayer), player) + "Player";

            // generate view
            model.Transcoders = profiles;
            model.Transcoder = profile.Name;
            model.TranscoderProfile = profile;
            model.Player = player;
            model.PlayerViewName = viewName;
            Log.Debug("Created player with size={0} view={1} transcoder={2} url={3}", model.Size, viewName, profile.Name, model.URL);
            return PartialView("Player", model);
        }

        [ServiceAuthorize]
        public ActionResult Player(WebStreamMediaType type, string itemId)
        {
            PlayerViewModel model = new PlayerViewModel();
            model.MediaType = type;
            model.MediaId = itemId;

            // get profile
            var defaultProfile = type == WebStreamMediaType.TV || type == WebStreamMediaType.Recording ?
                Settings.ActiveSettings.DefaultTVProfile :
                Settings.ActiveSettings.DefaultMediaProfile;
            var profile = GetProfile(GetStreamControl(type), defaultProfile);
 
            // get size
            if(type == WebStreamMediaType.TV)
            {
                // TODO: we should start the timeshifting through an AJAX call, and then load the player based upon the results
                // from that call. Also avoids timeouts of the player when initiating the timeshifting takes a long time.
                // HACK: currently there is no method in WSS to get the aspect ratio for streams with a fixed aspect ratio. 
                model.Size = GetStreamControl(type).GetStreamSize(type, null, "", profile.Name);
            } 
            else 
            {
                model.Size = GetStreamControl(type).GetStreamSize(type, GetProvider(type), itemId, profile.Name);
            }

            // generate url
            RouteValueDictionary parameters = new RouteValueDictionary();
            parameters["item"] = itemId;
            parameters["transcoder"] = profile.Name;
            parameters["continuationId"] = randomGenerator.Next(10000, 99999);
            model.URL = Url.Action(Enum.GetName(typeof(WebStreamMediaType), type), parameters);

            // generic part
            return CreatePlayer(GetStreamControl(type), model, StreamTarget.GetVideoTargets(), profile);
        }

        [ServiceAuthorize]
        public ActionResult MusicPlayer(string albumId)
        {
            AlbumPlayerViewModel model = new AlbumPlayerViewModel();
            model.MediaId = albumId;
            WebTranscoderProfile profile = GetProfile(MPEServices.MASStreamControl, Settings.ActiveSettings.DefaultAudioProfile);
            model.Tracks = MPEServices.MAS.GetMusicTracksDetailedForAlbum(Settings.ActiveSettings.MusicProvider, albumId);
            return CreatePlayer(MPEServices.MASStreamControl, model, StreamTarget.GetAudioTargets(), profile);
        }

        [ServiceAuthorize]
        public ActionResult Playlist(WebStreamMediaType type, string itemId)
        {
            // save stream request
            if (!PlayerOpenedBy.Contains(Request.UserHostAddress))
            {
                PlayerOpenedBy.Add(Request.UserHostAddress);
            }

            // get profile
            var defaultProfile = type == WebStreamMediaType.TV || type == WebStreamMediaType.Recording ?
                Settings.ActiveSettings.DefaultTVProfile :
                Settings.ActiveSettings.DefaultMediaProfile;
            var profile = GetProfile(GetStreamControl(type), defaultProfile);

            // generate url
            RouteValueDictionary parameters = new RouteValueDictionary();
            parameters["item"] = itemId;
            parameters["transcoder"] = profile.Name;
            parameters["continuationId"] = "playlist-" + randomGenerator.Next(10000, 99999);
            string url = Url.Action(Enum.GetName(typeof(WebStreamMediaType), type), "Stream", parameters, Request.Url.Scheme, Request.Url.Host);

            // create playlist
            StringBuilder m3u = new StringBuilder();
            m3u.AppendLine("#EXTM3U");
            m3u.AppendLine("#EXTINF:-1, " + MediaName.GetMediaName(type, itemId));
            m3u.AppendLine(url);

            // return it
            byte[] data = Encoding.UTF8.GetBytes(m3u.ToString());
            return File(data, "audio/x-mpegurl", "stream.m3u");
        }
    }
}