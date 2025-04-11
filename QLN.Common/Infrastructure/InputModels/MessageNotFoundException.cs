using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.InputModels
{
    public class MessageNotFoundException : Exception
    {
        public MessageNotFoundException() { }
        public MessageNotFoundException(string message) : base(message) { }
        public MessageNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}
