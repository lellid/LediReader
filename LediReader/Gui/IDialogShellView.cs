// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LediReader.Gui
{
    /// <summary>
    /// This interface is intended to provide a "shell" as a dialog which can host a user control.
    /// </summary>
    public interface IDialogShellView
    {
        /// <summary>
        /// Sets if the Apply button should be visible.
        /// </summary>
        bool ApplyVisible { set; }

        /// <summary>
        /// Sets the title
        /// </summary>
        string Title { set; }

        event Action<System.ComponentModel.CancelEventArgs> ButtonOKPressed;

        event Action ButtonCancelPressed;

        event Action ButtonApplyPressed;
    }
}
