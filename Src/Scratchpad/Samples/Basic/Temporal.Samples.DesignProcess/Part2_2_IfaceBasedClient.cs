using System;
using System.Threading.Tasks;
using Temporal.Common.DataModel;
using Temporal.Common.WorkflowConfiguration;
using Temporal.WorkflowClient;

using static Temporal.Sdk.BasicSamples.Part1_5_AttributesAndInterfaces;

namespace Temporal.Sdk.BasicSamples
{
    public class Part2_2_IfaceBasedClient
    {        
        public static async Task Minimal(string[] _)
        {
            TemporalClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalClient client = await TemporalClient.ConnectAsync(serviceConfig);

            // Create a workflow stub that implements the `IShoppingCart` iface:
            // (The stub will start a new workflow chain and bind to it later, when its main routine is invoked later.)
            IShoppingCart cart = client.CreateWorkflowStub<IShoppingCart>("SomeUserCard-ID", "ShoppingCart", "taskQueue");

            // Start the workflow chain by invoking the stub's main routine:
            Task<OrderConfirmation> order = cart.ShopAsync(new User("Jean-Luc", "Picard", Guid.NewGuid()));
            
            // Interact with the workflow by sending signals:
            await cart.AddProductAsync(new Product("Starship Model", new MoneyAmount(42, 99)));
            await cart.SetDeliveryInfoAsync(new DeliveryInfo("11 Headquarters Street, San Francisco"));

            // Execute a workflow query and send another signal:
            MoneyAmount price = (await cart.TryGetTotalWithTaxAsync()).Result;
            await cart.ApplyPaymentAsync(price);

            // Await the completion of the workflow:
            OrderConfirmation confirmation = await order;

            // Use the workflow result:
            Console.WriteLine($" Order \"{confirmation.OrderId}\" was placed at {confirmation.TimestampUtc}.");
        }

        public static async Task<bool> TryAddShippingInfoToExistingCart_Main(User shopper, DeliveryInfo shippingInfo)
        {
            TemporalClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalClient client = await TemporalClient.ConnectAsync(serviceConfig);

            // Create a new stub for the latest workflow with workflowId `shopper.UserKey`:
            // (The stub is NOT bound to any particular chain, until we invoke any method.)
            // (Note that both 'IProductList' and 'IShoppingCart' are valid here, but we need 'IShoppingCart' later.)
            IShoppingCart cart = client.CreateWorkflowStub<IShoppingCart>(shopper.UserKey);

            return await TryAddShippingInfoToExistingCart_Logic(shippingInfo, cart);
        }

        private static async Task<bool> TryAddShippingInfoToExistingCart_Logic(DeliveryInfo shippingInfo, IShoppingCart cart)
        {
            try
            {
                // Invoke a stub method (representing a signal). This will bind the stub or throw if there is nothing to bind to.
                await cart.SetDeliveryInfoAsync(shippingInfo);
                return true;  // Shipping info applied.
            }
            catch (NeedsDesignException)
            {
                return false;  // "Could not apply shipping info. Does user have an active shopping cart?
            }
        }

        public static async Task<bool> AddProductToCartIfUserIsShopping_Main(User shopper, Product product)
        {
            TemporalClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalClient client = await TemporalClient.ConnectAsync(serviceConfig);

            // Get a handle to a EXISTING chain with the workflowId `shopper.UserKey` AND make sure it is running:
            IWorkflowChain wfChain = client.CreateWorkflowHandle(shopper.UserKey);
            if (WorkflowExecutionStatus.Running == await wfChain.GetStatusAsync())
            {
                // Get a stub that is bound to the specified chain:
                // (Note that both 'IProductList' and 'IShoppingCart' are valid here.)
                IProductList cart = wfChain.GetStub<IProductList>();  

                return await AddProductToCartIfUserIsShopping_Logic(product, cart);
            }
            else
            {
                return false;
            }
        }

        private static async Task<bool> AddProductToCartIfUserIsShopping_Logic(Product product, IProductList cart)
        {
            try
            {
                await cart.AddProductAsync(product);
                MoneyAmount total = await cart.GetTotalAsync();

                Console.WriteLine($"Item \"{product.Name}\" added to cart. New total is: ${total.Dollars}.{total.Cents}.");
                return true;
            }
            catch (NeedsDesignException)
            {
                return false;
            }
        }

        public static async Task<bool> PayAndWaitForOrderCompletionIfUserIsShopping_Main(User shopper, MoneyAmount paymentAmount)
        {
            TemporalClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalClient client = await TemporalClient.ConnectAsync(serviceConfig);

            // Create an unbound stub using `WorkflowChainBindingConfiguration.Strategy.LatestChain`:
            IShoppingCart cart = client.CreateWorkflowStub<IShoppingCart>(shopper.UserKey);
            
            return await PayAndWaitForOrderCompletionIfUserIsShopping_Logic(paymentAmount, cart);
        }

