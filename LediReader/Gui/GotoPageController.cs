using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LediReader.Gui
{
    public class GotoPageController : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;


        private int _pageNumber = 1;
        public int PageNumber
        {
            get
            {
                return _pageNumber;
            }
            set
            {
                if (!(_pageNumber == value))
                {
                    _pageNumber = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PageNumber)));
                }
            }
        }

        private int _maxPageNumber = 1;
        public int MaxPageNumber
        {
            get
            {
                return _maxPageNumber;
            }
            set
            {
                if (!(_maxPageNumber == value))
                {
                    _maxPageNumber = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxPageNumber)));
                }
            }
        }

    }
}
