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
using System.Web;
using System.Web.Mvc;
using MPExtended.Applications.WebMediaPortal.Code;
using MPExtended.Applications.WebMediaPortal.Models;
using MPExtended.Libraries.Client;

namespace MPExtended.Applications.WebMediaPortal.Controllers
{
    [ServiceAuthorize]
    public class TVServerController : BaseController
    {
        //
        // GET: /TVServer/
        public ActionResult Index()
        {
            var recordingDiskInfo = MPEServices.TAS.GetAllRecordingDiskInformation();
            var cards = MPEServices.TAS.GetCards();
            var activeCards = MPEServices.TAS.GetActiveCards();
            return View(new TVServerStatusViewModel(cards, activeCards, recordingDiskInfo));
        }

        public ActionResult Stop(string user)
        {
            MPEServices.TAS.CancelCurrentTimeShifting(user);
            return RedirectToAction("Index");
        }

        public ActionResult Details(string user)
        {
            var vcard = MPEServices.TAS.GetActiveCards().Where(vc => vc.User.Name == user).FirstOrDefault();
            if (vcard == null)
                return HttpNotFound();

            var card = MPEServices.TAS.GetCards().Where(c => c.IdCard == vcard.Id).FirstOrDefault();
            if (card == null)
                return HttpNotFound();

            return View(new TVCardViewModel(card, vcard));
        }
    }
}
