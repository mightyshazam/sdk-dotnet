using System;
using Candidly.Util;

namespace Temporal.Demos.AdHocScenarios
{
    public class Program
    {
        public static void Main(string[] _)
        {
            Console.WriteLine($"RuntimeEnvironmentInfo: \n{RuntimeEnvironmentInfo.SingeltonInstance}");

            (new UseRawGrpcClient()).Run();
        }
    }
}