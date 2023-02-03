using Cornifer.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Helpers
{
    public interface ITabController<T> where T : Tab
    {
        void AddTab(T tab);
        void RemoveTab(T tab);
        void ClearTabs();
    }
}
