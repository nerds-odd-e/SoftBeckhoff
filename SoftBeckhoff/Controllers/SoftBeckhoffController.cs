using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using SoftBeckhoff.Interfaces;
using SoftBeckhoff.Models;
using SoftBeckhoff.Services;
using TwinCAT.Ads.TcpRouter;

namespace SoftBeckhoff.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SoftBeckhoffController : ControllerBase
    {
        private readonly ILogger<SoftBeckhoffController> logger;
        private readonly IPlcService plcService;
        private readonly IRouterService routerService;

        public SoftBeckhoffController(ILogger<SoftBeckhoffController> logger, IPlcService plcService, IRouterService routerService)
        {
            this.logger = logger;
            this.plcService = plcService;
            this.routerService = routerService;
        }

        [HttpGet("/symbols")]
        public IEnumerable<SymbolDto> GetSymbols()
        {
            return plcService.GetSymbols();
        }
        
        [HttpGet("/symbols/{name}")]
        public byte[] GetSymbol([FromRoute]string name)
        {
            return plcService.GetSymbol(name);
        }
        
        [HttpPut("/symbols/{name}")]
        [Consumes("application/octet-stream")]
        public void SetSymbol([FromRoute]string name, [ModelBinder(BinderType = typeof(ByteArrayModelBinder))] byte[] value)
        {
            plcService.SetSymbol(name, value);
        }
        
        [HttpPost("/symbols")]
        public void CreateSymbol([FromBody]SymbolDto symbol)
        {
            plcService.CreateSymbol(symbol);
        }
        
        [HttpGet("/routes")]
        public RouteCollection GetRoutes()
        {
            return routerService.GetRoutes();
        }
        
        [HttpPut("/routes")]
        public bool AddRoutes([FromBody]RouteSetting route)
        {
            return routerService.TryAddRoute(route);
        }
    }

    public class ByteArrayModelBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            using (var ms = new MemoryStream())
            {
                await bindingContext.HttpContext.Request.Body.CopyToAsync(ms);
                bindingContext.Result = ModelBindingResult.Success(ms.ToArray());
            }
        }
    }
}
