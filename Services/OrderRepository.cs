
using DriveHub.Models;
using Google.Cloud.Firestore;

namespace DriveHub.Services;

public class OrderRepository(FirestoreDb db)
{
    private CollectionReference Orders =>
        db.Collection("orders");

    private CollectionReference OrderItems =>
        db.Collection("orderItems");

    // Convert Firestore data into an Order.
    private static Order ReadOrder(DocumentSnapshot snapshot)
    {
        var data = snapshot.ToDictionary();

        T Get<T>(string key, T fallback = default!)
        {
            if (!data.TryGetValue(key, out var value) ||
                value == null)
                return fallback;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        DateTime? GetDate(string key)
        {
            if (!data.TryGetValue(key, out var value) ||
                value is not Timestamp timestamp)
                return null;

            return timestamp.ToDateTime();
        }

        return new Order
        {
            Id = snapshot.Id,
            OrderNumber = Get("orderNumber", ""),
            BuyerId = Get("buyerId", ""),

            ShippingStreet = Get("shippingStreet", ""),
            ShippingCity = Get("shippingCity", ""),
            ShippingProvince = Get("shippingProvince", ""),
            ShippingPostalCode = Get("shippingPostalCode", ""),

            MerchandiseTotalCents =
                Get("merchandiseTotalCents", 0L),

            Currency = Get("currency", "cad"),

            PaymentProvider = (PaymentProvider)
                Get("paymentProvider", 0),

            PaymentStatus = (PaymentStatus)
                Get("paymentStatus", 0),

            CheckoutMode = (CheckoutMode)
                Get("checkoutMode", 0),

            StripeCheckoutSessionId =
                Get<string?>("stripeCheckoutSessionId", null),

            StripePaymentIntentId =
                Get<string?>("stripePaymentIntentId", null),

            CreatedAt = GetDate("createdAt")
                ?? DateTime.UtcNow,

            PaidAt = GetDate("paidAt")
        };
    }

    // Convert Order into Firestore fields.
    private static Dictionary<string, object?> OrderData(
        Order order)
    {
        return new Dictionary<string, object?>
        {
            ["orderNumber"] = order.OrderNumber,
            ["buyerId"] = order.BuyerId,

            ["shippingStreet"] = order.ShippingStreet,
            ["shippingCity"] = order.ShippingCity,
            ["shippingProvince"] = order.ShippingProvince,
            ["shippingPostalCode"] = order.ShippingPostalCode,

            ["merchandiseTotalCents"] =
                order.MerchandiseTotalCents,

            ["currency"] = order.Currency,

            ["paymentProvider"] = (int)order.PaymentProvider,
            ["paymentStatus"] = (int)order.PaymentStatus,
            ["checkoutMode"] = (int)order.CheckoutMode,

            ["stripeCheckoutSessionId"] =
                order.StripeCheckoutSessionId,

            ["stripePaymentIntentId"] =
                order.StripePaymentIntentId,

            ["createdAt"] =
                Timestamp.FromDateTime(
                    order.CreatedAt.ToUniversalTime()),

            ["paidAt"] = order.PaidAt.HasValue
                ? Timestamp.FromDateTime(
                    order.PaidAt.Value.ToUniversalTime())
                : null
        };
    }

    // Convert Firestore data into an OrderItem.
    private static OrderItem ReadItem(DocumentSnapshot snapshot)
    {
        var data = snapshot.ToDictionary();

        T Get<T>(string key, T fallback = default!)
        {
            if (!data.TryGetValue(key, out var value) ||
                value == null)
                return fallback;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        return new OrderItem
        {
            Id = snapshot.Id,
            OrderId = Get("orderId", ""),
            PartId = Get("partId", ""),
            SellerId = Get("sellerId", ""),
            PartName = Get("partName", ""),

            UnitPriceCents = Get("unitPriceCents", 0L),
            Quantity = Get("quantity", 1),

            FulfillmentStatus = (FulfillmentStatus)
                Get("fulfillmentStatus", 0)
        };
    }

    private static Dictionary<string, object> ItemData(
        OrderItem item)
    {
        return new Dictionary<string, object>
        {
            ["orderId"] = item.OrderId,
            ["partId"] = item.PartId,
            ["sellerId"] = item.SellerId,
            ["partName"] = item.PartName,
            ["unitPriceCents"] = item.UnitPriceCents,
            ["quantity"] = item.Quantity,
            ["fulfillmentStatus"] =
                (int)item.FulfillmentStatus
        };
    }

    // SAVE A NEW ORDER AND ITS ITEMS TOGETHER.
    //
    // Internal persistence method only.
    // The checkout service must validate prices,
    // stock, ownership and payment information first.
    //
    // Inventory is NOT decremented here.
    public async Task<string> AddAsync(
        Order order,
        List<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(order.BuyerId))
            throw new ArgumentException(
                "Buyer ID is required.");

        if (items == null || items.Count == 0)
            throw new ArgumentException(
                "An order must contain at least one item.");

        if (items.Count > 100)
            throw new ArgumentException(
                "Too many items in one order.");

        if (order.PaymentStatus != PaymentStatus.Pending)
            throw new InvalidOperationException(
                "New orders must start as Pending.");

        if (order.PaidAt != null)
            throw new InvalidOperationException(
                "New orders cannot have a paid date.");

        if (order.PaymentProvider == PaymentProvider.Simulation &&
            order.CheckoutMode != CheckoutMode.Simulation)
            throw new InvalidOperationException(
                "Invalid simulation payment configuration.");

        if (order.PaymentProvider == PaymentProvider.Stripe &&
            order.CheckoutMode == CheckoutMode.Simulation)
            throw new InvalidOperationException(
                "Invalid Stripe payment configuration.");

        long total = 0;

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.PartId) ||
                string.IsNullOrWhiteSpace(item.SellerId) ||
                string.IsNullOrWhiteSpace(item.PartName))
                throw new ArgumentException(
                    "Order item information is incomplete.");

