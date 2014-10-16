using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mihari
{
    using System.Windows;

    public static class WindowExtensions
    {
        public static void ShowForeground(this Window Window)
        {
            Window.Show();
            Window.Activate();
            Window.Topmost = true;  
            Window.Topmost = false; 
            Window.Focus();         
        }
    }
}
