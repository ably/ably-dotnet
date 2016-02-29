using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IO.Ably
{
    internal enum AuthMethod : byte
    {
        Basic,
        Token
    }

    /// <summary>Specifies a description for a property or event.</summary>
    internal class DescriptionAttribute : Attribute
    {
        public string Description { get; private set; }
        public DescriptionAttribute( string d )
        {
            Description = d;
        }
    }

    /// <summary>The library supports several token authentication methods, this enum lists those methods + descriptions.</summary>
    internal enum TokenAuthMethod : byte
    {
        [Description( "None, no authentication parameters" )]
        None = 0,

        [Description( "Token auth with callback" )]
        Callback,

        [Description( "Token auth with URL" )]
        Url,

        [Description( "Token auth with client-side signing" )]
        Signing,

        [Description( "Token auth with supplied token only" )]
        JustToken
    }

    internal static class TokenAuthMethods
    {
        static readonly Dictionary<TokenAuthMethod,string> s_dict;

        static TokenAuthMethods()
        {
            Type tEnum = typeof( TokenAuthMethod );
            s_dict = Enum.GetValues( tEnum )
                .Cast<TokenAuthMethod>()
                .ToDictionary( a => a, a =>
                {
                    // http://blogs.msdn.com/b/paulwhit/archive/2008/03/31/use-the-descriptionattribute-with-an-enum-to-display-status-messages.aspx
                    FieldInfo fi = tEnum.GetRuntimeField( a.ToString() );
                    DescriptionAttribute attribute = fi.GetCustomAttribute<DescriptionAttribute>( false );
                    if( null != attribute )
                        return attribute.Description;
                    return a.ToString();
                } );
        }

        /// <summary>Make human-readable string from TokenAuthMethod</summary>
        public static string description( this TokenAuthMethod m )
        {
            return s_dict[ m ];
        }
    }
}