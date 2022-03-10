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
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            // Create a workflow stub that implements the `IShoppingCart` iface:
            // (The stub will start a new workflow chain and bind to it later, when its main routine is invoked later.)
            IShoppingCart cart = serviceClient.CreateUnboundWorkflowStub<IShoppingCart>("ShoppingCart", "SomeUserCard-ID", "taskQueue");

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

        public static async Task<bool> AddProductToExistingCart_Main(User shopper, Product product)
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            // Get a handle to a EXISTING chain with the workflowId `shopper.UserKey` AND make sure it is running:
            if ((await serviceClient.TryGetWorkflowAsync(shopper.UserKey)).IsSuccess(out IWorkflowChain wfChain)
                    && await wfChain.IsRunningAsync())
            {
                // Get a stub that is bound to the specified chain:
                // (Note that both 'IProductList' and 'IShoppingCart' are valid here.)
                IProductList cart = wfChain.GetStub<IProductList>();  

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
                await cart.AddProductAsync(product);  // This would throw if `cart` was not bound to a workflow chain.
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
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            // Create a new stub for a workflow with workflowTypeName "ShoppingCart", workflowId `shopper.UserKey`etc.:
            // (The stub is NOT bound to any particular chain, until we invoke a main routine.)
            IShoppingCart cart = serviceClient.CreateUnboundWorkflowStub<IShoppingCart>("ShoppingCart", shopper.UserKey, "taskQueue");
             
            return await TryAddShippingInfoIfUserIsShopping_Logic(shippingInfo, cart);
        }

        private static async Task<bool> TryAddShippingInfoIfUserIsShopping_Logic(DeliveryInfo shippingInfo, IShoppingCart cart)
        {
            try
            {
                // Bind the stub to an actual chain (throws if no appropriate chain exists):
                // (any stub can be cast to 'IWorkflowRunStub')

                WorkflowChainStubConfiguration bindConfig = new (canBindToNewChain: false,
                                                                       canBindToExistingRunningChain: true,
                                                                       canBindToExistingFinishedChain: false);

                await ((IWorkflowChainStub) cart).EnsureIsBoundAsync(bindConfig);  

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
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            // Create an unbound stub and specify to what it can be bound later, when the main routine is invoked:
            IShoppingCart cart = serviceClient.CreateUnboundWorkflowStub<IShoppingCart>(
                                                    "ShoppingCart",
                                                    shopper.UserKey,
                                                    "taskQueue",
                                                    new WorkflowChainStubConfiguration(canBindToNewChain: false,
                                                                                             canBindToExistingRunningChain: true,
                                                                                             canBindToExistingFinishedChain: false));
            
            return await PayAndWaitForOrderCompletionIfUserIsShopping_Logic(paymentAmount, cart);
        }

        private static async Task<bool> PayAndWaitForOrderCompletionIfUserIsShopping_Logic(MoneyAmount paymentAmount, IShoppingCart cart)
        {
            Task<OrderConfirmation> order;

            try
            {
                // This will cause the stub to be bound to a chain according to the config specified in `CreateUnboundWorkflowStub(..)`,
                // i.e to an EXISTING and RUNNING chain. If no such chain is found, this will throw.
                // Because we only bind to a readily running chain, the input parameters will be ignored, so we can just specify null.
                order = cart.ShopAsync(null);                
            }
            catch (NeedsDesignException)
            {
                return false;  // "Could not apply shipping info. Does user have an active shopping cart?
            }

            // Wait for the main routine (i.e. the entire workflow chain) to complete:
            await cart.ApplyPaymentAsync(paymentAmount);
            
            OrderConfirmation confirmation = await order;
            Console.WriteLine($"Order \"{confirmation.OrderId}\" was placed.");

            return true;
        }

        public static async Task<bool> AddProductToExistingCart2_Main(User shopper, Product product)
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            // Create an unbound stub, specify to what it can be bound later when the main routine is invoked,
            // and specify options for the underlying client:
            IShoppingCart cart = serviceClient.CreateUnboundWorkflowStub<IShoppingCart>(
                                                    "ShoppingCart",
                                                    shopper.UserKey,
                                                    "taskQueue",
                                                    StartWorkflowChainConfiguration.Default,
                                                    new WorkflowChainStubConfiguration(canBindToNewChain: false,
                                                                                             canBindToExistingRunningChain: true,
                                                                                             canBindToExistingFinishedChain: false));

            return await AddProductToExistingCart2_Logic(product, cart);
        }

        private static async Task<bool> AddProductToExistingCart2_Logic(Product product, IShoppingCart cart)
        {
            // Force binding to an existing workflow run:
            try
            {
                // Binding will only succeed for an EXISTING and RUNNING cart, based on the settings specified when stub was created.
                await ((IWorkflowChainStub) cart).EnsureIsBoundAsync();
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
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "Shopping" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            IShoppingCart cart = serviceClient.CreateUnboundWorkflowStub<IShoppingCart>(
                                                    "ShoppingCart",
                                                    shopper.UserKey,
                                                    "taskQueue",
                                                    new WorkflowChainStubConfiguration(canBindToNewChain: true,
                                                                                             canBindToExistingRunningChain: true,
                                                                                             canBindToExistingFinishedChain: false));


            await AddProductToNewOrExistingCart_Logic(shopper, product, cart);
        }

        private static async Task AddProductToNewOrExistingCart_Logic(User shopper, Product product, IShoppingCart cart)
        {
            // Based on the config passed to `CreateUnboundWorkflowStub(..)`, this will bind to a new or an existing chain.
            // We must pass a valid input parameter in case a new chain is started.
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
