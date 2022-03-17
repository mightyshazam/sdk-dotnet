using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExceptionSerialization
{
    public class Program
    {
        public interface ITemporalFailure
        {
        }

        public sealed class RemoteTemporalException : Exception
        {
            private static Exception AsException(ITemporalFailure failure)
            {
                if (failure == null)
                {
                    throw new ArgumentNullException(nameof(failure));
                }

                if (failure is not Exception exception)
                {
                    throw new ArgumentException($"The type of the specified instance of {nameof(ITemporalFailure)} must"
                                              + $" be a subclass of {nameof(Exception)}, but it is not the case for the actual"
                                              + $" runtime type (\"{failure.GetType().FullName}\").", nameof(failure));
                }

                return exception;
            }

#if NET6_0_OR_GREATER
        [System.Diagnostics.StackTraceHidden]
#endif
            public static RemoteTemporalException Throw(string message, ITemporalFailure innerException)
            {
                throw new RemoteTemporalException(message, innerException);
            }

            public RemoteTemporalException(string message, ITemporalFailure innerException)
                : base(message, AsException(innerException))
            {
                Cause = innerException;
            }

            public ITemporalFailure Cause { get; }
        }

        public class MySpecialException : Exception, ITemporalFailure
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


        internal static void Main(string[] _)
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
                Console.WriteLine($"\n-----------\nException caught first time:\n {ex}");
            }

            SerializationInfo info = new(caughtEx.GetType(), new FormatterConverter());
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

            info = new SerializationInfo(caughtEx.GetType(), new FormatterConverter());
            context = new StreamingContext(StreamingContextStates.CrossMachine);
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

            info = new SerializationInfo(caughtEx.GetType(), new FormatterConverter());
            context = new StreamingContext(StreamingContextStates.CrossMachine);
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

            info = new SerializationInfo(caughtEx.GetType(), new FormatterConverter());
            context = new StreamingContext(StreamingContextStates.CrossMachine);
            caughtEx.GetObjectData(info, context);

            try
            {
                FuncB1(info);
            }
            catch (Exception ex)
            {
                caughtEx = ex;
                Console.WriteLine($"\n-----------\nException caught fifth time:\n {ex}");
            }

            info = new SerializationInfo(caughtEx.GetType(), new FormatterConverter());
            context = new StreamingContext(StreamingContextStates.CrossMachine);
            caughtEx.GetObjectData(info, context);

            try
            {
                FuncE1(info);
            }
            catch (Exception ex)
            {
                caughtEx = ex;
                Console.WriteLine($"\n-----------\nException caught 6th time:\n {ex}");
            }

            info = new SerializationInfo(caughtEx.GetType(), new FormatterConverter());
            context = new StreamingContext(StreamingContextStates.CrossMachine);
            caughtEx.GetObjectData(info, context);

            try
            {
                FuncE1(info);
            }
            catch (Exception ex)
            {
                caughtEx = ex;
                Console.WriteLine($"\n-----------\nException caught 7th time:\n {ex}");
            }

            Console.WriteLine(caughtEx.ToString().Substring(0, 0));
        }

        internal void FuncA1()
        {
            FuncA2();
        }

        internal void FuncA2()
        {
            FuncA3();
        }

        internal void FuncA3()
        {
            ArgumentOutOfRangeException ex = new("myParam", "This is a test exception.");
            Console.WriteLine($"\n-----------\nException being thrown first time:\n {ex}");

            throw ex;
        }

        internal void FuncB1(SerializationInfo serializedEx)
        {
            FuncB2(serializedEx);
        }

        internal void FuncB2(SerializationInfo serializedEx)
        {
            FuncB3(serializedEx);
        }

        internal static void PrintSerializationInfo(SerializationInfo serializedEx)
        {
            Console.WriteLine("\n*********** SerializationInfo:");
            Console.WriteLine($"    ClassName:              \"{serializedEx.GetString("ClassName")}\"");
            Console.WriteLine($"    Message:                \"{serializedEx.GetString("Message")}\"");
            Console.WriteLine($"    Data:                   \"{serializedEx.GetValue("Data", typeof(IDictionary))}\"");
            Console.WriteLine($"    InnerException:         \"{serializedEx.GetValue("InnerException", typeof(Exception))}\"");
            Console.WriteLine($"    HelpURL:                \"{serializedEx.GetString("HelpURL")}\"");
            Console.WriteLine($"    StackTraceString:       \"{serializedEx.GetString("StackTraceString")}\"");
            Console.WriteLine($"    RemoteStackTraceString: \"{serializedEx.GetString("RemoteStackTraceString")}\"");
            Console.WriteLine($"    RemoteStackIndex:       \"{serializedEx.GetInt32("RemoteStackIndex")}\"");
            Console.WriteLine($"    ExceptionMethod:        \"{serializedEx.GetValue("ExceptionMethod", typeof(string))}\"");
            Console.WriteLine($"    HResult:                \"{serializedEx.GetInt32("HResult")}\"");
            Console.WriteLine($"    Source:                 \"{serializedEx.GetString("Source")}\"");
            Console.WriteLine($"    WatsonBuckets:          \"{serializedEx.GetValue("WatsonBuckets", typeof(byte[]))}\"");
        }

        internal static class StackTraceMarkers
        {
            public static class Literals
            {
                public const string StartRemoteTracePrefix = "--- -------- Start of stack trace from an external process";
                public const string StartRemoteTrace = StartRemoteTracePrefix + " -------- ---";

                public const string EndRemoteTrace = "--- -------- End of stack trace from an external process -------- ---";
                public const string StartAnotherRemoteTrace = "--- -------- Start of stack trace from another external process -------- ---";
            }

            public static class Templates
            {
                public const string StartRemoteTrace = "--- -------- Start of stack trace from an external process {0}-------- ---";
                public const string EndRemoteTrace = "--- -------- End of stack trace from an external process {0}-------- ---";
                public const string StartAnotherRemoteTrace = "--- -------- Start of stack trace from another external process {0}-------- ---";
            }
        }

        internal static Exception RehydrateException(SerializationInfo serializedEx, bool formatForBeingWrapped)
        {
            bool useNewLineAfterRemoteStack = !formatForBeingWrapped;
            //PrintSerializationInfo(serializedEx);

            string stackTrace = serializedEx.GetString("StackTraceString");
            string remoteStackTrace = serializedEx.GetString("RemoteStackTraceString");
            string className = serializedEx.GetString("ClassName");

            // --- End of stack trace from previous location ---

            StringBuilder remoteTraceBuilder = new();

            if (!String.IsNullOrWhiteSpace(remoteStackTrace))
            {
                remoteStackTrace = remoteStackTrace.Trim();

                bool alreadyMarkedUp = remoteStackTrace.StartsWith(StackTraceMarkers.Literals.StartRemoteTracePrefix);

                if (!alreadyMarkedUp)
                {
                    remoteTraceBuilder.AppendLine(StackTraceMarkers.Literals.StartRemoteTrace);
                }

                remoteTraceBuilder.AppendLine(remoteStackTrace);

                if (!alreadyMarkedUp)
                {
                    remoteTraceBuilder.AppendLine(StackTraceMarkers.Literals.EndRemoteTrace);
                }
            }

            if (!String.IsNullOrWhiteSpace(stackTrace))
            {
                StringBuilder stackInfo = new();

                if (!String.IsNullOrWhiteSpace(className))
                {
                    if (stackInfo.Length > 0)
                    {
                        stackInfo.Append("; ");
                    }

                    stackInfo.Append(className);
                }

                if (stackInfo.Length > 0)
                {
                    stackInfo.Insert(0, '(');
                    stackInfo.Append(") ");
                }

                if (remoteTraceBuilder.Length == 0)
                {
                    remoteTraceBuilder.AppendFormat(StackTraceMarkers.Templates.StartRemoteTrace, stackInfo.ToString());
                }
                else
                {
                    remoteTraceBuilder.AppendFormat(StackTraceMarkers.Templates.StartAnotherRemoteTrace, stackInfo.ToString());
                }

                remoteTraceBuilder.AppendLine();
                remoteTraceBuilder.AppendLine(stackTrace);

                remoteTraceBuilder.AppendFormat(StackTraceMarkers.Templates.EndRemoteTrace, stackInfo.ToString());

                if (useNewLineAfterRemoteStack)
                {
                    remoteTraceBuilder.AppendLine();
                }
                                
                stackTrace = String.Empty;
            }

            SerializationInfo mutatedSerializedEx = new(serializedEx.ObjectType, new FormatterConverter());
            SerializationInfoEnumerator serInfoEnum = serializedEx.GetEnumerator();
            while (serInfoEnum.MoveNext())
            {
                SerializationEntry curr = serInfoEnum.Current;

                if (curr.Name.Equals("StackTraceString") && curr.ObjectType == typeof(string))
                {
                    mutatedSerializedEx.AddValue("StackTraceString", stackTrace, typeof(string));
                }
                else if (curr.Name.Equals("RemoteStackTraceString") && curr.ObjectType == typeof(string))
                {
                    mutatedSerializedEx.AddValue("RemoteStackTraceString", remoteTraceBuilder.ToString(), typeof(string));
                }
                else if (curr.Name.Equals("ClassName") && curr.ObjectType == typeof(string))
                {
                    mutatedSerializedEx.AddValue("ClassName", typeof(MySpecialException).FullName, typeof(string));
                }
                else
                {
                    mutatedSerializedEx.AddValue(curr.Name, curr.Value, curr.ObjectType);
                }
            }

            //PrintSerializationInfo(mutatedSerializedEx);

            StreamingContext context = new(StreamingContextStates.CrossMachine);

            MySpecialException ex = new MySpecialException(mutatedSerializedEx, context);
            //MySpecialException ex = new MySpecialException("Some Problematic Issue Test.");
            return ex;
        }

        internal void FuncB3(SerializationInfo serializedEx)
        {
            Exception ex = RehydrateException(serializedEx, formatForBeingWrapped: false);
            //Console.WriteLine($"\n-----------\nException being thrown after rehydration:\n {ex}");

            //ExceptionDispatchInfo.Capture(ex).Throw();
            throw ex;
        }

        internal void FuncC1(Exception ex)
        {
            FuncC2(ex);
        }

        internal void FuncC2(Exception ex)
        {
            FuncC3(ex);
        }

        internal void FuncC3(Exception ex)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
        }

        internal void FuncD1(Exception ex)
        {
            FuncD2(ex);
        }

        internal void FuncD2(Exception ex)
        {
            FuncD3(ex);
        }

        internal void FuncD3(Exception ex)
        {
            if (ex is ITemporalFailure failure)
            {
                RemoteTemporalException.Throw("Temporal error occurred.", failure);
            }
            else
            {
                throw new InvalidTimeZoneException("Non-Temporal error occurred.", ex);
            }
        }

        internal void FuncE1(SerializationInfo serializedEx)
        {
            FuncE2(serializedEx);
        }

        internal void FuncE2(SerializationInfo serializedEx)
        {
            FuncE3(serializedEx);
        }

        internal void FuncE3(SerializationInfo serializedEx)
        {
            Exception ex = RehydrateException(serializedEx, formatForBeingWrapped: true);
            //Console.WriteLine($"\n-----------\nException being wrapped-thrown after rehydration:\n {ex}");

            RemoteTemporalException.Throw("Temporal error occurred on a remote location.", (ITemporalFailure) ex);
        }
    }
}