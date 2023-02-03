using Cornifer.UI.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI
{
    public class TabCollection<T> : IEnumerable<T>
        where T : Tab
    {
        private readonly ITabController<T> Controller;

        private List<T> Tabs = new();

        internal TabCollection(ITabController<T> controller)
        {
            Controller = controller;
        }

        public void Add(T tab)
        {
            Tabs.Add(tab);
            Controller.AddTab(tab);
        }

        public void Remove(T tab)
        {
            Tabs.Remove(tab);
            Controller.RemoveTab(tab);
        }

        public void Clear()
        {
            Tabs.Clear();
            Controller.ClearTabs();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Tabs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
