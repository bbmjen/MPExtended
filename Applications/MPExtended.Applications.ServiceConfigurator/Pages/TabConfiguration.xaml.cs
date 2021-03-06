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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Navigation;
using MPExtended.Applications.ServiceConfigurator.Code;
using MPExtended.Libraries.Service;
using MPExtended.Libraries.Service.Hosting;

namespace MPExtended.Applications.ServiceConfigurator.Pages
{
    /// <summary>
    /// Interaction logic for TabConfiguration.xaml
    /// </summary>
    public partial class TabConfiguration : Page, ITabCloseCallback
    {
        public TabConfiguration()
        {
            InitializeComponent();

            // load config
            txtPort.Text = Configuration.Services.Port.ToString();
            txtServiceName.Text = GetServiceName();
            txtNetworkUser.Text = Configuration.Services.NetworkImpersonation.Username;
            txtNetworkPassword.Password = Configuration.Services.NetworkImpersonation.GetPassword();

            // check if bonjour is enabled
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += delegate(object source, DoWorkEventArgs args)
            {
                args.Result = Zeroconf.CheckBonjourInstallation();
            };
            bw.RunWorkerCompleted += delegate (object source, RunWorkerCompletedEventArgs args)
            {
                tbAutodetection.Inlines.Clear();
                tbAutodetection.Inlines.Add(Strings.UI.AutodetectionText);

                if (!(bool)args.Result)
                {
                    tbAutodetection.Inlines.Add(new LineBreak());
                    tbAutodetection.Inlines.Add(Strings.UI.BonjourNotInstalled);
                    Hyperlink link = new Hyperlink();
                    link.NavigateUri = new Uri("http://support.apple.com/kb/DL999");
                    link.RequestNavigate += CommonEventHandlers.NavigateHyperlink;
                    link.Inlines.Add(Strings.UI.BonjourNotInstalledDownload);
                    tbAutodetection.Inlines.Add(link);

                    cbBonjourEnabled.IsChecked = false;
                }
                else
                {
                    lblServiceName.IsEnabled = true;
                    cbBonjourEnabled.IsEnabled = true;
                    cbBonjourEnabled.IsChecked = Configuration.Services.BonjourEnabled;
                    txtServiceName.IsEnabled = true;
                }
            };
            bw.RunWorkerAsync();
        }

        public void TabClosed()
        {
            Configuration.Services.Port = Int32.Parse(txtPort.Text);
            Configuration.Services.BonjourName = txtServiceName.Text;
            Configuration.Services.BonjourEnabled = cbBonjourEnabled.IsChecked.Value;
            Configuration.Services.NetworkImpersonation.Username = txtNetworkUser.Text;
            Configuration.Services.NetworkImpersonation.SetPasswordFromPlaintext(txtNetworkPassword.Password);

            Configuration.Services.Save();
        }

        private string GetServiceName()
        {
            string value = Configuration.Services.BonjourName;
            if (!String.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            try
            {
                return System.Environment.MachineName;
            }
            catch (Exception)
            {
                return "MPExtended Services";
            }
        }

        private void btnTestCredentials_Click(object sender, RoutedEventArgs e)
        {
            if (CredentialTester.TestCredentials("", txtNetworkUser.Text, txtNetworkPassword.Password))
            {
                MessageBox.Show(Strings.UI.CredentialValidationSuccessful, "MPExtended", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(Strings.UI.CredentialValidationFailed, "MPExtended", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            CommonEventHandlers.NavigateHyperlink(sender, e);
        }
    }
}