            if (item.Quantity <= 0 ||
                item.UnitPriceCents < 0)
                throw new ArgumentException(
                    "Invalid item quantity or price.");

            total = checked(
                total + checked(
                    item.UnitPriceCents * item.Quantity));
        }

        if (order.MerchandiseTotalCents != total)
            throw new InvalidOperationException(
                "Order total does not match item totals.");

        var orderDoc = Orders.Document();

        order.Id = orderDoc.Id;
        order.CreatedAt = DateTime.UtcNow;

        // A readable unique reference for the buyer.
        order.OrderNumber = "DH-" +
            orderDoc.Id[..8].ToUpperInvariant();

        var batch = db.StartBatch();

        batch.Set(orderDoc, OrderData(order));

        foreach (var item in items)
        {
            var itemDoc = OrderItems.Document();

            item.Id = itemDoc.Id;
            item.OrderId = order.Id;
            item.FulfillmentStatus =
                FulfillmentStatus.Pending;

            batch.Set(itemDoc, ItemData(item));
        }

        await batch.CommitAsync();

        return order.Id;
    }

    public async Task<Order?> GetForBuyerAsync(
        string orderId,
        string buyerId)
    {
        if (string.IsNullOrWhiteSpace(orderId) ||
            string.IsNullOrWhiteSpace(buyerId))
            return null;

        var snapshot = await Orders.Document(orderId)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
            return null;

        var order = ReadOrder(snapshot);

        return order.BuyerId == buyerId
            ? order
            : null;
    }

    public async Task<List<Order>> GetByBuyerAsync(
        string buyerId)
    {
        if (string.IsNullOrWhiteSpace(buyerId))
            return new List<Order>();

        var snapshot = await Orders
            .WhereEqualTo("buyerId", buyerId)
            .GetSnapshotAsync();

        return snapshot.Documents
            .Select(ReadOrder)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();
    }

    public async Task<List<OrderItem>> GetItemsAsync(
        string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return new List<OrderItem>();

        var snapshot = await OrderItems
            .WhereEqualTo("orderId", orderId)
            .GetSnapshotAsync();

        return snapshot.Documents
            .Select(ReadItem)
            .ToList();
    }

    public async Task<List<OrderItem>> GetItemsForBuyerAsync(
        string orderId,
        string buyerId)
    {
        var order = await GetForBuyerAsync(
            orderId, buyerId);

        if (order == null)
            return new List<OrderItem>();

        return await GetItemsAsync(orderId);
    }

    public async Task<List<OrderItem>> GetBySellerAsync(
        string sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
            return new List<OrderItem>();

        var snapshot = await OrderItems
            .WhereEqualTo("sellerId", sellerId)
            .GetSnapshotAsync();

        return snapshot.Documents
            .Select(ReadItem)
            .ToList();
    }
 
    public async Task<bool> UpdateFulfillmentAsync(
        string itemId,
        string sellerId,
        FulfillmentStatus newStatus)
    {
        if (string.IsNullOrWhiteSpace(itemId) ||
            string.IsNullOrWhiteSpace(sellerId))
            return false;

        if (!Enum.IsDefined(newStatus))
            return false;

        var itemDoc = OrderItems.Document(itemId);

        return await db.RunTransactionAsync(
            async transaction =>
            {
                var itemSnapshot =
                    await transaction.GetSnapshotAsync(itemDoc);

                if (!itemSnapshot.Exists)
                    return false;

                var item = ReadItem(itemSnapshot);

                if (item.SellerId != sellerId)
                    return false;

                var orderDoc = Orders.Document(item.OrderId);

                var orderSnapshot =
                    await transaction.GetSnapshotAsync(orderDoc);

                if (!orderSnapshot.Exists)
                    return false;

                var order = ReadOrder(orderSnapshot);

                // Never fulfill an unpaid order.
                if (order.PaymentStatus != PaymentStatus.Paid)
                    return false;

                // Prevent changing finished/cancelled items.
                if (item.FulfillmentStatus ==
                        FulfillmentStatus.Completed ||
                    item.FulfillmentStatus ==
                        FulfillmentStatus.Cancelled)
                    return false;

                bool allowed =
                    item.FulfillmentStatus ==
                        FulfillmentStatus.Pending &&
                    (newStatus == FulfillmentStatus.Accepted ||
                     newStatus == FulfillmentStatus.Cancelled)
                    ||
                    item.FulfillmentStatus ==
                        FulfillmentStatus.Accepted &&
                    (newStatus == FulfillmentStatus.Shipped ||
                     newStatus == FulfillmentStatus.Cancelled)
                    ||
                    item.FulfillmentStatus ==
                        FulfillmentStatus.Shipped &&
                    newStatus == FulfillmentStatus.Completed;

                if (!allowed)
                    return false;

                transaction.Update(itemDoc,
                    new Dictionary<string, object>
                    {
                        ["fulfillmentStatus"] = (int)newStatus
                    });

                return true;
            });
    }
}
