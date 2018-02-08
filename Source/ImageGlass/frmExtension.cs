﻿/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2017 DUONG DIEU PHAP
Project homepage: http://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using ImageGlass.Theme;
using ImageGlass.Services.Configuration;
using System.Diagnostics;
using ImageGlass.Library;

namespace ImageGlass
{
    public partial class frmExtension : Form
    {
        public frmExtension()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Global.Plugins.ClosePlugins();
            Close();
        }

        private void btnRefreshAllExt_Click(object sender, EventArgs e)
        {
            LoadExtensions();
        }

        private void btnGetMoreExt_Click(object sender, EventArgs e)
        {
            try
            {
                string version = Application.ProductVersion.Replace(".", "_");
                Process.Start("http://www.imageglass.org/download/extensions?utm_source=app_" + version + "&utm_medium=app_click&utm_campaign=app_extension");
            }
            catch { }
        }

        private void btnInstallExt_Click(object sender, EventArgs e)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = GlobalSetting.StartUpDir + "igtasks.exe";
                p.StartInfo.Arguments = "iginstallext";

                try
                {
                    p.Start();
                }
                catch { }
            }
            catch { }
        }


        private void frmExtension_Load(object sender, EventArgs e)
        {
            //Load config
            //Windows Bound (Position + Size)--------------------------------------------
            Rectangle rc = GlobalSetting.StringToRect(GlobalSetting.GetConfig($"{Name}.WindowsBound", "280,125,850,550"));

            if (!Helper.IsOnScreen(rc.Location))
            {
                rc.Location = new Point(280, 125);
            }
            Bounds = rc;

            //windows state--------------------------------------------------------------
            string s = GlobalSetting.GetConfig($"{Name}.WindowsState", "Normal");
            if (s == "Normal")
            {
                WindowState = FormWindowState.Normal;
            }
            else if (s == "Maximized")
            {
                WindowState = FormWindowState.Maximized;
            }

            //Apply Windows theme
            RenderTheme r = new RenderTheme();
            r.ApplyTheme(tvExtension);

            //load extensions
            LoadExtensions();

            //Load language:
            Text = GlobalSetting.LangPack.Items["frmExtension._Text"];
            btnRefreshAllExt.Text = GlobalSetting.LangPack.Items["frmExtension.btnRefreshAllExt"];
            btnGetMoreExt.Text = GlobalSetting.LangPack.Items["frmExtension.btnGetMoreExt"];
            btnInstallExt.Text = GlobalSetting.LangPack.Items["frmExtension.btnInstallExt"];
            btnClose.Text = GlobalSetting.LangPack.Items["frmExtension.btnClose"];

            RightToLeft = GlobalSetting.LangPack.IsRightToLeftLayout;
        }

        private void tvExtension_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (tvExtension.SelectedNode != null)
            {
                ImageGlass.Plugins.Types.AvailablePlugin p = Global.Plugins.AvailablePlugins.Find(
                                          tvExtension.SelectedNode.Text);
                if (p != null)
                {
                    panExtension.Controls.Clear();
                    p.Instance.MainInterface.Dock = DockStyle.Fill;
                    panExtension.Controls.Add(p.Instance.MainInterface);
                }
                else
                {
                    panExtension.Controls.Clear();
                }
            }
        }

        private void frmExtension_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Save config---------------------------------
            if (WindowState == FormWindowState.Normal)
            {
                //Windows Bound-------------------------------------------------------------------
                GlobalSetting.SetConfig($"{Name}.WindowsBound", GlobalSetting.RectToString(Bounds));
            }

            //Windows State-------------------------------------------------------------------
            GlobalSetting.SetConfig($"{Name}.WindowsState", WindowState.ToString());
        }

        private void LoadExtensions()
        {
            tvExtension.Nodes.Clear();

            string pluginsDir = Path.Combine(Application.StartupPath, "Plugins");

            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }
            else
            {
                Global.Plugins.FindPlugins(pluginsDir);

                try
                {
                    foreach (ImageGlass.Plugins.Types.AvailablePlugin p in Global.Plugins.AvailablePlugins)
                    {
                        TreeNode n = new TreeNode(p.Instance.Name);
                        tvExtension.Nodes.Add(n);
                        n = null;
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
