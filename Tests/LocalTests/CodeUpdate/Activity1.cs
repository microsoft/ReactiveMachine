using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LocalTests.CodeUpdate
{
    [ReplacedInVersion(1)]
    public class Activity1 : 
        IAtLeastOnceActivity<string>
    {
        public TimeSpan TimeLimit => TimeSpan.FromSeconds(30);
        public async Task<string> Execute(IContext context)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            return "Activity1";
        }

        public Activity2 Upgrade()
        {
            return new Activity2();   
        }
    }
}
