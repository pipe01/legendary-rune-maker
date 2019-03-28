using Legendary_Rune_Maker;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCU.NET.Extras
{
    public static class LCUApp
    {
        public static IUiActuator MainWindow
        {
            get;
            set;
        }

        public static IKernel Container
        {
            get;
            set;
        } = new StandardKernel();

    }
}
