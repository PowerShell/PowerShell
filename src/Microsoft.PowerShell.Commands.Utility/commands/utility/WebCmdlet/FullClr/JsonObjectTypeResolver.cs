/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Microsoft.PowerShell.Commands
{
    internal class JsonObjectTypeResolver : JavaScriptTypeResolver
    {
        public override Type ResolveType(string id)
        {
            // TODO: this seems to work, but it's still a little suspect
            // and probably worth gaining a little deeper understanding.
            Type type = typeof(Dictionary<string, object>);
            return (type);
        }

        /// <summary>
        /// Override abstract methods
        /// </summary>
        public override string ResolveTypeId(Type type)
        {
            return (string.Empty);
        }
    }
}