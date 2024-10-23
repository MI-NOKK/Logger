using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logger
{
    static class Programs
    {
        [STAThread]
        static void Main(string[] args)
        {
            Log log = new Log("Project");

            log.Write("프로젝트 시작점입니다.");
        }

    }
}