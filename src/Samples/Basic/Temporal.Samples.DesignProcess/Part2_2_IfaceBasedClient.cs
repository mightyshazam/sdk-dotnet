using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Temporal.WorkflowClient;

using static Temporal.Sdk.BasicSamples.Part1_5_AttributesAndInterfaces;

namespace Temporal.Sdk.BasicSamples
{
    public class Part2_2_IfaceBasedClient
    {        
        public static async Task Minimal(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new();
            TemporalServiceNamespaceClient serviceClient = await (new TemporalServiceClient(serviceConfig)).GetNamespaceClientAsync();

            // Create a new workflow and obtain a workflow run stub that implements the `IShoppingCart` iface:
            IShoppingCart cart = serviceClient.GetNewWorkflow("ShoppingCart").GetRunStub<IShoppingCart>("taskQueue");

            // Start the workflow by invoking its main routine:
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

        /// <summary>Convenience method for obtaining a <c>TemporalServiceNamespaceClient</c> for the 
        /// namespace "Shopping" (reused in subsequent samples).</summary>
        private static async Task<TemporalServiceNamespaceClient> GetShoppingNamespaceClientAsync()
        {
            TemporalServiceClientConfiguration serviceConfig = new();
            TemporalServiceClient serviceClient = new(serviceConfig);
            return await serviceClient.GetNamespaceClientAsync("Shopping");
        }

        public static async Task<bool> AddProductToExistingCart_Main(User shopper, Product product)
        {
            TemporalServiceNamespaceClient client = await GetShoppingNamespaceClientAsync();

            Workflow userVisits = await client.GetWorkflowAsync("ShoppingCart", shopper.UserKey);

            if ((await userVisits.TryGetLatestRunAsync()).IsSuccess(out WorkflowRun wfRun))
            {
                IProductList cart = wfRun.GetStub<IProductList>();  // Note that both 'IProductList' and 'IShoppingCart' are valid here.
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
                await cart.AddProductAsync(product);  // Will throw if no active run available for binding.
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
            TemporalServiceNamespaceClient client = await GetShoppingNamespaceClientAsync(); // Defined in previous sample

            Workflow userVisits = await client.GetWorkflowAsync("ShoppingCart", shopper.UserKey);
            IShoppingCart cart = (await userVisits.GetLatestRunAsync()).GetStub<IShoppingCart>();

            return await TryAddShippingInfoIfUserIsShopping_Logic(shippingInfo, cart);
        }

        private static async Task<bool> TryAddShippingInfoIfUserIsShopping_Logic(DeliveryInfo shippingInfo, IShoppingCart cart)
        {
            try
            {
                Task<OrderConfirmation> _ = cart.ContinueShoppingAsync(); // Will throw if no active run available for binding.
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
            TemporalServiceNamespaceClient client = await GetShoppingNamespaceClientAsync();

            Workflow userVisits = await client.GetWorkflowAsync("ShoppingCart", shopper.UserKey);
            IShoppingCart cart = userVisits.GetRunStub<IShoppingCart>();

            return await PayAndWaitForOrderCompletionIfUserIsShopping_Logic(paymentAmount, cart);
        }

        private static async Task<bool> PayAndWaitForOrderCompletionIfUserIsShopping_Logic(MoneyAmount paymentAmount, IShoppingCart cart)
        {
            Task<OrderConfirmation> order;

            try
            {
                order = cart.ContinueShoppingAsync();                
            }
            catch (NeedsDesignException)
            {
                return false;  // "Could not apply shipping info. Does user have an active shopping cart?
            }

            await cart.ApplyPaymentAsync(paymentAmount);
            
            OrderConfirmation confirmation = await order;
            Console.WriteLine($"Order \"{confirmation.OrderId}\" was placed.");

            return true;
        }

        public static async Task<bool> AddProductToExistingCart2_Main(User shopper, Product product)
        {
            TemporalServiceNamespaceClient client = await GetShoppingNamespaceClientAsync();

            Workflow userVisits = await client.GetWorkflowAsync("ShoppingCart", shopper.UserKey);
            IShoppingCart cart = userVisits.GetRunStub<IShoppingCart>();

            return await AddProductToExistingCart2_Logic(product, cart);
        }

        private static async Task<bool> AddProductToExistingCart2_Logic(Product product, IShoppingCart cart)
        {
            // Force binding to an existing workflow run:
            try
            {
                await cart.ContinueShoppingAsync();
            }
            catch (NeedsDesignException)
            {
                // We could return false here, but let's use another way of checking.
            }

            // Any stub can be cast to 'IWorkflowRunStub'. We can see if the above binding succeeded:
            if (! ((IWorkflowRunStub) cart).IsBound)
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
            TemporalServiceNamespaceClient client = await GetShoppingNamespaceClientAsync();

            Workflow userVisits = await client.GetWorkflowAsync("ShoppingCart", shopper.UserKey);
            IShoppingCart cart = userVisits.GetRunStub<IShoppingCart>("taskQueue");

            await AddProductToNewOrExistingCart_Logic(shopper, product, cart);
        }

        private static async Task AddProductToNewOrExistingCart_Logic(User shopper, Product product, IShoppingCart cart)
        {
            await cart.ShopAsync(shopper);  // Start new run or connect to existing.

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
