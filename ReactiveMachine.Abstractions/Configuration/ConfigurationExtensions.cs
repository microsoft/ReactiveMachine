// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    public static class ConfigurationExtensions
    {
        public static bool TryGetConfiguration<T>(this ICompiledApplication compiledApp, out T result)
        {
            if (compiledApp.Configurations.TryGetValue(typeof(T), out var o))
            {
                result = (T)o;
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        public static T GetRequiredConfiguration<T>(this ICompiledApplication compiledApp)
        {
            if (! compiledApp.Configurations.TryGetValue(typeof(T), out var o))
            {
                throw new BuilderException($"missing configuration {typeof(T).FullName}");
            }
            return (T)o;
        }

        public static bool TryGetConfiguration<T>(this IServiceBuilder sb, out T result)
        {
            var o = sb.GetConfiguration<T>();
            if (o != null)
            {
                result = (T)o;
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        public static T GetRequiredConfiguration<T>(this IServiceBuilder sb)
        {
            var o = sb.GetConfiguration<T>();
            if (o == null)
            {
                throw new BuilderException($"missing configuration {typeof(T).FullName}");
            }
            return (T)o;
        }
    }
}
