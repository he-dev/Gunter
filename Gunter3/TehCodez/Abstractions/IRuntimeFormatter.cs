﻿using System.Collections.Generic;
using Reusable;

namespace Gunter
{
    public interface IRuntimeFormatter
    {
        string Format(string text);

        IRuntimeFormatter AddRange(IEnumerable<KeyValuePair<SoftString, object>> variables);
    }
}