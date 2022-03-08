using System;
using System.Threading.Tasks;
using Temporal.Common.WorkflowConfiguration;
using Temporal.WorkflowClient;

using static Temporal.Sdk.BasicSamples.Part1_5_AttributesAndInterfaces;

namespace Temporal.Sdk.BasicSamples
{
    public class Part2_2_IfaceBasedClient
    {        
        public static async Task Minimal(string[] _)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient();

            // Create a workflow stub that implements the `IShoppingCart` iface:
            // (The stub will start a new workflow consecution and bind to it later, when its main routine is invoked later.)
            IShoppingCart cart = serviceClient.CreateUnboundWorkflowStub<IShoppingCart>("ShoppingCart", "SomeUserCard-ID", "taskQueue");

            // Start the workflow consecution by invoking the stub's main routine:
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

        public static async Task<bool> AddProductToExistingCart_Main(User shopper, Product product)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "Shopping" });

            // Get a handle to a EXISTING consecution with the workflowId `shopper.UserKey` AND make sure it is running:
            if ((await serviceClient.TryGetWorkflowAsync(shopper.UserKey)).IsSuccess(out IWorkflowConsecution wfConsecution)
                    && await wfConsecution.IsRunningAsync())
            {
                // Get a stub that is bound to the specified consecution:
                // (Note that both 'IProductList' and 'IShoppingCart' are valid here.)
                IProductList cart = wfConsecution.GetStub<IProductList>();  

                return await AddProductToExistingCart_Logic(product, cart);
            }
            else
            {
                return false;
            }
        }

        private static async Task<bool> AddProductToExistingCart_Logic(Product product, IProductList cart)
        {
            try
            {
                await cart.AddProductAsync(product);  // This would throw if `cart` was not bound to a workflow consecution.
                MoneyAmount total = await cart.GetTotalAsync();

                Console.WriteLine($"Item \"{product.Name}\" added to cart. New total is: ${total.Dollars}.{total.Cents}.");
                return true;
            }
            catch (NeedsDesignException)
            {
                return false;
            }
        }

        public static async Task<bool> TryAddShippingInfoIfUserIsShopping_Main(User shopper, DeliveryInfo shippingInfo)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "Shopping" });

            // Create a new stub for a workflow with workflowTypeName "ShoppingCart", workflowId `shopper.UserKey`etc.:
            // (The stub is NOT bound to any particular consecution, until we invoke a main routine.)
            IShoppingCart cart = serviceClient.CreateUnboundWorkflowStub<IShoppingCart>("ShoppingCart", shopper.UserKey, "taskQueue");
             
            return await TryAddShippingInfoIfUserIsShopping_Logic(shippingInfo, cart);
        }

        private static async Task<bool> TryAddShippingInfoIfUserIsShopping_Logic(DeliveryInfo shippingInfo, IShoppingCart cart)
        {
            try
            {
                // Bind the stub to an actual consecution (throws if no appropriate consecution exists):
                // (any stub can be cast to 'IWorkflowRunStub')

                WorkflowConsecutionStubConfiguration bindConfig = new (canBindToNewConsecution: false,
                                                                       canBindToExistingRunningConsecution: true,
                                                                       canBindToExistingFinishedConsecution: false);

                await ((IWorkflowConsecutionStub) cart).EnsureIsBoundAsync(bindConfig);  

                await cart.SetDeliveryInfoAsync(shippingInfo); 
                return true;  // Shipping info applied.
            }
            catch (NeedsDesignException)
            {
                return false;  // "Could not apply shipping info. Does user have an active shopping cart?
            }
        }

        public static async Task<bool> PayAndWaitForOrderCompletionIfUserIsShopping_Main(User shopper, MoneyAmount paymentAmount)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "Shopping" });

            // Create an unbound stub and specify to what it can be bound later, when the main routine is invoked:
            IShoppingCart cart = serviceClient.CreateUnboundWorkflowStub<IShoppingCart>(
                                                    "ShoppingCart",
                                                    shopper.UserKey,
                                                    "taskQueue",
                                                    new WorkflowConsecutionStubConfiguration(canBindToNewConsecution: false,
                                                                                             canBindToExistingRunningConsecution: true,
                                                                                             canBindToExistingFinishedConsecution: false));
            
            return await PayAndWaitForOrderCompletionIfUserIsShopping_Logic(paymentAmount, cart);
        }

        private static async Task<bool> PayAndWaitForOrderCompletionIfUserIsShopping_Logic(MoneyAmount paymentAmount, IShoppingCart cart)
        {
            Task<OrderConfirmation> order;

            try
            {
                // This will cause the stub to be bound to a consecution according to the config specified in `CreateUnboundWorkflowStub(..)`,
                // i.e to an EXISTING and RUNNING consecution. If no such consecution is found, this will throw.
                // Because we only bind to a readily running consecution, the input parameters will be ignored, so we can just specify null.
                order = cart.ShopAsync(null);                
            }
            catch (NeedsDesignException)
            {
                return false;  // "Could not apply shipping info. Does user have an active shopping cart?
            }

            // Wait for the main routine (i.e. the entire workflow consecution) to complete:
            await cart.ApplyPaymentAsync(paymentAmount);
            
            OrderConfirmation confirmation = await order;
            Console.WriteLine($"Order \"{confirmation.OrderId}\" was placed.");

            return true;
        }

        public static async Task<bool> AddProductToExistingCart2_Main(User shopper, Product product)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "Shopping" });

            // Create an unbound stub, specify to what it can be bound later when the main routine is invoked,
            // and specify options for the underlying client:
            IShoppingCart cart = serviceClient.CreateUnboundWorkflowStub<IShoppingCart>(
                                                    "ShoppingCart",
                                                    shopper.UserKey,
                                                    new WorkflowExecutionConfiguration() { TaskQueue = "taskQueue" },
                                                    new WorkflowConsecutionStubConfiguration(canBindToNewConsecution: false,
                                                                                             canBindToExistingRunningConsecution: true,
                                                                                             canBindToExistingFinishedConsecution: false),
                                                    new WorkflowConsecutionClientConfiguration());

            return await AddProductToExistingCart2_Logic(product, cart);
        }

        private static async Task<bool> AddProductToExistingCart2_Logic(Product product, IShoppingCart cart)
        {
            // Force binding to an existing workflow run:
            try
            {
                // Binding will only succeed for an EXISTING and RUNNING cart, based on the settings specified when stub was created.
                await ((IWorkflowConsecutionStub) cart).EnsureIsBoundAsync();
            }
            catch (NeedsDesignException)
            {
                Console.WriteLine("Cart is new. Will not add product.");
                return false;
            }
            
            Console.WriteLine("Cart is already active. Current items:");
            Products cartItems = await cart.GetProductsAsync();
            foreach(Product p in cartItems.Collection)
            {
                Console.WriteLine($"    {p.Name}: ${p.Price.Dollars}.{p.Price.Cents}");
            }

            await cart.AddProductAsync(product);
            MoneyAmount total = await cart.GetTotalAsync();

            Console.WriteLine($"Item \"{product.Name}\" added to cart. New total is: ${total.Dollars}.{total.Cents}.");
            return true;
        }

        public static async Task AddProductToNewOrExistingCart_Main(User shopper, Product product)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "Shopping" });

            IShoppingCart cart = serviceClient.CreateUnboundWorkflowStub<IShoppingCart>(
                                                    "ShoppingCart",
                                                    shopper.UserKey,
                                                    "taskQueue",
                                                    new WorkflowConsecutionStubConfiguration(canBindToNewConsecution: true,
                                                                                             canBindToExistingRunningConsecution: true,
                                                                                             canBindToExistingFinishedConsecution: false));


            await AddProductToNewOrExistingCart_Logic(shopper, product, cart);
        }

        private static async Task AddProductToNewOrExistingCart_Logic(User shopper, Product product, IShoppingCart cart)
        {
            // Based on the config passed to `CreateUnboundWorkflowStub(..)`, this will bind to a new or an existing consecution.
            // We must pass a valid input parameter in case a new consecution is started.
            await cart.ShopAsync(shopper);  

            Console.WriteLine("Current items:");
            Products cartItems = await cart.GetProductsAsync();
            foreach (Product p in cartItems.Collection)
            {
                Console.WriteLine($"    {p.Name}: ${p.Price.Dollars}.{p.Price.Cents}");
            }

            await cart.AddProductAsync(product);
            MoneyAmount total = await cart.GetTotalAsync();

            Console.WriteLine($"Item \"{product.Name}\" added to cart. New total is: ${total.Dollars}.{total.Cents}.");
        }
    }
}
