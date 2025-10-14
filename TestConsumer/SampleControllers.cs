using System;
using System.Threading.Tasks;
using Vantum.AppKit;

// Simulate ASP.NET Core MVC attributes (for testing without referencing the full framework)
namespace Microsoft.AspNetCore.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RouteAttribute : Attribute
    {
        public string Template { get; }
        public RouteAttribute(string template) => Template = template;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ApiControllerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class AreaAttribute : Attribute
    {
        public string Name { get; }
        public AreaAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpGetAttribute : Attribute
    {
        public string? Template { get; }
        public HttpGetAttribute() { }
        public HttpGetAttribute(string template) => Template = template;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPostAttribute : Attribute
    {
        public string? Template { get; }
        public HttpPostAttribute() { }
        public HttpPostAttribute(string template) => Template = template;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPutAttribute : Attribute
    {
        public string? Template { get; }
        public HttpPutAttribute() { }
        public HttpPutAttribute(string template) => Template = template;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpDeleteAttribute : Attribute
    {
        public string? Template { get; }
        public HttpDeleteAttribute() { }
        public HttpDeleteAttribute(string template) => Template = template;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPatchAttribute : Attribute
    {
        public string? Template { get; }
        public HttpPatchAttribute() { }
        public HttpPatchAttribute(string template) => Template = template;
    }

    public class ActionResult { }
    public class ActionResult<T> { }
}

namespace TestConsumer.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    // ===== PATTERN B EXAMPLE: Anchor + BelongsTo + Auto-Infer =====

    /// <summary>
    /// Anchor class for Contacts module (Pattern B)
    /// </summary>
    [AppModule(Name = "Contacts", DisplayName = "Contacts Management", Version = "1.0.0",
               Description = "Manage customer contacts and organizations")]
    [AppPermissions("Contacts.Read", "Contacts.Write", "Contacts.Delete")]
    public static class ContactsModuleAnchor
    {
        [AppSetting("Contacts.DefaultPageSize", AppSettingType.Int,
                    DefaultValue = "25",
                    Description = "Default number of contacts per page")]
        public const string DefaultPageSize = "Contacts.DefaultPageSize";
    }

    /// <summary>
    /// Contacts controller using Pattern B (auto-inference)
    /// </summary>
    [ApiController]
    [Route("api/contacts")]
    [AppBelongsTo("Contacts")]
    public class ContactsController
    {
        [HttpGet]
        [AppRouteAuto(RequiredPermission = "Contacts.Read")]
        public ActionResult ListContacts() => new ActionResult();

        [HttpGet("{id}")]
        [AppRouteAuto(RequiredPermission = "Contacts.Read")]
        public ActionResult GetContact() => new ActionResult();

        [HttpPost]
        [AppRouteAuto(RequiredPermission = "Contacts.Write")]
        public ActionResult CreateContact() => new ActionResult();

        [HttpPut("{id}")]
        [AppRouteAuto(RequiredPermission = "Contacts.Write")]
        public ActionResult UpdateContact() => new ActionResult();

        [HttpDelete("{id}")]
        [AppRouteAuto(RequiredPermission = "Contacts.Delete")]
        public ActionResult DeleteContact() => new ActionResult();
    }

    /// <summary>
    /// Bank accounts controller - nested resource pattern
    /// </summary>
    [ApiController]
    [Route("api")]
    [AppBelongsTo("Contacts")]
    public class BankAccountsController
    {
        [HttpGet("contacts/{contactId}/bank-accounts")]
        [AppRouteAuto(RequiredPermission = "Contacts.Read")]
        public ActionResult ListBankAccounts() => new ActionResult();

        [HttpGet("contacts/{contactId}/bank-accounts/{id}")]
        [AppRouteAuto(RequiredPermission = "Contacts.Read")]
        public ActionResult GetBankAccount() => new ActionResult();

        [HttpPost("contacts/{contactId}/bank-accounts")]
        [AppRouteAuto(RequiredPermission = "Contacts.Write")]
        public ActionResult CreateBankAccount() => new ActionResult();
    }

    // ===== TESTING EDGE CASES =====

    /// <summary>
    /// Anchor for Inventory module
    /// </summary>
    [AppModule(Name = "Inventory", DisplayName = "Inventory", Version = "0.5.0")]
    public static class InventoryModuleAnchor { }

    /// <summary>
    /// Controller with multiple [Route] attributes (each should generate separate routes)
    /// </summary>
    [ApiController]
    [Route("api/inventory/products")]
    [Route("api/v2/products")] // Second route prefix
    [AppBelongsTo("Inventory")]
    public class ProductsController
    {
        [HttpGet]
        [AppRouteAuto(RequiredPermission = "Inventory.Read")]
        public ActionResult List() => new ActionResult();

        [HttpGet("{id}")]
        [AppRouteAuto(RequiredPermission = "Inventory.Read")]
        public ActionResult Get() => new ActionResult();
    }

    /// <summary>
    /// Controller using [controller] and [action] tokens
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AppBelongsTo("Inventory")]
    public class WarehousesController
    {
        [HttpGet]
        [AppRouteAuto(RequiredPermission = "Inventory.Read")]
        public ActionResult Index() => new ActionResult();

        [HttpGet("[action]/{id}")]
        [AppRouteAuto(RequiredPermission = "Inventory.Read")]
        public ActionResult Details() => new ActionResult();

        // Test PathOverride
        [HttpGet]
        [AppRouteAuto(RequiredPermission = "Inventory.Read",
                      PathOverride = "/api/warehouse-summary")]
        public ActionResult Summary() => new ActionResult();
    }

    /// <summary>
    /// Controller with MethodOverride
    /// </summary>
    [ApiController]
    [Route("api/stock")]
    [AppBelongsTo("Inventory")]
    public class StockController
    {
        // This has [HttpGet] but we override to POST for some reason
        [HttpGet("adjust")]
        [AppRouteAuto(RequiredPermission = "Inventory.Adjust",
                      MethodOverride = "POST")]
        public ActionResult AdjustStock() => new ActionResult();
    }

    // ===== MODULE WITHOUT ANCHOR (implicit module) =====
    // This controller belongs to a module that has no anchor class

    [ApiController]
    [Route("api/reports")]
    [AppBelongsTo("Reporting")]
    public class ReportsController
    {
        [HttpGet("sales")]
        [AppRouteAuto(RequiredPermission = "Reports.View")]
        public ActionResult SalesReport() => new ActionResult();

        [HttpPost("generate")]
        [AppRouteAuto(RequiredPermission = "Reports.Generate")]
        public ActionResult GenerateReport() => new ActionResult();
    }
}
