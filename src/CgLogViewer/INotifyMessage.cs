using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CgLogViewer
{
    public interface INotifyMessage
    {
        bool Notify(string message);
    }
}
