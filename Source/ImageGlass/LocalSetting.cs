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


namespace ImageGlass
{
    public static class LocalSetting
    {
        private static frmFacebook _fFacebook = new frmFacebook();
        private static frmSetting _fSetting = new frmSetting();
        private static frmExtension _fExtension = new frmExtension();
        private static frmColorPicker _fColorPicker = new frmColorPicker();
        private static string _imageModifiedPath = "";
        private static bool _isResetScrollPosition = true;
        private static Theme.Theme _theme = new Theme.Theme();
        private static bool _isThumbnailDimensionChanged = false;

        #region "Properties"
        /// <summary>
        /// Form frmFacebook
        /// </summary>
        public static frmFacebook FFacebook
        {
            get { return _fFacebook; }
            set { _fFacebook = value; }
        }

        /// <summary>
        /// Form frmSetting
        /// </summary>
        public static frmSetting FSetting
        {
            get { return _fSetting; }
            set { _fSetting = value; }
        }

        /// <summary>
        /// Form frmExtension
        /// </summary>
        public static frmExtension FExtension
        {
            get { return _fExtension; }
            set { _fExtension = value; }
        }

        /// <summary>
        /// Form frmColorPicker
        /// </summary>
        public static frmColorPicker FColorPicker
        {
            get { return _fColorPicker; }
            set { _fColorPicker = value; }
        }

        /// <summary>
        /// Gets, sets value if image was modified
        /// </summary>
        public static string ImageModifiedPath
        {
            get { return _imageModifiedPath; }
            set { _imageModifiedPath = value; }
        }

        

        /// <summary>
        /// Gets, sets value indicating that picmain's scrollbar need to be reset
        /// </summary>
        public static bool IsResetScrollPosition
        {
            get
            {
                return _isResetScrollPosition;
            }

            set
            {
                _isResetScrollPosition = value;
            }
        }

        /// <summary>
        /// Gets, sets current app theme
        /// </summary>
        public static Theme.Theme Theme { get => _theme; set => _theme = value; }

        /// <summary>
        /// Gets, sets the value that will request frmMain to update thumbnail bar
        /// </summary>
        public static bool IsThumbnailDimensionChanged { get => _isThumbnailDimensionChanged; set => _isThumbnailDimensionChanged = value; }
        #endregion

    }
}



