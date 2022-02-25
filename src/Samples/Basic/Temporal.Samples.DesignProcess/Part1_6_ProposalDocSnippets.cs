//using System;
//using System.Threading.Tasks;

//using Microsoft.Extensions.Hosting;

//using Temporal.Common.DataModel;
//using Temporal.Worker.Activities;
//using Temporal.Worker.Hosting;
//using Temporal.Worker.Workflows;

//namespace Temporal.Sdk.BasicSamples
//{
//    public class Part1_6_ProposalDocSnippets
//    {


//        // [Workflow(runMethod: nameof(RunAsync))]
//        // public class ProcessCustomer
//        // {
//        //     public async Task RunAsync(Customer customerInfo, IWorkflowContext workflowCtx)
//        //     {
//        //         string fullName = $"{customerInfo.FirstName} {customerInfo.LastName}";

//        //         PayloadsCollection payload = workflowCtx.GetSerializer().Serialize(fullName);
//        //         await workflowCtx.Activities.ExecuteAsync("DisplayCustomerGreeting", payload);

//        //         string cId = GetCustomerId(customerInfo);

//        //         // Invoke a .NET activity:
//        //         CustomerRating rating1 = await workflowCtx.Activities.ExecuteAsync<CustomerId, CustomerRating>("ReadCustomerRating1", new CustomerId(cId));

//        //         // Invoke an equivalent activity implemented in another language:
//        //         PayloadsCollection cIdPayload = workflowCtx.GetSerializer().Serialize(cId);
//        //         PayloadsCollection ratingPayload = await workflowCtx.Activities.ExecuteAsync("ReadCustomerRating2", cIdPayload);
//        //         CustomerRating rating2 = workflowCtx.GetSerializer(ratingPayload).Deserialize<CustomerRating>(ratingPayload);
//        //     }

//        //     private string GetCustomerId(Customer _)
//        //     {
//        //         return "The appropriate customer id";
//        //     }
//        // }
                
//        // public record Customer(string FirstName, string LastName, string NickName) : IDataValue;
//        // public record CustomerId(string Value) : IDataValue;
//        // public record CustomerRating(int Value) : IDataValue;


//        [Workflow(runMethod: nameof(MainAsync))]
//        public class SomeWorkflow
//        {
//            private bool _isConditionMet = false;
//            private int _countSomeActivityCompletions = 0;

//            public async Task MainAsync(IWorkflowContext workflowCtx)
//            {
//                while (! _isConditionMet)
//                {
//                    await workflowCtx.Activities.ExecuteAsync("SomeActivity");
//                    _countSomeActivityCompletions++;
//                }                                
//            }
        
//            [WorkflowSignalHandler]
//            public void NotifyConditionMet()
//            {
//                _isConditionMet = true;
//            }

//            [WorkflowQueryHandler]
//            public SomeActivityStats CountSomeActivityCompletions()
//            {
//                return new SomeActivityStats(_countSomeActivityCompletions);
//            }
//        }

//        //public record SomeActivityStats(int CountCompletions) : IDataValue;

//[Workflow(runMethod: nameof(MainBaseAsync), WorkflowTypeName = "SomeBaseWorkflow")]
//public class SomeWorkflowB
//{          
//    public virtual async Task MainBaseAsync()
//    {
//        // ...
//    }                
//}

//[Workflow(runMethod: nameof(SomeWorkflowB.MainBaseAsync))]
//public class SomeWorkflowD : SomeWorkflowB
//{          
//    public virtual async Task MainDerivedAsync()
//    {
//        // ...
//    }                
//}

////[Workflow(runMethod: "Task MainAsync(Parameters, IWorkflowContext)")]
//[Workflow(runMethod: "System.Threading.Tasks.Task MainAsync(Temporal.Sdk.BasicSamples.Parameters, Temporal.Worker.Workflows.IWorkflowContext)")]
//public class SomeOtherWorkflow
//{    
//    public Task<TResult> MainAsync(IWorkflowContext workflowCtx)
//    {
//        // ...
//    }

//    public Task MainAsync(Parameters input, IWorkflowContext workflowCtx)
//    {
//        // ...
//    }
//}

//public interface IDemo<TArg>
//{
//Task HandleSignalAsync();
//Task HandleSignalAsync(IWorkflowContext workflowCtx);
//Task HandleSignalAsync(TArg handlerArgs, IWorkflowContext workflowCtx) where TArg : IDataValue;
//Task HandleSignalAsync(PayloadsCollection handlerArgs, IWorkflowContext workflowCtx) where TArg : IDataValue;
//void HandleSignal();
//void HandleSignal(IWorkflowContext workflowCtx);
//void HandleSignal(TArg handlerArgs, IWorkflowContext workflowCtx) where TArg : IDataValue;
//void HandleSignal(PayloadsCollection handlerArgs, IWorkflowContext workflowCtx);
//}

//        public record Parameters(string Value) : IDataValue;

//        /// <summary>
//        /// Parameters to workflow APIs (main method, signal & query parameters) and to activities must implement <see cref="IDataValue" />.
//        /// In some specialized cases where it is not possible, the raw (non-deserialized) payload may be accessed
//        /// directly (e.g. <see cref="Part4_1_BasicWorkflowUsage" /> and <see cref="Part4_2_BasicWorkflowUsage_MultipleWorkers" />).
//        /// </summary>
//        public class SpeechRequest : IDataValue
//        {
//            public SpeechRequest(string text)
//            {
//                Text = text;
//            }

//            public string Text
//            {
//                get; set;
//            }
//        }


//        /// <summary>A sample activity implementation.</summary>
//        public static class Speak
//        {
//            public static Task GreetingAsync(SpeechRequest input, WorkflowActivityContext activityCtx)
//            {
//                Console.WriteLine($"[{activityCtx.ActivityTypeName}] {input.Text}");
//                return Task.CompletedTask;
//            }
//        }

//        public static void Main(string[] args)
//        {     
//            IHost appHost = Host.CreateDefaultBuilder(args)
//                    .UseTemporalWorkerHost()
//                    .ConfigureServices(serviceCollection =>
//                    {
//                        serviceCollection.AddTemporalWorker()
//                                .Configure(temporalWorkerConfig =>
//                                {
//                                    temporalWorkerConfig.TaskQueue = "SomeTaskQueue";
//                                });
        
//                        serviceCollection.AddWorkflowWithAttributes<SomeWorkflow>();
//                    })
//                    .Build();
        
//            appHost.Run();
//        }
//    }
//}
