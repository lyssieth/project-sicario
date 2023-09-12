﻿using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SicarioPatch.Core;

namespace SicarioPatch.App;

[PublicAPI]
public static class CoreExtensions
{
    public static void AddToOrder<TKey>(this Dictionary<TKey, int> dict, TKey obj, bool? state = null)
        where TKey : notnull
    {
        if (state.HasValue)
        {
            switch (state)
            {
                case true when !dict.ContainsKey(obj):
                    dict.Add(obj, 10);
                    break;
                case false when dict.ContainsKey(obj):
                    dict.Remove(obj);
                    break;
            }
        }
        else
        {
            if (!dict.ContainsKey(obj))
                dict.Add(obj, 10);
            else
                dict.Remove(obj);
        }
    }
}