﻿using System;

namespace MyExtensions
{
   public static class MyExtensionsData
   {
      public const string VersionRaw = "1.3";

      public static Version Version
      {
         get { return new Version(VersionRaw); }
      }
   }
}