        private static async Task<bool> PayAndWaitForOrderCompletionIfUserIsShopping_Logic(MoneyAmount paymentAmount, IShoppingCart cart)
        {
            try
            {
                // Invoke the signal behind this stub method and bind to the chain that will receive the signal (most recent chain).
                // This will throw if there is no chain to bind to.
                await cart.ApplyPaymentAsync(paymentAmount);

                // Wait for the workflow to finish and get the result. 
                // Note that `GetOrderAsync()` is a `WorkflowMainMethodStub` with `WorkflowMainMethodStubInvocationPolicy.GetResult`,
                // so it will get the result of the chain that the stub was bound to earlier.
                OrderConfirmation confirmation = await cart.GetOrderAsync();
                Console.WriteLine($"Order \"{confirmation.OrderId}\" was placed.");

                return true;
            }
            catch (NeedsDesignException)
            {
                return false;  // "Could not apply shipping info. Does user have an active shopping cart?
            }
        }

        public static async Task AddProductToNewOrExistingCart_Main(User shopper, Product product)
        {
            TemporalClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalClient client = await TemporalClient.ConnectAsync(serviceConfig);

            // Create an unbound stub, that can bing to a new (preferred) or an existing workflow.
            // Binding occurs when an appropriate main routine stub is invoked.
            IShoppingCart cart = client.CreateWorkflowStub<IShoppingCart>(shopper.UserKey,
                                                                          "ShoppingCart",
                                                                          "taskQueue");
            await AddProductToNewOrExistingCart_Logic(shopper, product, cart);
        }

        private static async Task AddProductToNewOrExistingCart_Logic(User shopper, Product product, IShoppingCart cart)
        {
            // Start and bind to a new cart if no cart is active or bind to an existing active cart otherwise:
            Task<OrderConfirmation> cartCompletion = cart.ShopAsync(shopper);
            
            Console.WriteLine("Current cart items:");
            Products cartItems = await cart.GetProductsAsync();
            foreach(Product p in cartItems.Collection)
            {
                Console.WriteLine($"    {p.Name}: ${p.Price.Dollars}.{p.Price.Cents}");
            }

            await cart.AddProductAsync(product);
            MoneyAmount total = await cart.GetTotalAsync();

            Console.WriteLine($"Item \"{product.Name}\" added to cart. New total is: ${total.Dollars}.{total.Cents}.");

            Console.WriteLine($"The user has {(cartCompletion.IsCompleted ? "" : "not")} completed their shopping.");
        }

        public static async Task AddProductToNewCart_Main(User shopper, Product product)
        {
            TemporalClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalClient client = await TemporalClient.ConnectAsync(serviceConfig);

            // Create an unbound stub, that can bing to a new workflow only.
            // Binding occurs when an appropriate main routine stub is invoked.
            IShoppingCart cart = client.CreateWorkflowStub<IShoppingCart>(WorkflowChainBindingPolicy.NewChainOnly,
                                                                          shopper.UserKey,
                                                                          "ShoppingCart",
                                                                          "taskQueue");
            await AddProductToNewCart_Logic(shopper, product, cart);
        }

        private static async Task AddProductToNewCart_Logic(User shopper, Product product, IShoppingCart cart)
        {
            // Note that the `WorkflowMainMethodStub` attribute on this method
            // has `WorkflowMainMethodStubInvocationPolicy.StartNewOrGetResult`.
            // However, when this stub was created, `WorkflowChainBindingPolicy.NewChainOnly` was specified.
            // So if new chain creation will fail, this stub call will not fall back to an existing chain and will throw instead.
            Task<OrderConfirmation> cartCompletion = cart.ShopAsync(shopper);  

            Console.WriteLine("Current items:");
            Products cartItems = await cart.GetProductsAsync();
            foreach (Product p in cartItems.Collection)
            {
                Console.WriteLine($"    {p.Name}: ${p.Price.Dollars}.{p.Price.Cents}");
            }

            await cart.AddProductAsync(product);
            MoneyAmount total = await cart.GetTotalAsync();

            Console.WriteLine($"Item \"{product.Name}\" added to cart. New total is: ${total.Dollars}.{total.Cents}.");
            Console.WriteLine($"The user has {(cartCompletion.IsCompleted ? "" : "not")} completed their shopping.");
        }

        public static async Task GetStatusOfStubsUnderlyingChain_Main(IShoppingCart cartStub)
        {
            if (cartStub == null)
            {
                Console.WriteLine($"The specified {nameof(IShoppingCart)}-stub is null.");
                return;
            }

            if (cartStub is not IWorkflowChainStub stubInfo)
            {
                Console.WriteLine($"The specified {nameof(IShoppingCart)}-stub cannot be cast to {nameof(IWorkflowChainStub)}."
                                + $" However, every generated workflow stub must be cast-able to {nameof(IWorkflowChainStub)}."
                                + $" There must be a bug in the SDK.");
                return;
            }

            if (!stubInfo.TryGetWorkflow(out IWorkflowChain workflowChainHandle))
            {
                Console.WriteLine($"The specified {nameof(IShoppingCart)}-stub is not bound to a workflow chain.");
                return;
            }

            Console.WriteLine($"The specified {nameof(IShoppingCart)}-stub is bound to a workflow chain with"
                            + $" the status \"{await workflowChainHandle.GetStatusAsync()}\".");
        }
    }
}
