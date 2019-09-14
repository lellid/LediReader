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

        bool _useDarkTheme;
        double _margin;

        #region Bindables

        public bool UseDarkTheme
        {
            get
            {
                return _useDarkTheme;
            }
            set
            {
                if (!(_useDarkTheme == value))
                {
                    _useDarkTheme = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseDarkTheme)));
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
            UseDarkTheme = bookSettings.IsInDarkMode;
            LeftAndRightMargin = bookSettings.LeftAndRightMargin;
        }

        public void Apply(BookSettings bookSettings)
        {
            bookSettings.IsInDarkMode = UseDarkTheme;
            bookSettings.LeftAndRightMargin = LeftAndRightMargin;
        }


        #endregion
    }
}
