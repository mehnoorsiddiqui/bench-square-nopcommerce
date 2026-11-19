using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Square.Models;
using Nop.Plugin.Payments.Square.Services;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;

namespace Nop.Plugin.Payments.Square.Components;

/// <summary>
/// Represents payment info view component
/// </summary>
public class PaymentSquareViewComponent : ViewComponent
{
    #region Fields

    private readonly CurrencySettings _currencySettings;
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly ICustomerService _customerService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger _logger;
    private readonly IOrderTotalCalculationService _orderTotalCalculationService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IStateProvinceService _stateProvinceService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly SquareCardService _squareCardService;
    private readonly SquarePaymentSettings _squarePaymentSettings;

    #endregion

    #region Ctor

    public PaymentSquareViewComponent(CurrencySettings currencySettings,
        ICountryService countryService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        ILogger logger,
        IOrderTotalCalculationService orderTotalCalculationService,
        IShoppingCartService shoppingCartService,
        IStateProvinceService stateProvinceService,
        IStoreContext storeContext,
        IWorkContext workContext,
        SquareCardService squareCardService,
        SquarePaymentSettings squarePaymentSettings)
    {
        _currencySettings = currencySettings;
        _countryService = countryService;
        _currencyService = currencyService;
        _customerService = customerService;
        _genericAttributeService = genericAttributeService;
        _localizationService = localizationService;
        _logger = logger;
        _orderTotalCalculationService = orderTotalCalculationService;
        _shoppingCartService = shoppingCartService;
        _stateProvinceService = stateProvinceService;
        _storeContext = storeContext;
        _workContext = workContext;
        _squareCardService = squareCardService;
        _squarePaymentSettings = squarePaymentSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Invoke view component
    /// </summary>
    /// <param name="widgetZone">Widget zone name</param>
    /// <param name="additionalData">Additional data</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the view component result
    /// </returns>
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        var currentCustomer = await _workContext.GetCurrentCustomerAsync();
        var model = new PaymentInfoModel
        {
            //whether current customer is guest
            IsGuest = await _customerService.IsGuestAsync(currentCustomer),

            //get postal code from the billing address or from the shipping one
            PostalCode = (await _customerService.GetCustomerBillingAddressAsync(currentCustomer))?.ZipPostalCode
                ?? (await _customerService.GetCustomerShippingAddressAsync(currentCustomer))?.ZipPostalCode
        };

        //whether customer already has stored cards in current store
        var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
        var customerId = await _genericAttributeService
                .GetAttributeAsync<string>(currentCustomer, SquarePaymentDefaults.CustomerIdAttribute)
            ?? string.Empty;
        IList<SavedCardInfo> savedCards;
        try
        {
            savedCards = await _squareCardService.GetSavedCardsAsync(customerId, storeId);
        }
        catch (Exception exception)
        {
            //the checkout still works with a new card when the saved ones cannot be loaded
            await _logger.ErrorAsync($"Square payment error: {exception.Message}.", exception, currentCustomer);
            savedCards = new List<SavedCardInfo>();
        }

        if (savedCards.Any())
        {
            var cardNumberMask = await _localizationService.GetResourceAsync("Plugins.Payments.Square.Fields.StoredCard.Mask");
            model.StoredCards = savedCards
                .Select(card => new SelectListItem
                {
                    Text = $"{card.Brand} {string.Format(cardNumberMask, card.Last4)} ({card.ExpMonth:00}/{card.ExpYear % 100:00})",
                    Value = card.Id
                })
                .ToList();
        }

        //add the special item for 'select card' with empty GUID value
        if (model.StoredCards.Any())
        {
            var selectCardText = await _localizationService.GetResourceAsync("Plugins.Payments.Square.Fields.StoredCard.SelectCard");
            model.StoredCards.Insert(0, new SelectListItem { Text = selectCardText, Value = Guid.Empty.ToString() });
        }

        //set verfication details
        if (_squarePaymentSettings.Use3ds)
        {
            var cart = await _shoppingCartService.GetShoppingCartAsync(currentCustomer, ShoppingCartType.ShoppingCart, storeId);
            model.OrderTotal = (await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart, false, false)).shoppingCartTotal ?? decimal.Zero;

            var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
            model.Currency = currency?.CurrencyCode;

            var billingAddress = await _customerService.GetCustomerBillingAddressAsync(currentCustomer);
            var country = await _countryService.GetCountryByAddressAsync(billingAddress);
            var stateProvince = await _stateProvinceService.GetStateProvinceByAddressAsync(billingAddress);

            model.BillingFirstName = billingAddress?.FirstName;
            model.BillingLastName = billingAddress?.LastName;
            model.BillingEmail = billingAddress?.Email;
            model.BillingCountry = country?.TwoLetterIsoCode;
            model.BillingState = stateProvince?.Abbreviation;
            model.BillingCity = billingAddress?.City;
            model.BillingPostalCode = billingAddress?.ZipPostalCode;
            model.BillingAddressLine1 = billingAddress?.Address1 ?? string.Empty;
            model.BillingAddressLine2 = billingAddress?.Address2 ?? string.Empty;
        }

        return View("~/Plugins/Payments.Square/Views/PaymentInfo.cshtml", model);
    }

    #endregion
}