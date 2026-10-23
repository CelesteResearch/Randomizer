using System;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Errors
{
    public class GenerationError : Exception
    {
        public GenerationError(string message) : base(message)
        {
        }
    }
}
