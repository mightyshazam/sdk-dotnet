using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Temporal.Common.DataModel;
using Temporal.Common.WorkflowConfiguration;
using Temporal.Worker.Activities;
using Temporal.Worker.Workflows;
using Temporal.Worker.Workflows.Base;

namespace Temporal.Worker.Hosting
{
    public class HostingApi
    {
    }

    // ----------- -----------

    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseTemporalWorkerHost(this IHostBuilder hostBuilder)
        {
            return hostBuilder;
        }

        public static IHostBuilder UseTemporalWorkerHost(this IHostBuilder hostBuilder,
                                                         Action<TemporalServiceConfiguration> configureTemporalService)
        {
            return hostBuilder;
        }

        public static IHostBuilder UseTemporalWorkerHost(this IHostBuilder hostBuilder,
                                                         Action<HostBuilderContext, TemporalServiceConfiguration> configureTemporalService)
        {
            return hostBuilder;
        }

        public static IHostBuilder UseTemporalWorkerHost(this IHostBuilder hostBuilder,
                                                         Action<TemporalServiceConfiguration,
                                                                TemporalWorkerConfiguration,
                                                                WorkflowExecutionConfiguration,
                                                                WorkflowImplementationConfiguration> configureDefaults)
        {
            return hostBuilder;
        }

