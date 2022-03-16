using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ExceptionSerialization
{
    public class Program
    {
        public class MySpecialException : Exception
        {
            public MySpecialException()
                : base()
            {
            }

            public MySpecialException(string message)
                : base(message) 
            {
            }

            public MySpecialException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            public MySpecialException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

        }

        static void Main(string[] _)
        {
            (new Program()).Exec();
        }

        public void Exec()
        {
            Exception caughtEx = null;

            try
            {
                FuncA1();
            }
            catch (Exception ex)
            {
                caughtEx = ex;
            }

            Console.WriteLine($"\n-----------\nException being serialized:\n {caughtEx}");

            Type caughtExType = caughtEx.GetType();
            SerializationInfo info = new(caughtExType, new FormatterConverter());
            StreamingContext context = new(StreamingContextStates.CrossMachine);

            caughtEx.GetObjectData(info, context);

            try
            {
                FuncB1(info);
            } catch (Exception ex)
            {
                caughtEx = ex;
                Console.WriteLine($"\n-----------\nException caught second time:\n {ex}");
            }

            caughtExType = caughtEx.GetType();
            info = new(caughtExType, new FormatterConverter());
            context = new(StreamingContextStates.CrossMachine);

            caughtEx.GetObjectData(info, context);

            try
            {
                //FuncB1(info);
                FuncC1(caughtEx);
            }
            catch (Exception ex)
            {
                caughtEx = ex;
                Console.WriteLine($"\n-----------\nException caught third time:\n {ex}");
            }

            caughtExType = caughtEx.GetType();
            info = new(caughtExType, new FormatterConverter());
            context = new(StreamingContextStates.CrossMachine);

            caughtEx.GetObjectData(info, context);

            try
            {
                FuncB1(info);
            }
            catch (Exception ex)
            {
                caughtEx = ex;
                Console.WriteLine($"\n-----------\nException caught fourth time:\n {ex}");
            }

            caughtExType = caughtEx.GetType();
            info = new(caughtExType, new FormatterConverter());
            context = new(StreamingContextStates.CrossMachine);

            caughtEx.GetObjectData(info, context);

            try
            {
                FuncB1(info);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n-----------\nException caught fifth time:\n {ex}");
            }
        }

        void FuncA1()
        {
            FuncA2();
        }

        void FuncA2()
        {
            FuncA3();
        }

        void FuncA3()
        {
            ArgumentOutOfRangeException ex = new("myParam", "This is a test exception.");
            Console.WriteLine($"\n-----------\nException being thrown first time:\n {ex}");

            throw ex;
        }

        void FuncB1(SerializationInfo serializedEx)
        {
            FuncB2(serializedEx);
        }

        void FuncB2(SerializationInfo serializedEx)
        {
            FuncB3(serializedEx);
        }

        private static void PrintSerializationInfo(SerializationInfo serializedEx)
        {
            Console.WriteLine("\n-----------\nSerializationInfo:");
            Console.WriteLine($"ClassName:              \"{serializedEx.GetString("ClassName")}\"");
            Console.WriteLine($"Message:                \"{serializedEx.GetString("Message")}\"");
            Console.WriteLine($"Data:                   \"{serializedEx.GetValue("Data", typeof(IDictionary))}\"");
            Console.WriteLine($"InnerException:         \"{serializedEx.GetValue("InnerException", typeof(Exception))}\"");
            Console.WriteLine($"HelpURL:                \"{serializedEx.GetString("HelpURL")}\"");
            Console.WriteLine($"StackTraceString:       \"{serializedEx.GetString("StackTraceString")}\"");
            Console.WriteLine($"RemoteStackTraceString: \"{serializedEx.GetString("RemoteStackTraceString")}\"");
            Console.WriteLine($"RemoteStackIndex:       \"{serializedEx.GetInt32("RemoteStackIndex")}\"");
            Console.WriteLine($"ExceptionMethod:        \"{serializedEx.GetValue("ExceptionMethod", typeof(String))}\"");
            Console.WriteLine($"HResult:                \"{serializedEx.GetInt32("HResult")}\"");
            Console.WriteLine($"Source:                 \"{serializedEx.GetString("Source")}\"");
            Console.WriteLine($"WatsonBuckets:          \"{serializedEx.GetValue("WatsonBuckets", typeof(byte[]))}\"");
        }

        static class StackTraceMarkers
        {
            public const string StartRemoteTrace =        "--- -------- Start of stack trace from an external process -------- ---";
            public const string EndRemoteTrace =          "--- -------- End of stack trace from an external process -------- ---";
            public const string StartAnotherRemoteTrace = "--- -------- Start of stack trace from another external process -------- ---";
            
        }

        void FuncB3(SerializationInfo serializedEx)
        {
            //PrintSerializationInfo(serializedEx);

            string stackTrace = serializedEx.GetString("StackTraceString");
            string remoteStackTrace = serializedEx.GetString("RemoteStackTraceString");

            // --- End of stack trace from previous location ---

            if (!String.IsNullOrWhiteSpace(remoteStackTrace))
            {
                remoteStackTrace = remoteStackTrace.Trim();

                if (!remoteStackTrace.StartsWith(StackTraceMarkers.StartRemoteTrace))
                {
                    remoteStackTrace = StackTraceMarkers.StartRemoteTrace
                                     + Environment.NewLine
                                     + remoteStackTrace;
                }
                
                if (!remoteStackTrace.EndsWith(StackTraceMarkers.EndRemoteTrace))
                {
                    remoteStackTrace = remoteStackTrace
                                     + Environment.NewLine
                                     + StackTraceMarkers.EndRemoteTrace;
                }

                remoteStackTrace = remoteStackTrace
                                 + Environment.NewLine;
            }

            if (!String.IsNullOrWhiteSpace(stackTrace))
            {
                if (String.IsNullOrWhiteSpace(remoteStackTrace))
                {
                    remoteStackTrace = StackTraceMarkers.StartRemoteTrace;
                }
                else
                {
                    remoteStackTrace = remoteStackTrace
                                     + StackTraceMarkers.StartAnotherRemoteTrace;
                }

                remoteStackTrace = remoteStackTrace
                                + Environment.NewLine
                                + stackTrace
                                + Environment.NewLine
                                + StackTraceMarkers.EndRemoteTrace
                                + Environment.NewLine;

                stackTrace = String.Empty;
            }

            SerializationInfo mutatedSerializedEx = new(serializedEx.ObjectType, new FormatterConverter());
            SerializationInfoEnumerator serInfoEnum = serializedEx.GetEnumerator();
            while (serInfoEnum.MoveNext())
            {
                SerializationEntry curr = serInfoEnum.Current;

                if (curr.Name.Equals("StackTraceString") && curr.ObjectType == typeof(String))
                {
                    mutatedSerializedEx.AddValue("StackTraceString", stackTrace, typeof(String));
                }
                else if (curr.Name.Equals("RemoteStackTraceString") && curr.ObjectType == typeof(String))
                {
                    mutatedSerializedEx.AddValue("RemoteStackTraceString", remoteStackTrace, typeof(String));
                }
                else
                {
                    mutatedSerializedEx.AddValue(curr.Name, curr.Value, curr.ObjectType);
                }
            }

            //PrintSerializationInfo(mutatedSerializedEx);

            StreamingContext context = new(StreamingContextStates.CrossMachine);

            MySpecialException ex = new MySpecialException(mutatedSerializedEx, context);
            Console.WriteLine($"\n-----------\nException being thrown after rehydration:\n {ex}");

            //ExceptionDispatchInfo.Capture(ex).Throw();
            throw ex;
        }

        void FuncC1(Exception ex)
        {
            FuncC2(ex);
        }

        void FuncC2(Exception ex)
        {
            FuncC3(ex);
        }

        void FuncC3(Exception ex)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
        }
    }
}