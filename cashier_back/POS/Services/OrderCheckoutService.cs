using Microsoft.EntityFrameworkCore;
using POS.Db;
using POS.Models;
using POS.Models.Requests;
using POS.Models.Response;

namespace POS.Services
{
    public interface IOrderCheckoutService
    {
        Task<GlobalResponse<object>?> ApplyCheckoutAsync(CustomerOrder order, CustomerOrderRequest request, int userId, int commercialUserId);
        Task<GlobalResponse<object>?> ValidateCardTransactionForCheckoutAsync(int transactionId, decimal expectedAmount, int commercialUserId);
        Task LinkCardTransactionToOrderAsync(int transactionId, int orderId);
    }

    public class OrderCheckoutService : IOrderCheckoutService
    {
        private readonly DbConfig _dbConfig;

        public OrderCheckoutService(DbConfig dbConfig)
        {
            _dbConfig = dbConfig;
        }

        public async Task<GlobalResponse<object>?> ValidateCardTransactionForCheckoutAsync(int transactionId, decimal expectedAmount, int commercialUserId)
        {
            var tx = await _dbConfig.CardPaymentTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (tx == null)
            {
                return new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "cardPaymentTransactionNotFound"
                };
            }

            if (!string.Equals(tx.Status, "Success", StringComparison.OrdinalIgnoreCase))
            {
                return new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "cardPaymentTransactionNotSuccessful"
                };
            }

            if (tx.CustomerOrderId.HasValue)
            {
                return new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "cardPaymentTransactionAlreadyLinked"
                };
            }

            var expected = (long)Math.Round(expectedAmount, MidpointRounding.AwayFromZero);
            var actual = (long)Math.Round(tx.Amount, MidpointRounding.AwayFromZero);
            if (expected != actual)
            {
                return new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "cardPaymentAmountMismatch"
                };
            }

            return null;
        }

        public async Task<GlobalResponse<object>?> ApplyCheckoutAsync(CustomerOrder order, CustomerOrderRequest request, int userId, int commercialUserId)
        {
            if (!request.IsCheckout)
            {
                return null;
            }

            var paymentMethod = request.PaymentMethod ?? "Cash";

            if (string.Equals(paymentMethod, "Card", StringComparison.OrdinalIgnoreCase))
            {
                if (!request.CardPaymentTransactionId.HasValue)
                {
                    return new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "cardPaymentTransactionRequired"
                    };
                }

                var expectedTotal = request.OrderTotalAfterDiscount ?? order.OrderTotalAfterDiscount ?? request.OrderSubTotal ?? 0;
                var validationError = await ValidateCardTransactionForCheckoutAsync(
                    request.CardPaymentTransactionId.Value,
                    expectedTotal,
                    commercialUserId);

                if (validationError != null)
                {
                    return validationError;
                }

                order.PaymentMethod = "Card";
                _dbConfig.CustomerOrders.Update(order);
                await _dbConfig.SaveChangesAsync();

                await LinkCardTransactionToOrderAsync(request.CardPaymentTransactionId.Value, order.Id);
            }

            return null;
        }

        public async Task LinkCardTransactionToOrderAsync(int transactionId, int orderId)
        {
            var tx = await _dbConfig.CardPaymentTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted);

            if (tx == null)
            {
                return;
            }

            tx.CustomerOrderId = orderId;
            tx.UpdateDate = DateTime.UtcNow;
            _dbConfig.CardPaymentTransactions.Update(tx);
            await _dbConfig.SaveChangesAsync();
        }
    }
}
