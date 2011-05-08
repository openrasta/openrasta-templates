using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using OpenRasta.Configuration;
using OpenRasta.Web;
using OpenRasta.Codecs;

namespace OpenRasta.AspNetTemplate
{
    public class Configuration : IConfigurationSource
    {
        public void Configure()
        {
            ResourceSpace.Uses.LinkingXmlDataContract();
            ResourceSpace.Uses.ConventionsFrom(this);
        }
    }
}