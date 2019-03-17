using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Interface
{
    public delegate void SchduleTaskDelegate();
    public interface ISchduleTask
    {
        void Run();
    }
}
