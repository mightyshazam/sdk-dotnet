using System;
using Candidly.Util;

using PayloadContainers = Temporal.Common.Payload;

namespace Temporal.Demos.AdHocScenarios
{
    public class Program
    {
        public static void Main(string[] _)
        {
            Console.WriteLine($"RuntimeEnvironmentInfo: \n{RuntimeEnvironmentInfo.SingletonInstance}");

            Console.WriteLine($"name1=\"{nameof(PayloadContainers)}\"");
            Console.WriteLine($"name2=\"{nameof(Temporal.Common.Payload)}\"");
            Console.WriteLine($"name3=\"{typeof(PayloadContainers).Name}\"");
            Console.WriteLine($"name4=\"{typeof(PayloadContainers).FullName}\"");

            (new UseRawGrpcClient()).Run();
        }
    }
}