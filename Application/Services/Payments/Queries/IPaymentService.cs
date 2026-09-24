using Application.ConnectedServices;
using Application.Interfaces.Contexts;
using Application.Services.AdditionalInformations.Commands;
using Application.Services.Baskets.Queries;
using Application.Services.Shippings.Queries;
using Common;
using Common.Dto;
using Domain.Entities.AdditionalInformations;
using Domain.Entities.Baskets;
using Domain.Entities.HandleException;
using Domain.Entities.Payments;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Services.Payments.Queries
{
    // ==================== Interface ====================
    public interface IPaymentService
    {
        // -------- Sadad --------
        ResultDto<PaymentInitiateResultDto> InitiatePaymentSadad(long userId, string publicToken);
        ResultDto<PaymentVerifyResultDto> CallbackSadad(PaymentGatewayCallbackDto callback);

        // -------- Sep --------
        ResultDto<PaymentInitiateResultDto> InitiatePaymentSep(long userId, string publicToken, string? payerCellNumber = null);
        ResultDto<PaymentVerifyResultDto> CallbackSep(SepGatewayCallbackDto callback);

        ResultDto<ChequePaymentResultDto> PayByCheque(long userId, List<string> chequeImageNames);
    }

    // ==================== Implementation ====================
    public class PaymentService : IPaymentService
    {
        private const long RialsPerToman = 10;
        private const int MinDaysForRepeatPurchaseScore = 15;
        private const int MaxResultDescriptionLength = 500;

        // SEP reports State == "OK" on the callback for a successful payment.
        private const string SepExpectedSuccessState = "OK";

        private readonly IDataBaseContext _context;
        private readonly IConfiguration _configuration;
        private readonly ISadadGatewayClient _sadadClient;
        private readonly ISepGatewayClient _sepClient;
        private readonly IShippingService _shippingService;

        public PaymentService(
            IConfiguration configuration,
            IDataBaseContext context,
            ISadadGatewayClient sadadClient,
            ISepGatewayClient sepClient,
            IShippingService shippingService)
        {
            _context = context;
            _configuration = configuration;
            _sadadClient = sadadClient;
            _sepClient = sepClient;
            _shippingService = shippingService;
        }

        // ==================================================================
        // ============================ SADAD ==============================
        // ==================================================================

        // -------------------- Initiate (Sadad) --------------------
        // No client-supplied amount: we look up the caller's own Created basket and price it
        // ourselves. Same "find my current basket" pattern BasketService uses everywhere.
        public ResultDto<PaymentInitiateResultDto> InitiatePaymentSadad(long userId, string publicToken)
        {
            try
            {
                var basket = FindCreatedBasket(userId, publicToken);

                if (basket == null || !basket.BasketItems.Any())
                {
                    return new ResultDto<PaymentInitiateResultDto>
                    {
                        IsSuccess = false,
                        Status = 404,
                        Message = "سبد خرید یافت نشد یا خالی است"
                    };
                }

                if (basket.UserAddress == null || basket.ShippingId == null)
                {
                    return new ResultDto<PaymentInitiateResultDto>
                    {
                        IsSuccess = false,
                        Status = 400,
                        Message = "لطفا آدرس و نحوه سفارش انتخاب کنید"
                    };
                }

                // Re-validate the chosen shipping right before charging: address/weight may
                // have changed since ChooseShippingOption ran. Also re-syncs ShippingCost if
                // the price moved, so the amount charged is never based on stale data.
                var shippingError = ValidateAndRefreshShippingForPayment(basket);
                if (shippingError != null)
                {
                    return new ResultDto<PaymentInitiateResultDto>
                    {
                        IsSuccess = false,
                        Status = shippingError.Status,
                        Message = shippingError.Message
                    };
                }

                long amount = ComputeBasketAmount(basket);

                if (amount <= 0)
                {
                    return new ResultDto<PaymentInitiateResultDto>
                    {
                        IsSuccess = false,
                        Status = 400,
                        Message = "مبلغ سبد خرید نامعتبر است"
                    };
                }

                long amountInRials = amount * RialsPerToman;

                var sadadSettings = _configuration.GetSection("SadadGateway");
                string terminalId = sadadSettings["TerminalId"];
                string merchantId = sadadSettings["MerchantId"];
                string merchantKey = sadadSettings["MerchantKey"];
                string returnUrl = sadadSettings["ReturnUrl"];

                var orderId = DateTime.UtcNow.Ticks.ToString().Substring(0, 16);

                var transaction = new PaymentTransaction
                {
                    OrderId = orderId,
                    Amount = amount,
                    TerminalId = terminalId,
                    MerchantId = merchantId,
                    BasketId = basket.Id,
                    Status = PaymentTransactionStatus.Created
                };

                // Sadad's sign format: "{TerminalId};{OrderId};{Amount}"
                // IMPORTANT: the amount signed here MUST match the amount sent in the request body
                // below (Rials), or Sadad will reject the signature.
                var signData = _sadadClient.Sign($"{terminalId};{orderId};{amountInRials}", merchantKey);

                var gatewayRequest = new SadadPaymentGatewayRequestDto
                {
                    TerminalId = terminalId,
                    MerchantId = merchantId,
                    Amount = amountInRials,
                    SignData = signData,
                    ReturnUrl = returnUrl,
                    LocalDateTime = DateTime.Now,
                    OrderId = orderId
                };

                var gatewayResponse = _sadadClient.RequestPayment(gatewayRequest);

                if (gatewayResponse.ResCode == "0" && !string.IsNullOrEmpty(gatewayResponse.Token))
                {
                    transaction.Status = PaymentTransactionStatus.RedirectedToGateway;
                    transaction.GatewayToken = gatewayResponse.Token;

                    _context.PaymentTransactions.Add(transaction);
                    _context.SaveChanges();

                    return new ResultDto<PaymentInitiateResultDto>
                    {
                        Data = new PaymentInitiateResultDto
                        {
                            OrderId = orderId,
                            BasketId = basket.Id,
                            Amount = amount, // Tomans, matches what the user saw on-site
                            RedirectUrl = _sadadClient.BuildPurchaseRedirectUrl(gatewayResponse.Token)
                        },
                        IsSuccess = true,
                        Status = 200,
                        Message = "درخواست پرداخت با موفقیت ثبت شد"
                    };
                }

                transaction.Status = PaymentTransactionStatus.GatewayRejected;
                transaction.ResultDescription = gatewayResponse.Description;

                _context.PaymentTransactions.Add(transaction);
                _context.SaveChanges();

                return new ResultDto<PaymentInitiateResultDto>
                {
                    Data = null,
                    IsSuccess = false,
                    Status = 422,
                    Message = gatewayResponse.Description ?? "درخواست پرداخت توسط درگاه رد شد"
                };
            }
            catch (Exception ex)
            {
                return HandleException<PaymentInitiateResultDto>(ex);
            }
        }

        // -------------------- Verify (callback from bank, Sadad) --------------------
        public ResultDto<PaymentVerifyResultDto> CallbackSadad(PaymentGatewayCallbackDto callback)
        {
            try
            {
                var transaction = _context.PaymentTransactions
                    .FirstOrDefault(t => t.OrderId == callback.OrderId);

                if (transaction == null)
                {
                    return new ResultDto<PaymentVerifyResultDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "تراکنش یافت نشد"
                    };
                }

                // Idempotency guard: if this transaction was already verified (e.g. the bank
                // double-posts the callback, or the user refreshes the return page), don't
                // re-run verify or flip the basket status again.
                if (transaction.Status == PaymentTransactionStatus.Verified)
                {
                    return AlreadyVerifiedResult(transaction);
                }

                transaction.Status = PaymentTransactionStatus.ReturnedFromGateway;

                if (callback.ResCode != "0")
                {
                    transaction.Status = PaymentTransactionStatus.Failed;
                    transaction.ResultDescription = $"ResCode={callback.ResCode}";
                    _context.SaveChanges();

                    return new ResultDto<PaymentVerifyResultDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 400,
                        Message = "پرداخت با موفقیت انجام نشد"
                    };
                }

                var sadadSettings = _configuration.GetSection("SadadGateway");
                string merchantKey = sadadSettings["MerchantKey"];

                // Verify step signs the TOKEN, not the original order string.
                var signData = _sadadClient.Sign(callback.Token, merchantKey);

                var verifyResponse = _sadadClient.VerifyPayment(new SadadVerifyGatewayRequestDto
                {
                    Token = callback.Token,
                    SignData = signData
                });

                if (verifyResponse.ResCode != "0")
                {
                    transaction.Status = PaymentTransactionStatus.Failed;
                    transaction.ResultDescription = verifyResponse.Description;
                    _context.SaveChanges();

                    return new ResultDto<PaymentVerifyResultDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 422,
                        Message = verifyResponse.Description ?? "تایید پرداخت با خطا مواجه شد"
                    };
                }

                // Defensive check: the amount the bank confirms comes back in RIALS,
                // while transaction.Amount is stored in TOMANS — compare like-for-like.
                // NOTE: this is left "fail-open" (an unparseable amount is accepted) exactly as
                // before, because I can't see SadadGatewayClient's response schema. Consider
                // switching to the fail-closed form used in CallbackSep once you've confirmed
                // Sadad always returns Amount.
                long expectedRials = transaction.Amount * RialsPerToman;

                if (long.TryParse(verifyResponse.Amount, out var verifiedAmountRials) && verifiedAmountRials != expectedRials)
                {
                    return FailAmountMismatch(transaction, expectedRials, verifiedAmountRials.ToString());
                }

                transaction.Status = PaymentTransactionStatus.Verified;
                transaction.VerifiedAt = DateTime.Now;
                transaction.RetrievalReferenceNumber = verifyResponse.RetrivalRefNo;
                transaction.SystemTraceAuditNumber = verifyResponse.SystemTraceNo;
                transaction.ResultDescription = verifyResponse.Description;

                FinalizeBasketAndAwardScores(transaction);

                return new ResultDto<PaymentVerifyResultDto>
                {
                    Data = new PaymentVerifyResultDto
                    {
                        OrderId = transaction.OrderId,
                        Amount = transaction.Amount.ToString(),
                        RetrievalReferenceNumber = verifyResponse.RetrivalRefNo,
                        SystemTraceAuditNumber = verifyResponse.SystemTraceNo
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "پرداخت با موفقیت تایید شد"
                };
            }
            catch (Exception ex)
            {
                return HandleException<PaymentVerifyResultDto>(ex);
            }
        }

        // ==================================================================
        // ============================= SEP ===============================
        // ==================================================================

        // -------------------- Initiate (Sep) --------------------
        public ResultDto<PaymentInitiateResultDto> InitiatePaymentSep(long userId, string publicToken, string? payerCellNumber = null)
        {
            try
            {
                var basket = FindCreatedBasket(userId, publicToken);

                if (basket == null || !basket.BasketItems.Any())
                {
                    return new ResultDto<PaymentInitiateResultDto>
                    {
                        IsSuccess = false,
                        Status = 404,
                        Message = "سبد خرید یافت نشد یا خالی است"
                    };
                }

                if (basket.UserAddress == null || basket.ShippingId == null)
                {
                    return new ResultDto<PaymentInitiateResultDto>
                    {
                        IsSuccess = false,
                        Status = 400,
                        Message = "لطفا آدرس و نحوه سفارش انتخاب کنید"
                    };
                }

                var shippingError = ValidateAndRefreshShippingForPayment(basket);
                if (shippingError != null)
                {
                    return new ResultDto<PaymentInitiateResultDto>
                    {
                        IsSuccess = false,
                        Status = shippingError.Status,
                        Message = shippingError.Message
                    };
                }

                long amount = ComputeBasketAmount(basket);

                if (amount <= 0)
                {
                    return new ResultDto<PaymentInitiateResultDto>
                    {
                        IsSuccess = false,
                        Status = 400,
                        Message = "مبلغ سبد خرید نامعتبر است"
                    };
                }

                long amountInRials = amount * RialsPerToman;

                var sepSettings = _configuration.GetSection("SepGateway");
                string terminalId = sepSettings["TerminalId"] ?? string.Empty;
                string returnUrl = sepSettings["ReturnUrl"] ?? string.Empty;

                var resNum = DateTime.UtcNow.Ticks.ToString().Substring(0, 16);

                var transaction = new PaymentTransaction
                {
                    OrderId = resNum,
                    Amount = amount, // Tomans
                    TerminalId = terminalId,
                    BasketId = basket.Id,
                    Status = PaymentTransactionStatus.Created
                };

                var tokenRequest = new SepTokenRequestDto
                {
                    TerminalId = terminalId,
                    ResNum = resNum,
                    Amount = amountInRials,
                    CellNumber = payerCellNumber,
                    RedirectUrl = returnUrl
                };

                var tokenResponse = _sepClient.RequestToken(tokenRequest);

                if (tokenResponse.Status == 1 && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    transaction.Status = PaymentTransactionStatus.RedirectedToGateway;
                    transaction.GatewayToken = tokenResponse.Token;

                    _context.PaymentTransactions.Add(transaction);
                    _context.SaveChanges();

                    return new ResultDto<PaymentInitiateResultDto>
                    {
                        Data = new PaymentInitiateResultDto
                        {
                            OrderId = resNum,
                            BasketId = basket.Id,
                            Amount = amount, // Tomans, matches what the user saw on-site
                            GatewayToken = tokenResponse.Token,
                            SepRedirect = _sepClient.GetPurchaseRedirectInfo(tokenResponse.Token)
                        },
                        IsSuccess = true,
                        Status = 200,
                        Message = "درخواست پرداخت با موفقیت ثبت شد"
                    };
                }

                transaction.Status = PaymentTransactionStatus.GatewayRejected;
                transaction.ResultDescription = BuildSepRejectionDescription(tokenResponse);

                _context.PaymentTransactions.Add(transaction);
                _context.SaveChanges();

                return new ResultDto<PaymentInitiateResultDto>
                {
                    Data = null,
                    IsSuccess = false,
                    Status = 422,
                    Message = tokenResponse.ErrorDesc ?? "درخواست پرداخت توسط درگاه رد شد"
                };
            }
            catch (Exception ex)
            {
                return HandleException<PaymentInitiateResultDto>(ex);
            }
        }

        // -------------------- Verify (callback from bank, Sep) --------------------
        // Callback shape is SEP's, not Sadad's: ResNum/RefNum/State/TraceNo/TerminalId.
        // Nothing in the callback is trusted on its own: the amount and reference are always
        // confirmed by SEP's verify call, using OUR stored terminal id.
        public ResultDto<PaymentVerifyResultDto> CallbackSep(SepGatewayCallbackDto callback)
        {
            try
            {
                var transaction = _context.PaymentTransactions
                    .FirstOrDefault(t => t.OrderId == callback.ResNum);

                if (transaction == null)
                {
                    return new ResultDto<PaymentVerifyResultDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 404,
                        Message = "تراکنش یافت نشد"
                    };
                }

                if (transaction.Status == PaymentTransactionStatus.Verified)
                {
                    return AlreadyVerifiedResult(transaction);
                }

                // A transaction that never got a gateway token can't legitimately come back
                // from the gateway, so a callback for it is bogus.
                if (transaction.Status == PaymentTransactionStatus.Created ||
                    transaction.Status == PaymentTransactionStatus.GatewayRejected)
                {
                    return new ResultDto<PaymentVerifyResultDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 400,
                        Message = "وضعیت تراکنش نامعتبر است"
                    };
                }

                transaction.Status = PaymentTransactionStatus.ReturnedFromGateway;

                if (!IsSepCallbackSuccessful(callback.State))
                {
                    transaction.Status = PaymentTransactionStatus.Failed;
                    transaction.ResultDescription = $"State={callback.State}";
                    _context.SaveChanges();

                    return new ResultDto<PaymentVerifyResultDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 400,
                        Message = "پرداخت با موفقیت انجام نشد"
                    };
                }

                var verifyResponse = _sepClient.VerifyTransaction(new SepVerifyRequestDto
                {
                    TerminalNumber = transaction.TerminalId, // our own stored value, not callback.TerminalId
                    RefNum = callback.RefNum,
                    CellNumber = string.Empty,
                    NationalCode = string.Empty,
                    IgnoreNationalcode = true
                });

                if (!verifyResponse.Succeed)
                {
                    transaction.Status = PaymentTransactionStatus.Failed;
                    transaction.ResultDescription = Truncate(verifyResponse.Description ?? verifyResponse.RawResponse);
                    _context.SaveChanges();

                    return new ResultDto<PaymentVerifyResultDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 422,
                        Message = verifyResponse.Description ?? "تایید پرداخت با خطا مواجه شد"
                    };
                }

                // FAIL-CLOSED amount check: transaction.Amount is Tomans, the gateway reports Rials.
                // If the amount is missing or can't be parsed we do NOT approve the payment —
                // otherwise a schema mismatch would silently skip this check.
                long expectedRials = transaction.Amount * RialsPerToman;

                if (!long.TryParse(verifyResponse.Amount, out var verifiedAmountRials)
                    || verifiedAmountRials != expectedRials)
                {
                    return FailAmountMismatch(transaction, expectedRials, verifyResponse.Amount);
                }

                // Replay guard: the same bank reference (RRN) must not confirm two different orders.
                if (!string.IsNullOrEmpty(verifyResponse.Rrn) &&
                    _context.PaymentTransactions.Any(t =>
                        t.Id != transaction.Id &&
                        t.Status == PaymentTransactionStatus.Verified &&
                        t.RetrievalReferenceNumber == verifyResponse.Rrn))
                {
                    transaction.Status = PaymentTransactionStatus.Failed;
                    transaction.ResultDescription = $"Duplicate RRN {verifyResponse.Rrn} already used by another transaction";
                    _context.SaveChanges();

                    return new ResultDto<PaymentVerifyResultDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        Status = 422,
                        Message = "این تراکنش قبلا برای سفارش دیگری استفاده شده است"
                    };
                }

                transaction.Status = PaymentTransactionStatus.Verified;
                transaction.VerifiedAt = DateTime.Now;
                transaction.RetrievalReferenceNumber = verifyResponse.Rrn;
                transaction.SystemTraceAuditNumber = verifyResponse.TraceNo;
                transaction.ResultDescription = verifyResponse.Description;

                FinalizeBasketAndAwardScores(transaction);

                return new ResultDto<PaymentVerifyResultDto>
                {
                    Data = new PaymentVerifyResultDto
                    {
                        OrderId = transaction.OrderId,
                        Amount = transaction.Amount.ToString(),
                        RetrievalReferenceNumber = verifyResponse.Rrn,
                        SystemTraceAuditNumber = verifyResponse.TraceNo
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "پرداخت با موفقیت تایید شد"
                };
            }
            catch (Exception ex)
            {
                return HandleException<PaymentVerifyResultDto>(ex);
            }
        }

        private static bool IsSepCallbackSuccessful(string? state)
            => string.Equals(state, SepExpectedSuccessState, StringComparison.OrdinalIgnoreCase);

        private static string BuildSepRejectionDescription(SepTokenResponseDto response)
        {
            var text = string.Join(": ",
                new[] { response.ErrorCode, response.ErrorDesc }.Where(s => !string.IsNullOrWhiteSpace(s)));

            if (string.IsNullOrEmpty(text))
                text = response.RawResponse ?? "Unknown gateway rejection";

            return Truncate(text);
        }

        private static string Truncate(string? value)
            => string.IsNullOrEmpty(value) || value.Length <= MaxResultDescriptionLength
                ? value ?? string.Empty
                : value[..MaxResultDescriptionLength];

        // ==================================================================
        // ====================== SHARED / GATEWAY-AGNOSTIC ==================
        // ==================================================================

        private Basket FindCreatedBasket(long userId, string publicToken)
        {
            var basketQuery = _context.Baskets
                .Include(b => b.UserAddress)
                .Include(b => b.BasketItems)
                    .ThenInclude(bi => bi.Product)
                .Where(b => b.BasketStatus == BasketStatus.Created);

            basketQuery = userId != 0
                ? basketQuery.Where(b => b.UserId == userId)
                : basketQuery.Where(b => b.Token == publicToken);

            return basketQuery
                .OrderByDescending(b => b.Id)
                .FirstOrDefault();
        }

        private static long ComputeBasketAmount(Basket basket)
        {
            long itemsTotal = basket.BasketItems.Sum(item =>
                item.DiscountType == 2
                    ? (item.Price - item.Discount) * item.Count
                    : (item.Price - (item.Price * item.Discount / 100)) * item.Count);

            return itemsTotal + basket.ShippingCost;
        }

        private static int CalcBasketWeight(IEnumerable<BasketItem>? items)
        {
            if (items == null)
                return 0;

            // Product.Weight is stored in grams; shipping tiers are defined in kilograms.
            decimal totalGrams = items.Sum(i => (decimal)(i.Product?.Weight ?? 0) * i.Count);

            var totalKilos = totalGrams / 1000m;

            return totalKilos <= 0 ? 0 : (int)Math.Ceiling(totalKilos);
        }

        private ResultDto<string>? ValidateAndRefreshShippingForPayment(Basket basket)
        {
            var price = ComputeBasketAmount(basket);

            var weight = CalcBasketWeight(basket.BasketItems);

            var options = _shippingService.GetValidShippingOptions(
                weight,
                basket.UserAddress!.CityId,
                basket.ShippingId!.Value,
                price);

            var chosen = options.Data?.FirstOrDefault(o => o.ShippingId == basket.ShippingId!.Value);

            if (!options.IsSuccess || chosen == null)
            {
                return new ResultDto<string>
                {
                    IsSuccess = false,
                    Status = 400,
                    Message = "روش ارسال انتخاب شده دیگر برای این سبد خرید معتبر نیست. لطفا مجددا انتخاب کنید"
                };
            }

            if (basket.ShippingCost != chosen.Price)
            {
                basket.ShippingCost = chosen.Price;
                _context.Baskets.Update(basket);
                _context.SaveChanges();
            }

            return null;
        }

        private ResultDto<PaymentVerifyResultDto> AlreadyVerifiedResult(PaymentTransaction transaction)
        {
            // Idempotency guard: the bank can double-post a callback, or the user can refresh
            // the return page — don't re-run verify or flip the basket status again.
            return new ResultDto<PaymentVerifyResultDto>
            {
                Data = new PaymentVerifyResultDto
                {
                    OrderId = transaction.OrderId,
                    Amount = transaction.Amount.ToString(), // Tomans
                    RetrievalReferenceNumber = transaction.RetrievalReferenceNumber,
                    SystemTraceAuditNumber = transaction.SystemTraceAuditNumber
                },
                IsSuccess = true,
                Status = 200,
                Message = "این تراکنش قبلا تایید شده است"
            };
        }

        private ResultDto<PaymentVerifyResultDto> FailAmountMismatch(PaymentTransaction transaction, long expectedRials, string? reportedAmount)
        {
            transaction.Status = PaymentTransactionStatus.Failed;
            transaction.ResultDescription =
                $"Amount mismatch: expected {expectedRials} rials, gateway reported '{reportedAmount ?? "null"}'";
            _context.SaveChanges();

            return new ResultDto<PaymentVerifyResultDto>
            {
                Data = null,
                IsSuccess = false,
                Status = 422,
                Message = "مغایرت مبلغ پرداختی"
            };
        }

        private void FinalizeBasketAndAwardScores(PaymentTransaction transaction)
        {
            var basket = _context.Baskets
                .Include(b => b.BasketItems)
                .FirstOrDefault(b => b.Id == transaction.BasketId);

            bool shouldAwardScores = false;

            if (basket != null && basket.BasketStatus == BasketStatus.Created)
            {
                basket.BasketStatus = BasketStatus.Finalized;
                basket.PaymentDate = DateTime.Now;
                _context.Baskets.Update(basket);

                var scoreReservation = _context.Scores.FirstOrDefault(s =>
                    s.ReferenceId == basket.Id &&
                    s.Type == ScoreTypeEnum.Withdraw);

                if (scoreReservation != null)
                {
                    _context.Scores.Update(scoreReservation);
                }

                shouldAwardScores = basket.UserId.HasValue;
            }

            // Persists the Verified transaction, the basket and the score reservation together.
            _context.SaveChanges();

            if (shouldAwardScores)
            {

                // Persist anything the score service left tracked on the shared context.
                _context.SaveChanges();
            }
        }

        public ResultDto<ChequePaymentResultDto> PayByCheque(long userId, List<string> chequeImageNames)
        {
            try
            {
                if (chequeImageNames == null || chequeImageNames.Count == 0)
                {
                    return new ResultDto<ChequePaymentResultDto>
                    {
                        IsSuccess = false,
                        Status = 400,
                        Message = "تصویر چک الزامی است"
                    };
                }

                var basketQuery = _context.Baskets
                    .Include(b => b.BasketItems)
                    .Where(b => b.BasketStatus == BasketStatus.Created);

                basketQuery = basketQuery.Where(b => b.UserId == userId);

                var basket = basketQuery.OrderByDescending(b => b.Id).FirstOrDefault();

                if (basket == null || !basket.BasketItems.Any())
                {
                    return new ResultDto<ChequePaymentResultDto>
                    {
                        IsSuccess = false,
                        Status = 404,
                        Message = "سبد خرید یافت نشد یا خالی است"
                    };
                }

                long amount = ComputeBasketAmount(basket);

                if (amount <= 0)
                {
                    return new ResultDto<ChequePaymentResultDto>
                    {
                        IsSuccess = false,
                        Status = 400,
                        Message = "مبلغ سبد خرید نامعتبر است"
                    };
                }

                basket.BasketStatus = BasketStatus.Finalized;
                basket.PaymentDate = DateTime.Now;
                _context.Baskets.Update(basket);

                var scoreReservation = _context.Scores.FirstOrDefault(s =>
                    s.ReferenceId == basket.Id &&
                    s.Type == ScoreTypeEnum.Withdraw);

                if (scoreReservation != null)
                {
                    _context.Scores.Update(scoreReservation);
                }

                _context.SaveChanges();

                return new ResultDto<ChequePaymentResultDto>
                {
                    Data = new ChequePaymentResultDto
                    {
                        BasketId = basket.Id,
                        Amount = amount,
                        Message = "تصویر چک دریافت شد. سفارش پس از بررسی ادمین قطعی می‌شود"
                    },
                    IsSuccess = true,
                    Status = 200,
                    Message = "سفارش با پرداخت چکی ثبت شد"
                };
            }
            catch (Exception ex)
            {
                return HandleException<ChequePaymentResultDto>(ex);
            }
        }

        // ==================== Helpers ====================
        private void DetachAll()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
        }

        private ResultDto<T> HandleException<T>(Exception ex)
        {
            try
            {
                DetachAll();

                _context.HandleExceptions.Add(new HandleException
                {
                    Message = ex.InnerException?.Message ?? ex.Message,
                    StackTrace = ex.StackTrace
                });

                _context.SaveChanges();
            }
            catch
            {
                // avoid secondary crash during logging
            }

            return new ResultDto<T>
            {
                IsSuccess = false,
                Status = 500,
                Message = "خطای غیر منتظره - این خطا توسط تیم توسعه پیگیری خواهد شد"
            };
        }
    }

    public class PaymentInitiateResultDto
    {
        public string OrderId { get; set; }
        public long BasketId { get; set; }
        public long Amount { get; set; } // Tomans

        // -------- Sadad --------
        public string RedirectUrl { get; set; } // Sadad: GET redirect link

        // -------- Sep --------
        public string GatewayToken { get; set; }
        public SepRedirectInfo? SepRedirect { get; set; }
    }

    public class PaymentVerifyResultDto
    {
        public string OrderId { get; set; }
        public string? Amount { get; set; } // Tomans
        public string? RetrievalReferenceNumber { get; set; }
        public string? SystemTraceAuditNumber { get; set; }
    }

    public class ChequePaymentResultDto
    {
        public long BasketId { get; set; }
        public long Amount { get; set; } // Tomans
        public string Message { get; set; }
    }

    public class ChequePaymentRequestDto
    {
        public List<IFormFile> Files { get; set; }
    }
}