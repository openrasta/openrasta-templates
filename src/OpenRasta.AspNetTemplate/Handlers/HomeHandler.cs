using System;
using OpenRasta.AspNetTemplate.Resources;
using OpenRasta.Web;

namespace OpenRasta.AspNetTemplate.Handlers
{
    public class HomeHandler
    {
        public Home Get()
        {
            return new Home { Message = "Hello world" };
        }
    }
}