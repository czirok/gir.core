﻿using Generator3.Renderer.Internal;

namespace Generator3.Generation.Callback
{
    public class InternalDelegateTemplate : Template<InternalDelegateModel>
    {
        public string Render(InternalDelegateModel delegateModel)
        {
            return $@"
using System;
using System.Runtime.InteropServices;

#nullable enable

namespace { delegateModel.NamespaceName }
{{
    // AUTOGENERATED FILE - DO NOT MODIFY

    public delegate {delegateModel.ReturnType.NullableTypeName} {delegateModel.Name}({delegateModel.Parameters.Render()});
}}";
        }
    }
}
