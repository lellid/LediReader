// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LediReader.Book;

namespace LediReader.Gui
{
    public class BookSettingsController : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        bool _isBookInDarkMode;
        bool _isGuiInDarkMode;
        double _margin;

        #region Bindables

        public bool IsBookInDarkMode
        {
            get
            {
                return _isBookInDarkMode;
            }
            set
            {
                if (!(_isBookInDarkMode == value))
                {
                    _isBookInDarkMode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBookInDarkMode)));
                }
            }
        }

        public bool IsGuiInDarkMode
        {
            get
            {
                return _isGuiInDarkMode;
            }
            set
            {
                if (!(_isGuiInDarkMode == value))
                {
                    _isGuiInDarkMode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGuiInDarkMode)));
                }
            }
        }

        public double LeftAndRightMargin
        {
            get
            {
                return _margin;
            }
            set
            {
                value = Math.Round(value);
                if (!(_margin == value))
                {
                    _margin = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LeftAndRightMargin)));
                }
            }
        }

        public void Initialize(BookSettings bookSettings)
        {
            IsGuiInDarkMode = bookSettings.IsGuiInDarkMode;
            IsBookInDarkMode = bookSettings.IsBookInDarkMode;
            LeftAndRightMargin = bookSettings.LeftAndRightMargin;
        }

        public void Apply(BookSettings bookSettings)
        {
            bookSettings.IsGuiInDarkMode = IsGuiInDarkMode;
            bookSettings.IsBookInDarkMode = IsBookInDarkMode;
            bookSettings.LeftAndRightMargin = LeftAndRightMargin;
        }


        #endregion
    }
}
