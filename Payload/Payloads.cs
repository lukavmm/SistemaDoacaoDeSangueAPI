using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace SistemaDoacaoSangue.Payloads
{
    public record MessagePayload(string ?message, string ?error);


}
