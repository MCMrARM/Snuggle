﻿using System;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI, Flags]
    public enum UnityBundleBlockFlags {
        SerializedFile = 4,
    }
}