        public static IHostBuilder UseTemporalWorkerHost(this IHostBuilder hostBuilder,
                                                        Action<HostBuilderContext,
                                                               TemporalServiceConfiguration,
                                                               TemporalWorkerConfiguration,
                                                               WorkflowExecutionConfiguration,
                                                               WorkflowImplementationConfiguration> configureDefaults)
        {
            return hostBuilder;
        }
    }

    public interface ITemporalServiceConfiguration
    {
        string OrchestratorServiceUrl { get; }
        string Namespace { get; }
    }

    public class TemporalServiceConfiguration : ITemporalServiceConfiguration
    {
        public string OrchestratorServiceUrl { get; set; }
        public string Namespace { get; set; }
    }

    // ----------- -----------

    public static class ServiceCollectionExtensions
    {
        public static WorkerRegistration AddTemporalWorker(this IServiceCollection serviceCollection)
        { return null; }        

        public static WorkflowRegistration AddWorkflowWithOverrides<TWorkflowImplementation>(this IServiceCollection serviceCollection)
                where TWorkflowImplementation : class, IBasicWorkflow
        { return null; }

        public static WorkflowRegistration AddWorkflowWithAttributes<TWorkflowImplementation>(this IServiceCollection serviceCollection)
                where TWorkflowImplementation : class
        { return null; }

        public static ActivityRegistration AddActivity<TActivityImplementation>(this IServiceCollection serviceCollection)
                where TActivityImplementation : class, IBasicActivity
        { return null; }

        public static ActivityRegistration AddActivity(this IServiceCollection serviceCollection,
                                                       string activityTypeName,
                                                       Func<WorkflowActivityContext, Task> activityImplementation)
        { return null; }

        public static ActivityRegistration AddActivity(this IServiceCollection serviceCollection,
                                                       string activityTypeName,
                                                       Func<IServiceProvider, Func<WorkflowActivityContext, Task>> activityImplementationFactory)
        { return null; }

        public static ActivityRegistration AddActivity(this IServiceCollection serviceCollection,
                                                       string activityTypeName,
                                                       Action<WorkflowActivityContext> activityImplementation)
        { return null; }

        public static ActivityRegistration AddActivity(this IServiceCollection serviceCollection,
                                                       string activityTypeName,
                                                       Func<IServiceProvider, Action<WorkflowActivityContext>> activityImplementationFactory)
        { return null; }

        public static ActivityRegistration AddActivity<TArg>(this IServiceCollection serviceCollection,
                                                             string activityTypeName,
                                                             Func<TArg, WorkflowActivityContext, Task> activityImplementation)
                                            where TArg : IDataValue
        { return null; }

        public static ActivityRegistration AddActivity<TArg>(this IServiceCollection serviceCollection,
                                                             string activityTypeName,
                                                             Func<IServiceProvider, Func<TArg, WorkflowActivityContext, Task>> activityImplementationFactory)
                                            where TArg : IDataValue
        { return null; }

        public static ActivityRegistration AddActivity<TArg>(this IServiceCollection serviceCollection,
                                                             string activityTypeName,
                                                             Action<TArg, WorkflowActivityContext> activityImplementation)
                                            where TArg : IDataValue
        { return null; }

        public static ActivityRegistration AddActivity<TArg>(this IServiceCollection serviceCollection,
                                                             string activityTypeName,
                                                             Func<IServiceProvider, Action<TArg, WorkflowActivityContext>> activityImplementationFactory)
                                            where TArg : IDataValue
        { return null; }

        public static ActivityRegistration AddActivity<TResult>(this IServiceCollection serviceCollection,
                                                                string activityTypeName,
                                                                Func<WorkflowActivityContext, Task<TResult>> activityImplementation)
                                            where TResult : IDataValue
        { return null; }

        public static ActivityRegistration AddActivity<TResult>(this IServiceCollection serviceCollection,
                                                                string activityTypeName,
                                                                Func<IServiceProvider, Func<WorkflowActivityContext, Task<TResult>>> activityImplementationFactory)
                                            where TResult : IDataValue
        { return null; }

        public static ActivityRegistration AddActivity<TArg, TResult>(this IServiceCollection serviceCollection,
                                                                      string activityTypeName,
                                                                      Func<TArg, WorkflowActivityContext, Task<TResult>> activityImplementation)
                                            where TArg : IDataValue where TResult : IDataValue
        { return null; }

        public static ActivityRegistration AddActivity<TArg, TResult>(this IServiceCollection serviceCollection,
                                                                      string activityTypeName,
                                                                      Func<IServiceProvider, Func<TArg, WorkflowActivityContext, Task<TResult>>> activityImplementationFactory)
                                            where TArg : IDataValue where TResult : IDataValue
        { return null; }
    }

    public class WorkerRegistration
    {
        public WorkerRegistration Configure(Action<TemporalWorkerConfiguration> configurator) { return this; }
        public WorkerRegistration Configure(Action<IServiceProvider, TemporalWorkerConfiguration> configurator) { return this; }
    }

    public class WorkflowRegistration
    {
        public WorkflowRegistration ConfigureExecution(Action<WorkflowExecutionConfiguration> configurator) { return this; }
        public WorkflowRegistration ConfigureExecution(Action<IServiceProvider, WorkflowExecutionConfiguration> configurator) { return this; }
        public WorkflowRegistration ConfigureImplementation(Action<WorkflowImplementationConfiguration> configurator) { return this; }
        public WorkflowRegistration ConfigureImplementation(Action<IServiceProvider, WorkflowImplementationConfiguration> configurator) { return this; }
        public WorkflowRegistration AssignWorker(WorkerRegistration workerRegistration) { return this; }
    }

    public class ActivityRegistration
    {        
        public ActivityRegistration AssignWorker(WorkerRegistration workerRegistration) { return this; }
    }

    // ----------- -----------

    public interface ITemporalWorkerConfiguration
    {
        string TaskQueue { get; }
        int CachedStickyWorkflowsMax { get; }
        bool EnablePollForActivities { get; }
        IQueuePollingConfiguration NonStickyQueue { get; }
        IStickyQueuePollingConfiguration StickyQueue { get; }        
    }

    public class TemporalWorkerConfiguration : ITemporalWorkerConfiguration
    {
        public string TaskQueue { get; set; }
        public int CachedStickyWorkflowsMax { get; set; }
        public bool EnablePollForActivities { get; set; }
        public QueuePollingConfiguration NonStickyQueue { get; set; }
        public StickyQueuePollingConfiguration StickyQueue { get; set; }
        IQueuePollingConfiguration ITemporalWorkerConfiguration.NonStickyQueue { get { return this.NonStickyQueue; } }
        IStickyQueuePollingConfiguration ITemporalWorkerConfiguration.StickyQueue { get { return this.StickyQueue; } }
    }

    public interface IQueuePollingConfiguration
    {
        int ConcurrentWorkflowTaskPollsMax { get; }
        int ConcurrentActivityTaskPollsMax { get; }
    }

    public class QueuePollingConfiguration : IQueuePollingConfiguration
    {
        public int ConcurrentWorkflowTaskPollsMax { get; set; }
        public int ConcurrentActivityTaskPollsMax { get; set; }
    }

    public interface IStickyQueuePollingConfiguration : IQueuePollingConfiguration
    {
        int ScheduleToStartTimeoutMillisecs { get; }
    }

    public class StickyQueuePollingConfiguration : QueuePollingConfiguration, IStickyQueuePollingConfiguration
    {
        public int ScheduleToStartTimeoutMillisecs { get; set; }
    }
}
