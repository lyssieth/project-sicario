﻿using Microsoft.Extensions.Configuration;
using SicarioPatch.Components;

namespace SicarioPatch.App.Shared
{
    public class BrandProvider : IBrandProvider
    {
        public string AppName { get; init; } = "Project Sicario";
        public string OwnerName { get; init; } = "agc93";
        public string ShortName { get; init; } = "Sicario";
    }
}