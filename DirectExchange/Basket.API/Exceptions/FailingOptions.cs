using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Exceptions
{
    public class FailingOptions
    {
        public string ConfigPath = "/Failing";
        public List<string> EndpointPaths { get; set; } = new List<string>();

        public List<string> NotFilteredPaths { get; set; } = new List<string>();
    }
}
